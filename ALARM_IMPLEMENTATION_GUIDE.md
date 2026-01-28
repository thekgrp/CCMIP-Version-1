# Implementation Guide - Proper Milestone Alarm Integration

## Summary of Changes

### ? Remove:
- Recording Server plugin references
- Direct log entry creation for alarms
- `EnvironmentManager.Instance.Log()` for alarms

### ? Add:
- User-Defined Event creation in Event Server
- Alarm selection subscription in Smart Client  
- Map highlighting when alarm selected

## Code Changes Required

### 1. Background\TrackAlarmEventHandler.cs

**Add plugin FQID field:**
```csharp
private readonly FQID _pluginFqid;

public TrackAlarmEventHandler()
{
    _pluginFqid = new FQID(new ServerId(ServerType.MIPPluginServer, ""), 
                           new Guid(), 
                           FolderType.No, 
                           CoreCommandMIPDefinition.CoreCommandMIPPluginId);
}
```

**Replace ProcessTrackAlarm() method:**
```csharp
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
        // Create User-Defined Event (proper Milestone pattern)
        var eventHeader = new EventHeader
        {
            ID = Guid.NewGuid(),
            Timestamp = alarmData.Timestamp,
            Class = "User-defined",
            Type = 1, // User-defined event type
            Priority = alarmData.Priority,
            Source = _pluginFqid,
            Name = "Track Alarm",
            Message = $"{GetPriorityText(alarmData.Priority)} Track {alarmData.TrackId} ({alarmData.Classification}) detected at {alarmData.Site}",
            CustomTag = alarmData.TrackId.ToString() // For correlation in Smart Client
        };

        // Add metadata for Smart Client to use
        eventHeader.AddAnalyticsMetadata("TrackId", alarmData.TrackId.ToString());
        eventHeader.AddAnalyticsMetadata("Classification", alarmData.Classification);
        eventHeader.AddAnalyticsMetadata("Latitude", alarmData.Latitude.ToString("F6"));
        eventHeader.AddAnalyticsMetadata("Longitude", alarmData.Longitude.ToString("F6"));
        eventHeader.AddAnalyticsMetadata("Altitude", alarmData.Altitude.ToString("F1"));
        eventHeader.AddAnalyticsMetadata("Velocity", alarmData.Velocity.ToString("F1"));
        eventHeader.AddAnalyticsMetadata("Confidence", alarmData.Confidence.ToString("F2"));
        eventHeader.AddAnalyticsMetadata("Site", alarmData.Site);

        // Send event to XProtect
        var eventMessage = new Message(MessageId.Server.NewUserDefinedEvent, eventHeader);
        EnvironmentManager.Instance.SendMessage(eventMessage);

        LogBoth(false, $"? USER-DEFINED EVENT sent for Track {alarmData.TrackId} - {GetPriorityText(alarmData.Priority)}");
        LogBoth(false, $"  ? Event will appear in Management Client rules and can create alarms");
    }
    catch (Exception ex)
    {
        LogBoth(true, $"? Failed to send event for Track {alarmData.TrackId}: {ex.Message}");
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
```

### 2. Client\CoreCommandMIPViewItemWpfUserControl.xaml.cs

**Add fields:**
```csharp
private object _alarmSelectionReceiver;
```

**Add to Init() method:**
```csharp
// Subscribe to alarm selection events
SubscribeToAlarmSelection();
```

