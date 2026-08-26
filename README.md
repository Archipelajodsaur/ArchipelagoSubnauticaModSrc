# Archipelago Subnautica Mod

A Subnautica mod client for Archipelago Randomizer. More info on Archipelago here: https://github.com/ArchipelagoMW/Archipelago

## Build from source

The project needs assemblies from a local Subnautica installation and the BepInEx version shipped
with this mod. These binaries are staged under .local-dependencies/ and are never committed.

Prerequisites are the .NET 8 SDK, curl, unzip, and either sha256sum or shasum.

    ./scripts/setup-local-deps.sh /path/to/Managed.zip
    dotnet restore mod/Archipelago.csproj
    dotnet build mod/Archipelago.csproj --configuration Release

Managed.zip must contain the Managed/ directory copied from
Subnautica/Subnautica_Data/Managed. The setup script extracts only the assemblies referenced by
the project. It also downloads the official 1.9.3 mod archive, verifies its pinned SHA-256 digest,
and extracts the four required BepInEx assemblies.

To build directly against an installed game, override the dependency directories. Set
ModInstallDir to copy the built mod and Archipelago.MultiClient.Net.dll into the game after a
successful build:

    dotnet build mod/Archipelago.csproj --configuration Release -p:SubnauticaManagedDir='E:\SteamLibrary\steamapps\common\Subnautica\Subnautica_Data\Managed' -p:BepInExCoreDir='E:\SteamLibrary\steamapps\common\Subnautica\BepInEx\core' -p:ModInstallDir='E:\SteamLibrary\steamapps\common\Subnautica\BepInEx\plugins\Archipelago'
