using System;
using System.Collections.Generic;
using VideoOS.Platform;

namespace CoreCommandMIP.Client
{
	/// <summary>
	/// Manages alarm creation for tracks in alarming state.
	/// Prevents duplicate alarms and sends messages to Event Server for processing.
	/// </summary>
	internal sealed class TrackAlarmManager
	{
		private readonly HashSet<long> _activeAlarmTrackIds = new HashSet<long>();
		private readonly Guid _pluginId;
		private readonly string _siteName;

		internal TrackAlarmManager(Guid pluginId, string siteName)
		{
			_pluginId = pluginId;
			_siteName = siteName ?? "Unknown Site";
		}

		/// <summary>
		/// Process a list of tracks and create alarms for newly alarming tracks.
		/// Removes tracks from active set if they're no longer alarming.
		/// </summary>
		internal void ProcessTracks(IReadOnlyList<SmartMapLocation> tracks)
		{
			if (tracks == null || tracks.Count == 0)
			{
				return;
			}

			var currentAlarmingTracks = new HashSet<long>();

			foreach (var track in tracks)
			{
				if (track.Alarming && track.TrackId != 0)
				{
					currentAlarmingTracks.Add(track.TrackId);

					// Only create alarm if we haven't already created one for this track
					if (!_activeAlarmTrackIds.Contains(track.TrackId))
					{
						CreateAlarmEvent(track);
						_activeAlarmTrackIds.Add(track.TrackId);
					}
				}
			}

			// Remove tracks that are no longer alarming
			_activeAlarmTrackIds.RemoveWhere(id => !currentAlarmingTracks.Contains(id));
		}

	private void CreateAlarmEvent(SmartMapLocation track)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine($"=== Creating alarm for Track {track.TrackId} ===");
			
			// Create track alarm data
			var alarmData = new Background.TrackAlarmData
			{
				TrackId = track.TrackId,
				Classification = track.ClassificationLabel ?? "Unknown",
				Confidence = track.ClassificationConfidence,
				Latitude = track.Latitude,
				Longitude = track.Longitude,
				Altitude = track.Altitude,
				Velocity = track.Velocity,
				Site = _siteName,
				Timestamp = (track.Timestamp ?? DateTimeOffset.UtcNow).DateTime,
				Priority = DetermineAlarmPriority(track)
			};

			System.Diagnostics.Debug.WriteLine($"Alarm data created: Track={alarmData.TrackId}, Class={alarmData.Classification}, Site={alarmData.Site}");
			System.Diagnostics.Debug.WriteLine($"Sending message with ID: {CoreCommandMIPDefinition.TrackAlarmMessageId}");

			// Send alarm message to Event Server for processing
			var message = new VideoOS.Platform.Messaging.Message(
				CoreCommandMIPDefinition.TrackAlarmMessageId,
				alarmData);

			EnvironmentManager.Instance.PostMessage(message, null, null);

			System.Diagnostics.Debug.WriteLine($"? Message POSTED for track {track.TrackId} - Message ID: {CoreCommandMIPDefinition.TrackAlarmMessageId}");
			
			// Log to Smart Client output as well
			EnvironmentManager.Instance.Log(
				false,
				"CoreCommandMIP.SmartClient",
				$"? Sent alarm message for Track {track.TrackId} ({track.ClassificationLabel})",
				null);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"? FAILED to send track alarm for track {track.TrackId}: {ex}");
			EnvironmentManager.Instance.Log(
				true,
				"CoreCommandMIP.SmartClient",
				$"Failed to send alarm for Track {track.TrackId}: {ex.Message}",
				null);
		}
	}

		private static int DetermineAlarmPriority(SmartMapLocation track)
		{
			// XProtect priority: 1=High, 5=Medium, 10=Low
			if (string.Equals(track.ClassificationLabel, "Drone", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(track.ClassificationLabel, "Aerial", StringComparison.OrdinalIgnoreCase))
			{
				return 1; // High priority
			}

			if (string.Equals(track.ClassificationLabel, "Vehicle", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(track.ClassificationLabel, "Person", StringComparison.OrdinalIgnoreCase))
			{
				return 5; // Medium priority
			}

			return 8; // Low priority
		}

		/// <summary>
		/// Clear all tracked alarm IDs. Called when switching sites.
		/// </summary>
		internal void ClearAll()
		{
			_activeAlarmTrackIds.Clear();
		}
	}
}
