# Replace the Init and HandleTrackAlarmMessage methods manually

## File: Background\TrackAlarmEventHandler.cs

### Replace Init() method (around line 29-55):

```csharp
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
```

### Replace HandleTrackAlarmMessage() method (around line 71-97):

```csharp
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
```

### Replace ProcessTrackAlarm() method (around line 103-145):

```csharp
private void ProcessTrackAlarm(TrackAlarmData alarmData)
{
    // Prevent duplicate processing
    if (_processedAlarms.Contains(alarmData.TrackId))
    {
        LogBoth(false, $"? Track {alarmData.TrackId} already processed, skipping duplicate");
        return;
    }

    _processedAlarms.Add(alarmData.TrackId);

    // Create detailed log entry
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

    // Create alarm event (visible in Management Client and Smart Client Event Manager)
    EnvironmentManager.Instance.Log(
        alarmData.Priority <= 3, // isError=true for high/medium priority
        "CoreCommandMIP.TrackAlarm",
        logMessage,
        null);
    
    LogBoth(false, $"? ALARM CREATED for Track {alarmData.TrackId} - {priorityText}");

    // Allow new alarm after 30 seconds
    System.Threading.Tasks.Task.Delay(30000).ContinueWith(_ =>
    {
        _processedAlarms.Remove(alarmData.TrackId);
        LogBoth(false, $"? Track {alarmData.TrackId} removed from processed set, can alarm again");
    });
}
```

## What This Logs:

### In Management Client ? Logs:
```
[CoreCommandMIP.TrackAlarm] === Initializing - Message ID: CoreCommandMIP.TrackAlarm ===
[CoreCommandMIP.TrackAlarm] ? READY - Listening for alarm messages from Smart Client
[CoreCommandMIP.TrackAlarm] ? MESSAGE RECEIVED from: ...
[CoreCommandMIP.TrackAlarm] ? Processing Track 123 - Drone at Site Name
[CoreCommandMIP.TrackAlarm] ? ALARM CREATED for Track 123 - [HIGH]
[CoreCommandMIP.TrackAlarm] [HIGH] TRACK ALARM - Track 123 (Drone) detected at Site Name...
```

### In Debug Output:
```
[TrackAlarm] === Initializing - Message ID: CoreCommandMIP.TrackAlarm ===
[TrackAlarm] ? READY - Listening for alarm messages from Smart Client
[TrackAlarm] ? MESSAGE RECEIVED from: ...
[TrackAlarm] ? Processing Track 123 - Drone at Site Name
[TrackAlarm] ? ALARM CREATED for Track 123 - [HIGH]
```

## After Making Changes:

1. Build the project
2. Copy DLL to Event Server plugin folder
3. Restart Event Server service
4. Check Management Client ? Logs ? Filter by "CoreCommandMIP"
5. Trigger alarm from Smart Client (track with Alarming=true)
6. Watch logs appear in real-time!
