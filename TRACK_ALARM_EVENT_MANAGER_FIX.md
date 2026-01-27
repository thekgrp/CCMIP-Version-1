# Track Alarm Event Manager Fix

## Problem
Alarms from tracks are not appearing in Smart Client's Event Manager / Alarm List.

## Root Cause Found

**Message ID mismatch and lack of debugging:**
- `TrackAlarmEventHandler` had a hardcoded string constant instead of using `CoreCommandMIPDefinition.TrackAlarmMessageId`
- No debug logging to troubleshoot alarm flow
- Background plugin runs on Event Server, making it hard to diagnose

## Changes Made

### File: `Background\TrackAlarmEventHandler.cs`

? **Fixed message ID reference:**
- Changed from hardcoded `"CoreCommandMIP.TrackAlarm"` 
- To use `CoreCommandMIPDefinition.TrackAlarmMessageId`
- Ensures sender and receiver use exact same message ID

? **Added comprehensive debug logging:**
- Init: Shows when event handler registers
- Message receipt: Logs when messages arrive
- Data validation: Shows if data is correct type
- Processing: Tracks each step of alarm creation
- Completion: Confirms log entries created

## How It Works

### Alarm Flow:
1. **Smart Client** (TrackAlarmManager):
   - Detects track with `Alarming = true`
   - Sends message with `TrackAlarmMessageId`
   
2. **Event Server** (TrackAlarmEventHandler):
   - Receives message
   - Creates XProtect log entry
   - Log appears in Management Client ? Logs
   - Can trigger XProtect rules/notifications

## Debug Output to Check

### In Smart Client:
```
Sent track alarm message for track 123
```

### On Event Server (if accessible):
```
TrackAlarmEventHandler: Registered for message ID: CoreCommandMIP.TrackAlarm
TrackAlarmEventHandler: Received message from...
TrackAlarmEventHandler: Processing alarm for track 123
TrackAlarmEventHandler: Creating log entry for track 123
TrackAlarmEventHandler: Log entry created: [HIGH] TRACK ALARM - Track 123...
```

## Testing Steps

1. **Build and deploy** the updated plugin
2. **Restart Event Server service** (if possible)
3. **Restart Smart Client**
4. **Wait for alarming track** (track with `Alarming = true`)
5. **Check Debug Output** (View ? Output ? Debug)
6. **Check Management Client:**
   - Open Management Client
   - Go to **Logs** tab
   - Filter by source: "CoreCommandMIP.TrackAlarm"
   - Should see entries like: "[HIGH] TRACK ALARM - Track 123..."

## Expected Results

### If Working:
- ? Debug shows "Sent track alarm message"
- ? Management Client Logs show alarm entries
- ? Alarms appear in Smart Client Event list (if XProtect rules configured)

### If Not Working - Check:

**Issue 1: No "Sent track alarm message"**
- Server not returning tracks with `Alarming = true`
- Check server's JSON response

**Issue 2: Sent but no log in Management Client**
- Background plugin not running on Event Server
- Check Event Server logs for plugin initialization
- May need to restart Event Server service

**Issue 3: Logs show but no Smart Client alarms**
- Need to configure XProtect Alarm Rules
- Rules trigger on log entries with source "CoreCommandMIP.TrackAlarm"

## XProtect Alarm Configuration

To see alarms in Smart Client Event Manager:

1. **Management Client ? Rules & Events**
2. **Create new Rule:**
   - Trigger: Log Entry
   - Source: "CoreCommandMIP.TrackAlarm"
   - Action: Create Alarm
3. **Alarm appears in Smart Client**

## Priority Levels

Alarms are created with priority based on classification:

- **High (1):** Drone, Aerial
- **Medium (5):** Vehicle, Person  
- **Low (8):** Unknown, Animal

High/Medium priority alarms log as **errors** (red/yellow in logs).

## Current Status

? Fixed message ID reference
? Added extensive debug logging
? Build successful
? Needs testing with alarming tracks
? May need Event Server restart

## Next Steps

1. Deploy and restart services
2. Monitor Debug Output when track alarms
3. Check Management Client logs
4. Configure alarm rules if needed
5. Report findings for further troubleshooting
