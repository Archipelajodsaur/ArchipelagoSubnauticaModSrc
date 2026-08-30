using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
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
        private static string positionKey;
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
                var reporterId = ArchipelagoPlugin.PositionReporterId;
                if (string.IsNullOrWhiteSpace(reporterId))
                {
                    Logging.LogError("Could not publish live position: missing reporter ID.", ingame:false);
                    return;
                }

                var payload = new JObject
                {
                    ["x"] = position.x,
                    ["y"] = position.y,
                    ["z"] = position.z,
                };
                var label = ArchipelagoPlugin.PositionReporterLabel;
                if (!string.IsNullOrWhiteSpace(label))
                {
                    payload["label"] = label;
                }

                positionKey = $"LivePosition_{team}_{slot}";
                currentSession.DataStorage[positionKey] = currentSession.DataStorage[positionKey] +
                    Operation.Update(new Dictionary<string, object> { [reporterId] = payload });
            }
            catch (System.Exception exception)
            {
                Logging.LogError("Could not publish live position: " + exception.Message, ingame:false);
            }
        }

        public static void Reset()
        {
            session = null;
            positionKey = null;
            hasAttempted = false;
        }

        public static void Unpublish()
        {
            if (session == null || session.Socket == null || !session.Socket.Connected ||
                string.IsNullOrWhiteSpace(positionKey) || string.IsNullOrWhiteSpace(ArchipelagoPlugin.PositionReporterId))
            {
                return;
            }

            try
            {
                session.DataStorage[positionKey] = session.DataStorage[positionKey] +
                    Operation.Pop(ArchipelagoPlugin.PositionReporterId);
            }
            catch (System.Exception exception)
            {
                Logging.LogError("Could not remove live position: " + exception.Message, ingame:false);
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