**Add new methods:**
```csharp
/// <summary>
/// Subscribe to Alarm Manager selection events
/// </summary>
private void SubscribeToAlarmSelection()
{
    try
    {
        _alarmSelectionReceiver = EnvironmentManager.Instance.RegisterReceiver(
            new MessageReceiver(HandleAlarmSelection),
            new MessageIdFilter(MessageId.SmartClient.SelectedAlarmChangedIndication));
        
        System.Diagnostics.Debug.WriteLine("? Subscribed to alarm selection events");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"? Failed to subscribe to alarm events: {ex.Message}");
    }
}

/// <summary>
/// Handle alarm selection from Alarm Manager
/// </summary>
private object HandleAlarmSelection(Message message, FQID destination, FQID source)
{
    try
    {
        System.Diagnostics.Debug.WriteLine("? Alarm selection event received");
        
        if (message?.Data is AlarmSelectedEventArgs alarmArgs)
        {
            var eventHeader = alarmArgs.EventHeader;
            if (eventHeader != null && eventHeader.Name == "Track Alarm")
            {
                // Extract track ID from custom tag
                if (long.TryParse(eventHeader.CustomTag, out long trackId))
                {
                    System.Diagnostics.Debug.WriteLine($"? Highlighting Track {trackId} from alarm selection");
                    
                    // Highlight track on map
                    _ = HighlightTrackOnMapAsync(trackId, eventHeader.Timestamp);
                }
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"? Alarm selection handling failed: {ex.Message}");
    }
    
    return null;
}

/// <summary>
/// Highlight a track on the map (zoom, flash, open popup)
/// </summary>
private async System.Threading.Tasks.Task HighlightTrackOnMapAsync(long trackId, DateTime timestamp)
{
    try
    {
        // Build JavaScript to highlight track
        var script = $@"
            (function() {{
                console.log('=== ALARM SELECTION: Highlighting Track {trackId} ===');
                
                // Check if track exists on map
                if (!trackLayers[{trackId}]) {{
                    console.log('? Track {trackId} not currently visible on map');
                    return;
                }}
                
                var layer = trackLayers[{trackId}];
                var position = layer.marker.getLatLng();
                
                console.log('? Track {trackId} found, highlighting...');
                
                // Zoom to track
                map.setView(position, 18, {{ animate: true, duration: 1 }});
                
                // Save original styling
                var originalLineColor = layer.line.options.color;
                var originalLineWeight = layer.line.options.weight;
                
                // Highlight with yellow flash
                layer.line.setStyle({{ 
                    color: '#FFFF00', 
                    weight: 6,
                    opacity: 1
                }});
                
                // Bring marker to front
                if (layer.marker.setZIndexOffset) {{
                    layer.marker.setZIndexOffset(1000);
                }}
                
                // Open popup immediately
                layer.marker.openPopup();
                
                // Pulse effect - multiple flashes
                var pulseCount = 0;
                var pulseInterval = setInterval(function() {{
                    pulseCount++;
                    if (pulseCount % 2 === 0) {{
                        layer.line.setStyle({{ color: '#FFFF00', weight: 6 }});
                    }} else {{
                        layer.line.setStyle({{ color: originalLineColor, weight: 5 }});
                    }}
                    
                    if (pulseCount >= 6) {{
                        clearInterval(pulseInterval);
                        // Restore original styling
                        layer.line.setStyle({{ 
                            color: originalLineColor, 
                            weight: originalLineWeight 
                        }});
                        if (layer.marker.setZIndexOffset) {{
                            layer.marker.setZIndexOffset(0);
                        }}
                    }}
                }}, 300);
                
                console.log('? Track {trackId} highlighted');
            }})();
        ";
        
        await _mapView.ExecuteScriptAsync(script);
        System.Diagnostics.Debug.WriteLine($"? Map highlight script executed for Track {trackId}");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"? Map highlighting failed: {ex.Message}");
    }
}
```

**Update Close() method:**
```csharp
public override void Close()
{
    // Unsubscribe from alarm selection
    if (_alarmSelectionReceiver != null)
    {
        EnvironmentManager.Instance.UnRegisterReceiver(_alarmSelectionReceiver);
        _alarmSelectionReceiver = null;
        System.Diagnostics.Debug.WriteLine("? Unsubscribed from alarm selection events");
    }
    
    // ... existing close code ...
}
```

### 3. Background\CoreCommandMIPBackgroundPlugin.cs

