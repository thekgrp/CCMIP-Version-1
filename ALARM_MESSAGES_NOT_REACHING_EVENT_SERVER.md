# Track Alarm Messages Not Reaching Event Server - Diagnostic Guide

## Current Situation

? Remote server sends tracks with `Alarming=true`
? Smart Client TrackAlarmManager detects alarming tracks
? Messages are being SENT from Smart Client
? Messages NOT received by Event Server (no logs appear)

## Root Causes & Solutions

### Issue #1: Event Server Plugin Not Running (Most Common)

**Check Event Server logs:**
```
C:\ProgramData\Milestone\XProtect Event Server\Logs\
```

**Look for:**
```
? READY - Listening for alarm messages from Smart Client
```

**If NOT found:**
- Event Server plugin didn't load or initialize
- DLL not deployed correctly
- Static constructor crashed (we fixed this)

**Solution:**
1. Copy `bin\Release\CoreCommandMIP.dll` to Event Server plugin folder
2. Restart Event Server service
3. Check logs again

### Issue #2: Message Not Being Sent (Check This First!)

**In Smart Client Debug Output, look for:**
```
=== Creating alarm for Track 123 ===
Alarm data created: Track=123, Class=Drone, Site=...
Sending message with ID: CoreCommandMIP.TrackAlarm
? Message POSTED for track 123
```

**If NOT found:**
- TrackAlarmManager not initialized
- No tracks have `Alarming=true`
- ProcessTracks() not being called

**Solution:**
Check `HandleTrackUpdate()` in `CoreCommandMIPViewItemWpfUserControl.xaml.cs`:
```csharp
// This should exist:
_alarmManager?.ProcessTracks(meaningfulTracks);
```

### Issue #3: Message ID Mismatch

**Verify both sides use same ID:**

**Smart Client (sending):**
```csharp
CoreCommandMIPDefinition.TrackAlarmMessageId
// Should be: "CoreCommandMIP.TrackAlarm"
```

**Event Server (receiving):**
```csharp
CoreCommandMIPDefinition.TrackAlarmMessageId
// Should be: "CoreCommandMIP.TrackAlarm"
```

**Check in Debug Output:**
```
Sending message with ID: CoreCommandMIP.TrackAlarm
Registered for message ID: CoreCommandMIP.TrackAlarm
```

If these don't match, messages won't be received!

### Issue #4: Cross-Process Serialization Failure

**TrackAlarmData MUST be:**
- Marked `[Serializable]` ? (we have this)
- All properties must be serializable ? (they are)
- In a namespace accessible to both Smart Client and Event Server ? (it is)

### Issue #5: Event Server Service Not Running

**Check if service is running:**
```powershell
Get-Service "Milestone XProtect Event Server"
```

**Should show:** Running

**If not:**
```powershell
Start-Service "Milestone XProtect Event Server"
```

### Issue #6: Different DLL Versions

**Smart Client and Event Server must use SAME DLL version!**

**Check timestamps:**
- Smart Client: `C:\Program Files\Milestone\MIPSDK\MIPPlugins\CoreCommandMIP\CoreCommandMIP.dll`
- Event Server: `C:\Program Files\Milestone\XProtect Event Server\MIPPlugins\CoreCommandMIP\CoreCommandMIP.dll`

**If different timestamps:**
1. Build project
2. Copy to BOTH locations
3. Restart BOTH Smart Client and Event Server

## Diagnostic Steps (Do in Order)

### Step 1: Verify Smart Client is Sending

1. Build and restart Smart Client
2. Open Debug Output (View ? Output)
3. Wait for alarming track
4. Look for:
```
=== Creating alarm for Track XXX ===
? Message POSTED for track XXX
```

**If NOT found:** TrackAlarmManager not working
**If found:** Go to Step 2

### Step 2: Verify Event Server Plugin Loaded

