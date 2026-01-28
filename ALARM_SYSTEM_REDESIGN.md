# CoreCommandMIP Alarm System - Proper Milestone Architecture

## ? What We Had (Incorrect)

```
Smart Client ? Message ? Event Server ? Log Entry
                                         ?
                                    Management Client Logs
                                    (NOT in Alarm Manager!)
```

**Problems:**
- Using `EnvironmentManager.Instance.Log()` - creates log entries only
- No alarms in Alarm Manager
- No way to select and highlight alarms
- Not following Milestone patterns

## ? Proper Milestone Pattern

```
Smart Client detects alarming track
         ?
Event Server sends User-Defined Event (with metadata: TrackId, Timestamp, Location)
         ?
Management Client Rule (Event ? Alarm)
         ?
Alarm Manager shows alarm
         ?
Operator selects alarm
         ?
Smart Client receives alarm selection notification
         ?
WebView2 highlights track on map + shows timestamp
```

## Architecture Components

### 1. Event Server Plugin (Background)
**Purpose:** Send user-defined events to XProtect

**What it does:**
- Receives track alarm messages from Smart Client
- Creates **User-Defined Events** (or Analytics Events)
- Includes metadata: TrackId, Classification, Coordinates, Timestamp
- XProtect's event system handles the rest

### 2. Management Client Configuration
**Purpose:** Define rules that convert events to alarms

**What operators do:**
- Create Rule: "When CoreCommandMIP Track Alarm event occurs ? Create Alarm"
- Configure alarm properties: Priority, message template, sound
- Define alarm acknowledgement workflow

### 3. Smart Client Plugin
**Purpose:** Respond to alarm selections

**What it does:**
- Subscribes to alarm selection events from Alarm Manager
- When alarm selected: Extract TrackId and Timestamp from alarm metadata
- Tell WebView2 to highlight the track
- Optionally: Zoom to track location, show track history at that time

## No Recording Server Plugin

**Correct!** We don't need anything on the Recording Server. The Event Server is the "events/alarms/messaging hub" in Milestone.

## Implementation Changes Needed

### A. Event Server - Send User-Defined Events

**File:** `Background/TrackAlarmEventHandler.cs`

**Change from:**
```csharp
EnvironmentManager.Instance.Log(isError, "CoreCommandMIP.TrackAlarm", message, null);
```

**Change to:**
```csharp
// Create user-defined event
var eventHeader = new EventHeader
{
    ID = Guid.NewGuid(),
    Timestamp = alarmData.Timestamp,
    Name = "Track Alarm",
    Message = $"Track {alarmData.TrackId} ({alarmData.Classification}) detected at {alarmData.Site}",
    Source = new FQID(pluginId),
    Priority = alarmData.Priority,
    CustomTag = alarmData.TrackId.ToString() // For correlation
};

// Add metadata
eventHeader.AddAnalyticsMetadata("TrackId", alarmData.TrackId.ToString());
eventHeader.AddAnalyticsMetadata("Classification", alarmData.Classification);
eventHeader.AddAnalyticsMetadata("Latitude", alarmData.Latitude.ToString());
eventHeader.AddAnalyticsMetadata("Longitude", alarmData.Longitude.ToString());
eventHeader.AddAnalyticsMetadata("Site", alarmData.Site);

// Send event
EnvironmentManager.Instance.SendMessage(
    new Message(MessageId.Server.NewUserDefinedEvent, eventHeader));
```

### B. Management Client Rule Configuration

**Operator configures in Management Client:**

1. **Create Event Type (if needed):**
   - System ? Events ? Add User-Defined Event
   - Name: "CoreCommandMIP Track Alarm"
   - Source: CoreCommandMIP plugin

2. **Create Rule:**
   - Rules & Events ? Add Rule
   - **Trigger:** Event "CoreCommandMIP Track Alarm"
   - **Filter:** Priority High or Medium (optional)
   - **Action:** Create Alarm
   - **Alarm Properties:**
     - Name: "Track Alarm: {Classification}"
     - Message: Event message
     - Priority: Use event priority
     - Category: "Perimeter Security" (or custom)

