using System;
using System.Collections.Generic;
using VideoOS.Platform;
using VideoOS.Platform.Messaging;

namespace CoreCommandMIP
{
    /// <summary>
    /// Helper class for managing User-Defined Event (UDE) definitions in Milestone XProtect.
    /// These events can trigger alarms through Management Client rules.
    /// </summary>
    internal static class EventDefinitionHelper
    {
        // Event Type Names (as they appear in Management Client)
        public const string C2AlertEventName = "C2.Alert";
        public const string C2AlarmEventName = "C2.Alarm";
        public const string C2AlarmClearedEventName = "C2.AlarmCleared";
        public const string C2TrackEnterRegionEventName = "C2.TrackEnterRegion";
        public const string C2TrackLostEventName = "C2.TrackLost";

        /// <summary>
        /// Gets all defined C2 event types
        /// </summary>
        public static List<string> GetAllEventTypes()
        {
            return new List<string>
            {
                C2AlertEventName,
                C2AlarmEventName,
                C2AlarmClearedEventName,
                C2TrackEnterRegionEventName,
                C2TrackLostEventName
            };
        }

        /// <summary>
        /// Gets the core event types (Alert and Alarm) that should be wired by default
        /// </summary>
        public static List<string> GetCoreEventTypes()
        {
            return new List<string>
            {
                C2AlertEventName,
                C2AlarmEventName
            };
        }

        /// <summary>
        /// Creates event data for a C2 Alert (medium severity)
        /// </summary>
        public static C2EventData CreateAlertEvent(
            string c2AlarmId,
            long trackId,
            string message,
            string regionId = null,
            List<Guid> cameraIds = null)
        {
            return new C2EventData
            {
                EventType = C2AlertEventName,
                C2AlarmId = c2AlarmId,
                TrackId = trackId,
                Message = message,
                RegionId = regionId,
                Severity = EventSeverity.Medium,
                Timestamp = DateTime.UtcNow,
                CameraIds = cameraIds ?? new List<Guid>()
            };
        }

        /// <summary>
        /// Creates event data for a C2 Alarm (high severity)
        /// </summary>
        public static C2EventData CreateAlarmEvent(
            string c2AlarmId,
            long trackId,
            string message,
            string regionId = null,
            List<Guid> cameraIds = null)
        {
            return new C2EventData
            {
                EventType = C2AlarmEventName,
                C2AlarmId = c2AlarmId,
                TrackId = trackId,
                Message = message,
                RegionId = regionId,
                Severity = EventSeverity.High,
                Timestamp = DateTime.UtcNow,
                CameraIds = cameraIds ?? new List<Guid>()
            };
        }

        /// <summary>
        /// Creates event data for a cleared alarm
        /// </summary>
        public static C2EventData CreateAlarmClearedEvent(
            string c2AlarmId,
            long trackId,
            string message)
        {
            return new C2EventData
            {
                EventType = C2AlarmClearedEventName,
                C2AlarmId = c2AlarmId,
                TrackId = trackId,
                Message = message,
                Severity = EventSeverity.Info,
                Timestamp = DateTime.UtcNow,
                CameraIds = new List<Guid>()
            };
        }

        /// <summary>
        /// Creates event data for track entering a region
        /// </summary>
        public static C2EventData CreateTrackEnterRegionEvent(
            long trackId,
            string regionId,
            string regionName,
            string classification)
        {
            return new C2EventData
            {
                EventType = C2TrackEnterRegionEventName,
                C2AlarmId = null, // Not an alarm
                TrackId = trackId,
                Message = $"Track {trackId} ({classification}) entered region {regionName}",
                RegionId = regionId,
                Severity = EventSeverity.Info,
                Timestamp = DateTime.UtcNow,
                CameraIds = new List<Guid>()
            };
        }

        /// <summary>
        /// Gets the recommended alarm definition for an event type
        /// </summary>
        public static AlarmDefinitionInfo GetRecommendedAlarmDefinition(string eventType)
        {
            switch (eventType)
            {
                case C2AlertEventName:
                    return new AlarmDefinitionInfo
                    {
                        Name = "C2 Alert Alarm",
                        Description = "Medium-severity alert from C2 system",
                        SourceEventType = C2AlertEventName,
                        Severity = AlarmSeverity.Medium,
                        Category = "C2 Alerts"
                    };

                case C2AlarmEventName:
                    return new AlarmDefinitionInfo
                    {
                        Name = "C2 Alarm",
                        Description = "High-severity alarm from C2 system requiring immediate action",
                        SourceEventType = C2AlarmEventName,
                        Severity = AlarmSeverity.High,
                        Category = "C2 Alarms"
                    };

                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Data structure for C2 events sent to Milestone
    /// </summary>
    [Serializable]
    public class C2EventData
    {
        public string EventType { get; set; }
        public string C2AlarmId { get; set; }
        public long TrackId { get; set; }
        public string Message { get; set; }
        public string RegionId { get; set; }
        public EventSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public List<Guid> CameraIds { get; set; }

        // Additional metadata
        public string Classification { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Velocity { get; set; }
        public double Confidence { get; set; }
        public string Site { get; set; }
    }

    /// <summary>
    /// Event severity levels
    /// </summary>
    public enum EventSeverity
    {
        Info = 0,
        Medium = 1,
        High = 2
    }

    /// <summary>
    /// Alarm severity levels (matches Milestone priorities)
    /// </summary>
    public enum AlarmSeverity
    {
        Low = 1,
        Medium = 5,
        High = 10
    }

    /// <summary>
    /// Information about recommended alarm definitions
    /// </summary>
    public class AlarmDefinitionInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string SourceEventType { get; set; }
        public AlarmSeverity Severity { get; set; }
        public string Category { get; set; }
    }
}