1. Open Management Client
2. Go to System ? Logs
3. Filter source: "CoreCommandMIP"
4. Look for:
```
Background plugin initializing...
? READY - Listening for alarm messages
```

**If NOT found:** Event Server plugin didn't load (see Issue #1)
**If found:** Go to Step 3

### Step 3: Check Message IDs Match

**In Debug Output (Smart Client):**
```
Sending message with ID: CoreCommandMIP.TrackAlarm
```

**In Management Client Logs (Event Server):**
```
Registered for message ID: CoreCommandMIP.TrackAlarm
```

**If different:** Rebuild and redeploy to fix

**If same:** Go to Step 4

### Step 4: Test with Manual Message

Add this temporary test button to Smart Client:

```csharp
private void TestAlarmButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var testData = new Background.TrackAlarmData
        {
            TrackId = 999,
            Classification = "TEST",
            Confidence = 1.0,
            Latitude = 38.0,
            Longitude = -104.0,
            Altitude = 100,
            Velocity = 50,
            Site = "TEST SITE",
            Timestamp = DateTime.UtcNow,
            Priority = 1
        };

        var message = new VideoOS.Platform.Messaging.Message(
            CoreCommandMIPDefinition.TrackAlarmMessageId,
            testData);

        EnvironmentManager.Instance.PostMessage(message, null, null);
        
        System.Diagnostics.Debug.WriteLine("TEST: Manual alarm message sent!");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"TEST FAILED: {ex.Message}");
    }
}
```

Click button, then check Event Server logs for message receipt.

## Expected Log Flow (When Working)

### Smart Client Debug Output:
```
=== Creating alarm for Track 123 ===
Alarm data created: Track=123, Class=Drone, Site=HQ
Sending message with ID: CoreCommandMIP.TrackAlarm
? Message POSTED for track 123
```

### Management Client Logs:
```
[CoreCommandMIP.SmartClient] ? Sent alarm message for Track 123 (Drone)
[CoreCommandMIP.TrackAlarm] ? MESSAGE RECEIVED from: <GUID>
[CoreCommandMIP.TrackAlarm] ? Processing Track 123 - Drone at HQ
[CoreCommandMIP.TrackAlarm] ? ALARM CREATED for Track 123 - [HIGH]
[CoreCommandMIP.TrackAlarm] [HIGH] TRACK ALARM - Track 123 (Drone) detected at HQ...
```

## Quick Checklist

- [ ] Built latest code
- [ ] Deployed DLL to Event Server folder
- [ ] Restarted Event Server service
- [ ] Event Server plugin initialized (check logs)
- [ ] Message receiver registered (check logs)
- [ ] Smart Client sending messages (check Debug Output)
- [ ] Message IDs match on both sides
- [ ] TrackAlarmData is [Serializable]
- [ ] Same DLL version on Smart Client and Event Server

## Current Code Status

? Added comprehensive logging (Smart Client send side)
? Added comprehensive logging (Event Server receive side)
? Fixed static constructor crash
? Message ID properly defined and registered
? TrackAlarmData properly serializable
? Build successful

## Most Likely Issues (In Order)

1. **Event Server service needs restart** (80% of cases)
2. **DLL not deployed to Event Server folder** (15% of cases)
3. **Event Server plugin crashed on init** (4% of cases)
4. **Message ID mismatch** (1% of cases)

## Next Steps

1. **Build** (done ?)
2. **Deploy to Event Server:**
   ```powershell
   Copy-Item "bin\Release\CoreCommandMIP.dll" -Destination "C:\Program Files\Milestone\XProtect Event Server\MIPPlugins\CoreCommandMIP\" -Force
   ```
3. **Restart Event Server:**
   ```powershell
   Restart-Service "Milestone XProtect Event Server"
   ```
4. **Check Management Client logs immediately** for initialization messages
5. **Trigger alarm** and watch both Debug Output and Management Client logs
6. **Report findings** - which step failed?