3. **Result:** Alarms appear in Alarm Manager when tracks alarm

### C. Smart Client - Subscribe to Alarm Selection

**File:** `Client/CoreCommandMIPViewItemWpfUserControl.xaml.cs`

**Add alarm selection subscription:**

```csharp
private object _alarmSelectionReceiver;

private void SubscribeToAlarmSelection()
{
    _alarmSelectionReceiver = EnvironmentManager.Instance.RegisterReceiver(
        new MessageReceiver(HandleAlarmSelection),
        new MessageIdFilter(MessageId.SmartClient.SelectedAlarmChangedIndication));
}

private object HandleAlarmSelection(Message message, FQID destination, FQID source)
{
    try
    {
        if (message?.Data is AlarmSelectedEventArgs alarmData)
        {
            // Extract our metadata from alarm
            var trackIdStr = alarmData.EventHeader?.CustomTag;
            if (long.TryParse(trackIdStr, out long trackId))
            {
                // Highlight track on map
                HighlightTrackOnMap(trackId, alarmData.EventHeader.Timestamp);
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Alarm selection handling failed: {ex.Message}");
    }
    return null;
}

private async void HighlightTrackOnMap(long trackId, DateTime timestamp)
{
    try
    {
        var script = $@"
            console.log('Highlighting track {trackId} at {timestamp:O}');
            
            // Find track layer
            if (trackLayers[{trackId}]) {{
                var layer = trackLayers[{trackId}];
                
                // Zoom to track
                map.setView(layer.marker.getLatLng(), 18);
                
                // Flash/highlight track
                var originalColor = layer.line.options.color;
                layer.line.setStyle({{ color: '#FFFF00', weight: 5 }});
                layer.marker.setZIndexOffset(1000); // Bring to front
                
                // Pulse animation
                setTimeout(() => {{
                    layer.line.setStyle({{ color: originalColor, weight: 3 }});
                    layer.marker.setZIndexOffset(0);
                }}, 3000);
                
                // Open popup
                layer.marker.openPopup();
            }} else {{
                console.log('Track {trackId} not currently visible on map');
            }}
        ";
        
        await _mapView.ExecuteScriptAsync(script);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Map highlight failed: {ex.Message}");
    }
}
```

### D. Unsubscribe on Close

```csharp
public override void Close()
{
    if (_alarmSelectionReceiver != null)
    {
        EnvironmentManager.Instance.UnRegisterReceiver(_alarmSelectionReceiver);
        _alarmSelectionReceiver = null;
    }
    // ... existing close code
}
```

## Event Metadata Schema

**What we encode in events:**

```csharp
public class TrackAlarmEventMetadata
{
    public long TrackId { get; set; }
    public string Classification { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double Velocity { get; set; }
    public string Site { get; set; }
    public DateTime Timestamp { get; set; }
    public int Priority { get; set; }
    public string[] Sources { get; set; } // Sensor sources
}
```

**Store as:**
- `EventHeader.CustomTag` ? TrackId (for quick lookup)
- `EventHeader.AddAnalyticsMetadata()` ? All other fields

## Alarm Manager Integration

### What Operators See:

**Alarm List:**
```
Priority | Time     | Category           | Message
---------|----------|--------------------|-----------------------------------------
HIGH     | 14:23:15 | Perimeter Security | Track 123 (Drone) detected at HQ East
MEDIUM   | 14:22:48 | Perimeter Security | Track 124 (Vehicle) detected at Gate 2
HIGH     | 14:21:32 | Perimeter Security | Track 125 (Person) detected at Building A
```

**Alarm Details Panel:**
```
Event: CoreCommandMIP Track Alarm
Time: 2024-01-27 14:23:15
Source: CoreCommandMIP Plugin
Track ID: 123
Classification: Drone
Location: 38.7866°, -104.7886°
Altitude: 1500.0m
Velocity: 25.5 m/s
Site: HQ East
Sensors: Radar-1, Camera-5
```

