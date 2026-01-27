$file = "Background\TrackAlarmEventHandler.cs"
$content = Get-Content $file -Raw

Write-Host "Adding comprehensive logging to TrackAlarmEventHandler..."

# Fix Init method
$oldInit = @'
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
'@

$newInit = @'
	public void Init()
	{
		LogBoth(false, $"=== Initializing - Message ID: {CoreCommandMIPDefinition.TrackAlarmMessageId} ===");
		
		try
		{
			_messageReceiver = EnvironmentManager.Instance.RegisterReceiver(
				new MessageReceiver(HandleTrackAlarmMessage),
				new MessageIdFilter(CoreCommandMIPDefinition.TrackAlarmMessageId));

			LogBoth(false, "? READY - Listening for alarm messages from Smart Client");
		}
		catch (Exception ex)
		{
			LogBoth(true, $"? INIT FAILED: {ex.Message}");
			throw;
		}
	}
'@

$content = $content.Replace($oldInit, $newInit)
Write-Host "? Updated Init() method"

# Fix HandleTrackAlarmMessage
$oldHandle = @'
	private object HandleTrackAlarmMessage(Message message, FQID destination, FQID source)
	{
		System.Diagnostics.Debug.WriteLine($"TrackAlarmEventHandler: Received message from {source?.ObjectId}");
		
		try
		{
			if (message?.Data is TrackAlarmData alarmData)
			{
				System.Diagnostics.Debug.WriteLine($"TrackAlarmEventHandler: Processing alarm for track {alarmData.TrackId}");
				ProcessTrackAlarm(alarmData);
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"TrackAlarmEventHandler: Message data is not TrackAlarmData, type: {message?.Data?.GetType().FullName}");
			}
		}
		catch (Exception ex)
		{
			EnvironmentManager.Instance.Log(
				true,
				"CoreCommandMIP.TrackAlarm",
				$"Error processing track alarm: {ex.Message}",
				null);
			System.Diagnostics.Debug.WriteLine($"TrackAlarmEventHandler error: {ex}");
		}

		return null;
	}
'@

$newHandle = @'
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
'@

$content = $content.Replace($oldHandle, $newHandle)
Write-Host "? Updated HandleTrackAlarmMessage() method"

# Fix ProcessTrackAlarm - update logging
$content = $content -replace 'System\.Diagnostics\.Debug\.WriteLine\(\$"TrackAlarmEventHandler: ProcessTrackAlarm called for track \{alarmData\.TrackId\}"\);', 'LogBoth(false, $"? Processing alarm for Track {alarmData.TrackId}");'
$content = $content -replace 'System\.Diagnostics\.Debug\.WriteLine\(\$"TrackAlarmEventHandler: Track \{alarmData\.TrackId\} already processed, skipping"\);', 'LogBoth(false, $"? Track {alarmData.TrackId} already processed, skipping");'
$content = $content -replace 'System\.Diagnostics\.Debug\.WriteLine\(\$"TrackAlarmEventHandler: Creating log entry for track \{alarmData\.TrackId\}"\);', ''
$content = $content -replace 'System\.Diagnostics\.Debug\.WriteLine\(\$"TrackAlarmEventHandler: Log entry created: \{logMessage\}"\);', 'LogBoth(false, $"? ALARM CREATED for Track {alarmData.TrackId} - {priorityText}");'
$content = $content -replace 'System\.Diagnostics\.Debug\.WriteLine\(\$"TrackAlarmEventHandler: Removed track \{alarmData\.TrackId\} from processed set"\);', 'LogBoth(false, $"? Track {alarmData.TrackId} can alarm again");'

Write-Host "? Updated ProcessTrackAlarm() logging"

Set-Content $file -Value $content -NoNewline
Write-Host "`n??? Comprehensive logging added successfully!"
Write-Host "`nNow:"
Write-Host "1. Build the project"
Write-Host "2. Deploy to Event Server"
Write-Host "3. Restart Event Server service"
Write-Host "4. Check Management Client ? Logs for detailed event flow"
