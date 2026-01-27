using System;
using System.Collections.Generic;
using VideoOS.Platform;
using VideoOS.Platform.Messaging;

namespace CoreCommandMIP.Background
{
	/// <summary>
	/// Server-side component that handles Track Alarm events and creates XProtect logs.
	/// This runs on the Event Server and processes alarm messages from Smart Client.
	/// </summary>
	public class TrackAlarmEventHandler
	{
		private readonly HashSet<long> _processedAlarms = new HashSet<long>();
		private object _messageReceiver;
		
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
			try
			{
				// Register to receive Track Alarm messages from Smart Client
				_messageReceiver = EnvironmentManager.Instance.RegisterReceiver(
					new MessageReceiver(HandleTrackAlarmMessage),
					new MessageIdFilter(CoreCommandMIPDefinition.TrackAlarmMessageId));

				EnvironmentManager.Instance.Log(
					false,
					"CoreCommandMIP.TrackAlarm",
					"Track Alarm Event Handler initialized on Event Server",
					null);
				
				System.Diagnostics.Debug.WriteLine("TrackAlarmEventHandler: Registered for message ID: " + CoreCommandMIPDefinition.TrackAlarmMessageId);
			}
			catch (Exception ex)
			{
				EnvironmentManager.Instance.Log(
					true,
					"CoreCommandMIP.TrackAlarm",
					$"Failed to initialize event handler: {ex.Message}",
					null);
				System.Diagnostics.Debug.WriteLine($"TrackAlarmEventHandler Init failed: {ex}");
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
	/// Process a track alarm and create appropriate XProtect log entries.
	/// These appear in Management Client ? Log tab and can trigger rules.
	/// </summary>
	private void ProcessTrackAlarm(TrackAlarmData alarmData)
	{
		LogBoth(false, $"? Processing alarm for Track {alarmData.TrackId}");
		
		// Prevent duplicate processing
		if (_processedAlarms.Contains(alarmData.TrackId))
		{
			LogBoth(false, $"? Track {alarmData.TrackId} already processed, skipping");
			return;
		}

		_processedAlarms.Add(alarmData.TrackId);
		

		// Create detailed log entry (appears in Management Client logs)
		var priorityText = alarmData.Priority <= 2 ? "[HIGH]" :
		                   alarmData.Priority <= 5 ? "[MEDIUM]" :
		                   "[LOW]";

		var logMessage = string.Format(
			"{0} TRACK ALARM - Track {1} ({2}) detected at {3}\n" +
			"Location: {4:F4}°, {5:F4}° | Altitude: {6:F1}m | Velocity: {7:F1}m/s | Confidence: {8:P0}",
			priorityText,
			alarmData.TrackId,
			alarmData.Classification,
			alarmData.Site,
			alarmData.Latitude,
			alarmData.Longitude,
			alarmData.Altitude,
			alarmData.Velocity,
			alarmData.Confidence);

		// Log as error for high visibility (shows as red/yellow in Management Client)
		EnvironmentManager.Instance.Log(
			alarmData.Priority <= 3, // isError=true for high/medium priority
			"CoreCommandMIP.TrackAlarm",
			logMessage,
			null);
		
		LogBoth(false, $"? ALARM CREATED for Track {alarmData.TrackId} - {priorityText}");

		// If track stops alarming after 30 seconds, allow new alarm
		System.Threading.Tasks.Task.Delay(30000).ContinueWith(_ =>
		{
			_processedAlarms.Remove(alarmData.TrackId);
			LogBoth(false, $"? Track {alarmData.TrackId} can alarm again");
		});
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