**Update Init() to pass plugin ID:**
```csharp
public override void Init()
{
    _stop = false;
    
    EnvironmentManager.Instance.Log(
        false,
        "CoreCommandMIP.Background",
        "Background plugin initializing...",
        null);
    
    System.Diagnostics.Debug.WriteLine("=== CoreCommandMIP Background Plugin Init() called ===");
    
    // Initialize Track Alarm Event Handler (pass plugin ID)
    try
    {
        _alarmEventHandler = new TrackAlarmEventHandler(); // Updated constructor
        _alarmEventHandler.Init();
        
        EnvironmentManager.Instance.Log(
            false,
            "CoreCommandMIP.Background",
            "Track Alarm Event Handler initialized - Sending User-Defined Events",
            null);
            
        System.Diagnostics.Debug.WriteLine("? Track Alarm Handler will send User-Defined Events");
    }
    catch (Exception ex)
    {
        EnvironmentManager.Instance.Log(
            true,
            "CoreCommandMIP.Background",
            $"FAILED to initialize Track Alarm Event Handler: {ex.Message}",
            null);
            
        System.Diagnostics.Debug.WriteLine($"? Track Alarm Handler init failed: {ex}");
    }
    
    _thread = new Thread(new ThreadStart(Run));
    _thread.Name = "CoreCommandMIP Background Thread";
    _thread.Start();
    
    EnvironmentManager.Instance.Log(
        false,
        "CoreCommandMIP.Background",
        "Background plugin started successfully",
        null);
}
```

## Management Client Configuration

### Step 1: Verify User-Defined Events Enabled

1. Open **Management Client**
2. Go to **System Configuration** ? **Events**
3. Find **User-Defined Events**
4. Ensure **CoreCommandMIP** source is listed and enabled

### Step 2: Create Alarm Rule

1. Go to **Rules and Events**
2. Click **Add Rule**
3. Configure:
   - **Name:** "CoreCommandMIP Track Alarms"
   - **Description:** "Generate alarms for high-priority track detections"
   
4. **Event Tab:**
   - **Event:** User-Defined Event
   - **Source:** CoreCommandMIP
   - **Name:** "Track Alarm"
   - **Filter:** Priority ? 5 (High and Medium)

5. **Action Tab:**
   - **Action:** Create Alarm
   - **Alarm Message:** Use event message
   - **Alarm Priority:** Use event priority
   - **Category:** "Perimeter Security" (or create custom)

6. **Save Rule**

### Step 3: Test

1. Wait for alarming track from remote server
2. Check **Alarm Manager** in Smart Client
3. Alarm should appear with track information
4. Click alarm ? Map should highlight the track

## Testing Checklist

- [ ] Event Server plugin loads without errors
- [ ] User-Defined Event sent when track alarms
- [ ] Event visible in Management Client logs
- [ ] Rule configured (Event ? Alarm)
- [ ] Alarm appears in Smart Client Alarm Manager
- [ ] Clicking alarm highlights track on map
- [ ] Map zooms to track
- [ ] Track flashes/pulses for visibility
- [ ] Popup opens showing track details
- [ ] Alarm can be acknowledged
- [ ] Alarm workflow (In Progress, Close) works

## Verification Commands

**Check Event Server logs:**
```
Management Client ? System ? Logs ? Filter: "CoreCommandMIP"
```

Look for:
```
? USER-DEFINED EVENT sent for Track 123 - [HIGH]
? Event will appear in Management Client rules and can create alarms
```

**Check Smart Client Debug Output:**
```
? Alarm selection event received
? Highlighting Track 123 from alarm selection
? Map highlight script executed for Track 123
```

## Benefits

? **Standard Milestone Integration**
- Uses proper event ? rule ? alarm pattern
- Alarms appear in Alarm Manager natively
- All standard workflows supported

? **Operator Experience**
- Click alarm to see location on map instantly
- Standard acknowledge/in-progress/close workflow
- Familiar Alarm Manager interface

? **Flexibility**
- Management Client controls which events create alarms
- Can customize alarm priorities, sounds, notifications
- Can filter by classification, priority, site, etc.

? **No Recording Server Plugin Needed**
- Event Server is the proper hub for events/alarms
- Simpler architecture
- Standard Milestone pattern

## Next Steps

1. **Apply code changes** above
2. **Build and deploy** to Event Server
3. **Restart Event Server** service
4. **Configure rule** in Management Client
5. **Test** with alarming track
6. **Verify** alarm appears and highlights work

Ready to implement?
