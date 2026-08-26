#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "usage: $0 /path/to/Managed.zip" >&2
    exit 2
fi

managed_archive=$1
if [[ ! -f "$managed_archive" ]]; then
    echo "Managed archive not found: $managed_archive" >&2
    exit 1
fi

for command_name in curl unzip; do
    if ! command -v "$command_name" >/dev/null 2>&1; then
        echo "required command not found: $command_name" >&2
        exit 1
    fi
done

if command -v sha256sum >/dev/null 2>&1; then
    sha256_file() { sha256sum "$1" | awk '{print $1}'; }
elif command -v shasum >/dev/null 2>&1; then
    sha256_file() { shasum -a 256 "$1" | awk '{print $1}'; }
else
    echo "required command not found: sha256sum or shasum" >&2
    exit 1
fi

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repo_dir=$(cd "$script_dir/.." && pwd)
dependencies_dir=${LOCAL_DEPENDENCIES_DIR:-"$repo_dir/.local-dependencies"}
managed_dir="$dependencies_dir/managed"
bepinex_core_dir="$dependencies_dir/bepinex/core"
cache_dir="$dependencies_dir/cache"

bepinex_url=https://github.com/Berserker66/ArchipelagoSubnauticaModSrc/releases/download/1.9.3/Archipelago_193.zip
bepinex_sha256=c2165113cb9bf15723c946bb9b0ab314fbb8d8f9638453e634cdad0e0e1dc298
bepinex_archive="$cache_dir/Archipelago_193.zip"

mkdir -p "$managed_dir" "$bepinex_core_dir" "$cache_dir"

archive_entry_count() {
    unzip -Z1 "$1" | awk -v expected="$2" '$0 == expected { count++ } END { print count + 0 }'
}

extract_entry() {
    local archive=$1
    local entry=$2
    local destination=$3
    local entry_count
    entry_count=$(archive_entry_count "$archive" "$entry")
    if [[ "$entry_count" -ne 1 ]]; then
        echo "expected exactly one archive entry named $entry, found $entry_count" >&2
        exit 1
    fi

    local temporary_file
    temporary_file=$(mktemp "$dependencies_dir/.extract.XXXXXX")
    if ! unzip -p "$archive" "$entry" >"$temporary_file" || [[ ! -s "$temporary_file" ]]; then
        rm -f "$temporary_file"
        echo "failed to extract archive entry: $entry" >&2
        exit 1
    fi
    chmod 0644 "$temporary_file"
    mv -f "$temporary_file" "$destination"
}

managed_files=(
    Assembly-CSharp.dll
    Assembly-CSharp-firstpass.dll
    PlatformIODefault.dll
    Unity.Addressables.dll
    UnityEngine.dll
    UnityEngine.CoreModule.dll
    UnityEngine.IMGUIModule.dll
    UnityEngine.InputLegacyModule.dll
    UnityEngine.UI.dll
)

for file_name in "${managed_files[@]}"; do
    extract_entry "$managed_archive" "Managed/$file_name" "$managed_dir/$file_name"
done

if [[ ! -f "$bepinex_archive" ]] || [[ "$(sha256_file "$bepinex_archive")" != "$bepinex_sha256" ]]; then
    download_file=$(mktemp "$cache_dir/.Archipelago_193.XXXXXX.zip")
    trap 'rm -f "${download_file:-}"' EXIT
    curl --fail --location --retry 3 --output "$download_file" "$bepinex_url"
    actual_sha256=$(sha256_file "$download_file")
    if [[ "$actual_sha256" != "$bepinex_sha256" ]]; then
        echo "BepInEx archive checksum mismatch: expected $bepinex_sha256, got $actual_sha256" >&2
        exit 1
    fi
    mv -f "$download_file" "$bepinex_archive"
    trap - EXIT
fi

bepinex_files=(
    0Harmony.dll
    BepInEx.dll
    BepInEx.Harmony.dll
    BepInEx.Preloader.dll
)

for file_name in "${bepinex_files[@]}"; do
    extract_entry "$bepinex_archive" "BepInEx/core/$file_name" "$bepinex_core_dir/$file_name"
done

echo "Staged Subnautica assemblies in $managed_dir"
echo "Staged BepInEx assemblies in $bepinex_core_dir"
