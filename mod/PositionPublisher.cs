using Archipelago.MultiClient.Net;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Archipelago
{
    internal static class PositionPublisher
    {
        private const float MovementThreshold = 2.0f;
        private const float PublishInterval = 0.5f;
        private const float HeartbeatInterval = 5.0f;

        private static ArchipelagoSession session;
        private static Vector3 lastPosition;
        private static float lastAttemptTime;
        private static bool hasAttempted;

        public static void PublishIfNeeded()
        {
            var currentSession = APState.Session;
            if (APState.state != APState.State.InGame || !APState.Authenticated || currentSession == null ||
                currentSession.Socket == null || !currentSession.Socket.Connected || Player.main == null)
            {
                return;
            }

            var position = Player.main.transform.position;
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
            {
                return;
            }

            var team = currentSession.ConnectionInfo.Team;
            var slot = currentSession.ConnectionInfo.Slot;
            if (team < 0 || slot < 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            var sessionChanged = !object.ReferenceEquals(session, currentSession);
            var elapsed = now - lastAttemptTime;
            var movedEnough = (position - lastPosition).sqrMagnitude >= MovementThreshold * MovementThreshold;

            if (!sessionChanged && hasAttempted && elapsed < HeartbeatInterval &&
                (elapsed < PublishInterval || !movedEnough))
            {
                return;
            }

            session = currentSession;
            lastPosition = position;
            lastAttemptTime = now;
            hasAttempted = true;

            try
            {
                currentSession.DataStorage[$"LivePosition_{team}_{slot}"] = new JObject
                {
                    ["x"] = position.x,
                    ["y"] = position.y,
                    ["z"] = position.z
                };
            }
            catch (System.Exception exception)
            {
                Logging.LogError("Could not publish live position: " + exception.Message, ingame:false);
            }
        }

        public static void Reset()
        {
            session = null;
            hasAttempted = false;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
