using System;
using System.Collections.Generic;
using VideoOS.Platform;
using VideoOS.Platform.Messaging;

namespace CoreCommandMIP.Background
{
	/// <summary>
	/// Server-side component that handles Track Alarm messages and creates XProtect User-Defined Events.
	/// These events can trigger alarms in Alarm Manager through Management Client rules.
	/// </summary>
	public class TrackAlarmEventHandler
	{
		private readonly HashSet<long> _processedAlarms = new HashSet<long>();
		private object _messageReceiver;
		private readonly FQID _pluginFqid;
		private readonly EventTriggerService _eventTrigger;
		
	public TrackAlarmEventHandler()
	{
		// Create simple FQID for event source
		_pluginFqid = new FQID();
		
		// Create event trigger service
		_eventTrigger = new EventTriggerService(CoreCommandMIPDefinition.CoreCommandMIPPluginId);
	}
		
		/// <summary>
		/// Helper to log to both XProtect and Debug Output
		/// </summary>
		private void LogBoth(bool isError, string message)
		{
			EnvironmentManager.Instance.Log(isError, "CoreCommandMIP.TrackAlarm", message, null);
			System.Diagnostics.Debug.WriteLine($"[TrackAlarm] {message}");
		}

		/// <summary>
		/// Initialize the event handler and register for alarm messages.
		/// </summary>
		public void Init()
		{
			LogBoth(false, $"=== Initializing - Message ID: {CoreCommandMIPDefinition.TrackAlarmMessageId} ===");
			
			try
			{
				_messageReceiver = EnvironmentManager.Instance.RegisterReceiver(
					new MessageReceiver(HandleTrackAlarmMessage),
					new MessageIdFilter(CoreCommandMIPDefinition.TrackAlarmMessageId));

				LogBoth(false, "? READY - Listening for alarm messages from Smart Client, will send User-Defined Events");
			}
			catch (Exception ex)
			{
				LogBoth(true, $"? INIT FAILED: {ex.Message}");
				throw;
			}
		}

		/// <summary>
		/// Cleanup when unloading.
		/// </summary>
		public void Close()
		{
			if (_messageReceiver != null)
			{
				EnvironmentManager.Instance.UnRegisterReceiver(_messageReceiver);
				_messageReceiver = null;
			}
			_processedAlarms.Clear();
		}

		/// <summary>
		/// Handle incoming track alarm messages from Smart Client.
		/// </summary>
		private object HandleTrackAlarmMessage(Message message, FQID destination, FQID source)
		{
			LogBoth(false, $"? MESSAGE RECEIVED from: {source?.ObjectId}");
			
			try
			{
				if (message?.Data is TrackAlarmData alarmData)
				{
					LogBoth(false, $"? Processing Track {alarmData.TrackId} - {alarmData.Classification} at {alarmData.Site}");
					ProcessTrackAlarm(alarmData);
				}
				else
				{
					LogBoth(true, $"? Invalid message data type: {message?.Data?.GetType().FullName}");
				}
			}
			catch (Exception ex)
			{
				LogBoth(true, $"? Message handling error: {ex.Message}");
			}

			return null;
		}

	/// <summary>
	/// Process a track alarm and send User-Defined Event to XProtect.
	/// Management Client rules can convert these events to alarms.
	/// </summary>
	private void ProcessTrackAlarm(TrackAlarmData alarmData)
	{
		// Prevent duplicate processing
		if (_processedAlarms.Contains(alarmData.TrackId))
		{
			LogBoth(false, $"? Track {alarmData.TrackId} already processed, skipping");
			return;
		}

		_processedAlarms.Add(alarmData.TrackId);

		try
		{
			var priorityText = GetPriorityText(alarmData.Priority);

			// Generate C2 Alarm ID (unique identifier for this alarm instance)
			var c2AlarmId = $"T{alarmData.TrackId}_{alarmData.Timestamp:yyyyMMddHHmmss}";

			// Build event message
			var message = $"Track {alarmData.TrackId} ({alarmData.Classification}) detected at {alarmData.Site}";

			// Generate site-specific event type names
			var alertEventName = $"C2.Alert - {alarmData.Site}";
			var alarmEventName = $"C2.Alarm - {alarmData.Site}";

			// Create C2EventData with all metadata
			var eventData = new C2EventData
			{
				EventType = alarmData.Priority <= 2 ? alarmEventName : alertEventName,
				C2AlarmId = c2AlarmId,
				TrackId = alarmData.TrackId,
				Message = message,
				Severity = alarmData.Priority <= 2 ? EventSeverity.High : EventSeverity.Medium,
				Timestamp = alarmData.Timestamp,
				Classification = alarmData.Classification,
				Latitude = alarmData.Latitude,
				Longitude = alarmData.Longitude,
				Altitude = alarmData.Altitude,
				Velocity = alarmData.Velocity,
				Confidence = alarmData.Confidence,
				Site = alarmData.Site,
				CameraIds = new List<Guid>() // TODO: Get from configuration
			};

			// Trigger the event
			bool success = _eventTrigger.TriggerEvent(eventData);

			if (success)
			{
				LogBoth(false, $"? EVENT TRIGGERED for Track {alarmData.TrackId} - {priorityText}");
				LogBoth(false, $"  ? C2 Alarm ID: {c2AlarmId}");
				LogBoth(false, $"  ? Event Type: {eventData.EventType}");
			}
			else
			{
				LogBoth(true, $"? Failed to trigger event for Track {alarmData.TrackId}");
			}
		}
		catch (Exception ex)
		{
			LogBoth(true, $"? Failed to process alarm for Track {alarmData.TrackId}: {ex.Message}");
		}

		// Allow new alarm after 30 seconds
		System.Threading.Tasks.Task.Delay(30000).ContinueWith(_ =>
		{
			_processedAlarms.Remove(alarmData.TrackId);
			LogBoth(false, $"? Track {alarmData.TrackId} can alarm again");
		});
	}

		private string GetPriorityText(int priority)
		{
			return priority <= 2 ? "[HIGH]" :
			       priority <= 5 ? "[MEDIUM]" :
			       "[LOW]";
		}
	}

	/// <summary>
	/// Data structure for track alarm messages sent from Smart Client to Event Server.
	/// Must be Serializable for cross-process messaging.
	/// </summary>
	[Serializable]
	public class TrackAlarmData
	{
		public long TrackId { get; set; }
		public string Classification { get; set; }
		public double Confidence { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public double Altitude { get; set; }
		public double Velocity { get; set; }
		public string Site { get; set; }
		public DateTime Timestamp { get; set; }
		public int Priority { get; set; } // 1=High, 5=Medium, 10=Low
	}
}
