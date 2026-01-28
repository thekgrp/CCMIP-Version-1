using System;
using System.Collections.Generic;
using VideoOS.Platform;
using VideoOS.Platform.Messaging;

namespace CoreCommandMIP.Background
{
    /// <summary>
    /// Service for triggering User-Defined Events in Milestone XProtect.
    /// Events can be configured in Management Client to trigger alarms.
    /// </summary>
    internal class EventTriggerService
    {
        private readonly Guid _pluginId;

        public EventTriggerService(Guid pluginId)
        {
            _pluginId = pluginId;
        }

        /// <summary>
        /// Triggers a C2 event in Milestone
        /// </summary>
        public bool TriggerEvent(C2EventData eventData)
        {
            try
            {
                LogBoth(false, $"? Triggering event: {eventData.EventType} for Track {eventData.TrackId}");

                // Create log entry with detailed information
                var logMessage = BuildLogMessage(eventData);
                var isError = eventData.Severity == EventSeverity.High || eventData.Severity == EventSeverity.Medium;
                
                EnvironmentManager.Instance.Log(isError, "CoreCommandMIP.Events", logMessage, null);

                LogBoth(false, $"? Event logged: {eventData.EventType}");
                
                // TODO: When we discover the correct Milestone API for User-Defined Events,
                // we'll trigger the actual event here instead of just logging
                // For now, log entries appear in Management Client ? Logs
                
                return true;
            }
            catch (Exception ex)
            {
                LogBoth(true, $"? Failed to trigger event: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Triggers a C2 Alert event (medium severity)
        /// </summary>
        public bool TriggerAlert(string c2AlarmId, long trackId, string message, 
            string regionId = null, List<Guid> cameraIds = null)
        {
            var eventData = EventDefinitionHelper.CreateAlertEvent(
                c2AlarmId, trackId, message, regionId, cameraIds);
            return TriggerEvent(eventData);
        }

        /// <summary>
        /// Triggers a C2 Alarm event (high severity)
        /// </summary>
        public bool TriggerAlarm(string c2AlarmId, long trackId, string message,
            string regionId = null, List<Guid> cameraIds = null)
        {
            var eventData = EventDefinitionHelper.CreateAlarmEvent(
                c2AlarmId, trackId, message, regionId, cameraIds);
            return TriggerEvent(eventData);
        }

        /// <summary>
        /// Triggers an alarm cleared event
        /// </summary>
        public bool TriggerAlarmCleared(string c2AlarmId, long trackId, string message)
        {
            var eventData = EventDefinitionHelper.CreateAlarmClearedEvent(
                c2AlarmId, trackId, message);
            return TriggerEvent(eventData);
        }

        private string BuildLogMessage(C2EventData eventData)
        {
            var severityText = eventData.Severity == EventSeverity.High ? "[HIGH]" :
                               eventData.Severity == EventSeverity.Medium ? "[MEDIUM]" :
                               "[INFO]";

            var message = $"{severityText} {eventData.EventType} - {eventData.Message}";

            if (!string.IsNullOrEmpty(eventData.C2AlarmId))
            {
                message += $"\nC2 Alarm ID: {eventData.C2AlarmId}";
            }

            message += $"\nTrack ID: {eventData.TrackId}";

            if (!string.IsNullOrEmpty(eventData.RegionId))
            {
                message += $"\nRegion: {eventData.RegionId}";
            }

            if (!string.IsNullOrEmpty(eventData.Site))
            {
                message += $"\nSite: {eventData.Site}";
            }

            if (!string.IsNullOrEmpty(eventData.Classification))
            {
                message += $"\nClassification: {eventData.Classification}";
            }

            if (eventData.Latitude != 0 || eventData.Longitude != 0)
            {
                message += $"\nLocation: {eventData.Latitude:F6}°, {eventData.Longitude:F6}°";
            }

            if (eventData.Altitude != 0)
            {
                message += $" | Altitude: {eventData.Altitude:F1}m";
            }

            if (eventData.Velocity != 0)
            {
                message += $" | Velocity: {eventData.Velocity:F1}m/s";
            }

            if (eventData.Confidence > 0)
            {
                message += $" | Confidence: {eventData.Confidence:P0}";
            }

            if (eventData.CameraIds != null && eventData.CameraIds.Count > 0)
            {
                message += $"\nAssociated Cameras: {eventData.CameraIds.Count}";
            }

            message += $"\nTimestamp: {eventData.Timestamp:O}";

            return message;
        }

        private void LogBoth(bool isError, string message)
        {
            EnvironmentManager.Instance.Log(isError, "CoreCommandMIP.Events", message, null);
            System.Diagnostics.Debug.WriteLine($"[EventTrigger] {message}");
        }
    }
}
