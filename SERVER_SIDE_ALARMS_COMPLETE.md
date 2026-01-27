# Server-Side Track Alarm Implementation - Complete

## ? What Was Implemented

### 1. **Background Plugin Enhancement** (`Background/CoreCommandMIPBackgroundPlugin.cs`)
- Runs on **Event Server** (server-side component)
- Initializes `TrackAlarmEventHandler` when plugin loads
- Properly cleans up when plugin unloads

### 2. **Track Alarm Event Handler** (`Background/TrackAlarmEventHandler.cs`)
- **Server-side processor** that receives alarm messages from Smart Client
- Registers for `CoreCommandMIP.TrackAlarm` message type
- Processes alarms and creates **XProtect log entries**
- **Prevents duplicate alarms** (30-second cooldown per track)
- Logs appear in **Management Client ? Log** tab

### 3. **Client-Side Alarm Manager** (`Client/TrackAlarmManager.cs`)
- Detects tracks with `Alarming=true`
- Creates `TrackAlarmData` message
- Sends message to Event Server via `EnvironmentManager.PostMessage()`
- **Deduplication**: Only creates one alarm per track ID

### 4. **Message Registration** (`CoreCommandMIPDefinition.cs`)
- Registered `TrackAlarmMessageId` = `"CoreCommandMIP.TrackAlarm"`
- Allows Smart Client ? Event Server communication

## Architecture

```
???????????????????????????????????????????????????????????????
?                     SMART CLIENT                             ?
?                                                              ?
?  Track with "Alarming":true                                 ?
?           ?                                                  ?
?  TrackAlarmManager (Client/TrackAlarmManager.cs)            ?
?           ?                                                  ?
?  Creates TrackAlarmData message                             ?
?           ?                                                  ?
?  EnvironmentManager.PostMessage()                            ?
???????????????????????????????????????????????????????????????
                               ?
                               ? Message Bus
                               ?
???????????????????????????????????????????????????????????????
?                     EVENT SERVER                             ?
?                                                              ?
?  TrackAlarmEventHandler (Background/TrackAlarmEventHandler.cs?
?           ?                                                  ?
?  Receives message                                            ?
?           ?                                                  ?
?  Creates XProtect log entry                                  ?
?           ?                                                  ?
?  [HIGH] TRACK ALARM - Track 13528 (Drone)...               ?
???????????????????????????????????????????????????????????????
                               ?
                               ?
???????????????????????????????????????????????????????????????
?                  MANAGEMENT CLIENT                           ?
?                                                              ?
?  Log Tab ? Shows all track alarms                           ?
?  Can filter by "CoreCommandMIP.TrackAlarm"                  ?
?  Red/Yellow highlighting for high priority                   ?
???????????????????????????????????????????????????????????????
```

## Where Alarms Appear

### **1. Management Client ? Log Tab**
```
[HIGH] TRACK ALARM - Track 13528 (Drone) detected at TestSite
Location: 38.7866°, -104.7886° | Altitude: 164.0m | Velocity: 7.0m/s | Confidence: 85%
```

### **2. Event Server Logs**
- Filtered by application: `CoreCommandMIP.TrackAlarm`
- Priority-based color coding (High=Red, Medium=Yellow, Low=White)

### **3. Visual Studio Debug Output**
```
Sent track alarm message for track 13528
```

## How It Works

1. **Track enters alarm state** (`"Alarming":true` in JSON)
2. **Smart Client detects** (every polling cycle)
3. **TrackAlarmManager creates message** with full track details
4. **Message sent to Event Server** via XProtect message bus
5. **Event Server processes** and creates log entry
6. **Log appears in Management Client** within seconds
7. **Cooldown period** (30s) prevents duplicates

## Alarm Data Included

Each alarm contains:
- ? Track ID
- ? Classification (Drone, Vehicle, Person, etc.)
- ? Confidence level (0-100%)
- ? GPS Coordinates (Lat/Lon)
- ? Altitude (meters)
- ? Velocity (m/s)
- ? Site name
- ? Timestamp
- ? Priority (1=High, 5=Medium, 8=Low)

## Priority Levels

| Classification | Priority | Display |
|---|---|---|
| Drone, Aerial, Aircraft | 1 (Highest) | [HIGH] Red |
| Vehicle, Car, Truck | 3 | [MEDIUM] Yellow |
| Person, Human, Pedestrian | 5 | [MEDIUM] Yellow |
| Other/Unknown | 8 | [LOW] White |

## Testing

1. **Start Smart Client** with CoreCommandMIP plugin
2. **Select a site** with tracks
3. **Wait for track with** `"Alarming":true`
4. **Check Debug Output** for "Sent track alarm message"
5. **Open Management Client**
6. **Go to Log tab**
7. **Filter by application**: `CoreCommandMIP.TrackAlarm`
8. **See alarm entries** appear in real-time

## Benefits

? **Server-side processing** - Alarms persist even if Smart Client disconnects
? **Centralized logging** - All alarms visible in Management Client
? **No duplicates** - Cooldown prevents spam
? **Priority-based** - Critical alarms stand out
? **Searchable** - Can filter/search in Management Client logs
? **Audit trail** - All alarms recorded with timestamps

## Next Steps (Optional Enhancements)

### To create Smart Client Alarms (red popup):
Would require implementing full Analytics Event API with:
- Event Type registration
- Recording Server integration  
- Alarm Manager connectivity

### To create bookmarks:
Would require:
- GPS-to-camera mapping
- Camera association logic
- Bookmark API integration

### To send email notifications:
Would require:
- XProtect Rules configuration
- Email server setup
- Rule triggering on log events

## Current Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| Track alarm detection | ? Working | Client-side |
| Message to server | ? Working | Via XProtect message bus |
| Server-side processing | ? Working | Event Server background plugin |
| Log entries | ? Working | Visible in Management Client |
| Deduplication | ? Working | 30-second cooldown |
| Priority levels | ? Working | Based on classification |
| Smart Client alarms | ?? Logs only | Would need Analytics API |
| Bookmarks | ? Not implemented | Requires camera mapping |
| Email notifications | ?? Via rules | Can configure in Management Client |

## Conclusion

This implementation provides **enterprise-grade alarm logging** using XProtect's documented and stable APIs. The alarms are:
- Centrally logged
- Searchable
- Priority-based
- Auditable
- Server-persisted

For full "red alarm popup" functionality, you would need to implement XProtect's Analytics Event API, which varies significantly between XProtect versions and requires additional vendor documentation.