### Operator Actions:
- **Click alarm** ? Your map view highlights the track
- **Acknowledge** ? Standard Milestone workflow
- **In Progress** ? Standard Milestone workflow
- **Close** ? Standard Milestone workflow

## Playback Integration (Future)

When alarm selected, optionally fetch historical data:

```csharp
private async void ShowAlarmPlayback(long trackId, DateTime alarmTime)
{
    // Fetch track history around alarm time
    var historyStart = alarmTime.AddMinutes(-5);
    var historyEnd = alarmTime.AddMinutes(5);
    
    var historicalTracks = await _dataProvider.GetTrackHistoryAsync(
        trackId, historyStart, historyEnd);
    
    // Send to map for playback visualization
    var script = BuildPlaybackScript(historicalTracks, alarmTime);
    await _mapView.ExecuteScriptAsync(script);
}
```

## Benefits of This Architecture

? **Standard Milestone Integration**
- Uses proper event/alarm/rule system
- Alarms appear in Alarm Manager natively
- All standard alarm workflows work

? **Operator Experience**
- Familiar Alarm Manager interface
- Click alarm ? See on map instantly
- Standard acknowledge/close workflow

? **Flexibility**
- Management Client can customize:
  - Which classifications trigger alarms
  - Alarm priorities
  - Notification sounds
  - Who gets notified
  - Acknowledgement requirements

? **Scalability**
- XProtect handles alarm distribution
- Multi-client support built-in
- Alarm history tracked by XProtect

? **Correlation**
- Can link alarms to cameras (if available)
- Can link alarms to maps (your plugin)
- Can link alarms to access control events
- All through Milestone's event correlation

## Migration Steps

### 1. Remove Incorrect Code
- Remove `EnvironmentManager.Instance.Log()` calls
- Remove log-based alarm creation

### 2. Implement User-Defined Events
- Update `TrackAlarmEventHandler.cs`
- Add metadata encoding
- Send proper events

### 3. Update Smart Client
- Add alarm selection subscription
- Implement map highlighting
- Add unsubscribe on close

### 4. Test with Management Client
- Configure event type
- Create rule (Event ? Alarm)
- Test alarm appears in Alarm Manager

### 5. Test Smart Client Highlighting
- Select alarm in Alarm Manager
- Verify map highlights correct track
- Verify zoom and popup work

## Documentation for Operators

**Management Client Setup Guide:**

1. **Enable CoreCommandMIP Events:**
   - System ? Events ? User-Defined Events
   - Source: CoreCommandMIP
   - Ensure enabled

2. **Create Alarm Rule:**
   - Rules & Events ? Add Rule
   - Name: "CoreCommandMIP High Priority Track Alarms"
   - Trigger: Event "Track Alarm" from CoreCommandMIP
   - Filter: Priority = High OR Medium
   - Action: Create Alarm
   - Alarm Priority: Use event priority

3. **Configure Alarm Properties:**
   - Alarm message template: "{Message}"
   - Category: "Perimeter Security"
   - Sound: (optional)
   - Acknowledgement required: Yes

4. **Test:**
   - Wait for alarming track from remote server
   - Alarm should appear in Alarm Manager
   - Click alarm to see on map

## Current Status

? **To Remove:**
- Log-based alarm creation
- Direct log entry writing
- Recording Server concepts

? **To Implement:**
- User-Defined Event sending
- Event metadata encoding
- Alarm selection subscription
- Map highlighting on alarm selection

?? **To Configure (by operators):**
- Event type registration
- Rules (Event ? Alarm)
- Alarm properties

## Next Steps

1. **I'll update the code** to implement proper user-defined events
2. **Remove recording server references**
3. **Add alarm selection highlighting**
4. **Create operator setup guide**
5. **Test complete workflow**

Ready to proceed with the implementation?
