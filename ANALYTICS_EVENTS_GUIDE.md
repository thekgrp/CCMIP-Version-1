# Milestone XProtect Analytics Event Integration Guide

## Overview
To create proper alarms in XProtect Smart Client that appear in the Alarm Manager (not just logs), you need to implement **Analytics Events** that register with the Recording Server.

## Architecture

```
CoreCommandMIP Plugin
    ?
Analytic Event Definition (TrackAlarmAnalyticsEvent.cs)
    ?
Recording Server Registration
    ?
Smart Client Alarm Manager
```

## What's Already Implemented

? **TrackAlarmManager.cs** - Deduplication logic and basic logging
? **SmartMapLocation** - Has `Alarming` and `Alerting` properties
? **TrackAlarmAnalyticsEvent.cs** - Event definition (just created)

## What's Still Needed

### 1. **Plugin Definition Updates** (CoreCommandMIPDefinition.cs)

The plugin must implement `IAnalyticsDefinition` interface:

```csharp
public class CoreCommandMIPDefinition : PluginDefinition, IAnalyticsDefinition
{
    // Existing code...
    
    public List<AnalyticsEventType> GetAnalyticsEventTypes()
    {
        return new List<AnalyticsEventType>
        {
            new AnalyticsEventType
            {
                Id = TrackAlarmAnalyticsEvent.EventTypeId,
                Name = TrackAlarmAnalyticsEvent.EventTypeName,
                DisplayName = TrackAlarmAnalyticsEvent.EventTypeDisplayName,
                Description = TrackAlarmAnalyticsEvent.EventTypeDescription,
                CustomFields = new List<CustomFieldDefinition>
                {
                    new CustomFieldDefinition("TrackId", "Track ID", CustomFieldType.Integer),
                    new CustomFieldDefinition("Classification", "Object Type", CustomFieldType.String),
                    new CustomFieldDefinition("Latitude", "Latitude", CustomFieldType.Double),
                    new CustomFieldDefinition("Longitude", "Longitude", CustomFieldType.Double),
                    new CustomFieldDefinition("Altitude", "Altitude (m)", CustomFieldType.Double),
                    new CustomFieldDefinition("Velocity", "Velocity (m/s)", CustomFieldType.Double),
                    new CustomFieldDefinition("Site", "Site Name", CustomFieldType.String),
                    new CustomFieldDefinition("Confidence", "Confidence", CustomFieldType.Double)
                }
            }
        };
    }
}
```

### 2. **Update TrackAlarmManager to Use Analytics Events**

Replace the logging approach with proper analytics events:

```csharp
private void CreateAlarmEvent(SmartMapLocation track)
{
    try
    {
        // Create analytics event
        var analyticsEvent = TrackAlarmAnalyticsEvent.CreateEvent(track, _siteName, _pluginId);
        
        // Send to XProtect
        var message = new VideoOS.Platform.Messaging.Message(
            MessageId.Server.NewAnalyticsEventCommand,
            analyticsEvent);
        
        EnvironmentManager.Instance.SendMessage(message, null, null);
        
        System.Diagnostics.Debug.WriteLine($"Created analytics alarm for track {track.TrackId}");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Failed to create alarm for track {track.TrackId}: {ex.Message}");
    }
}
```

### 3. **Initialize Analytics on Plugin Load**

In plugin initialization (likely in `CoreCommandMIPPlugin.Init()`):

```csharp
public override void Init()
{
    // Existing initialization...
    
    // Register analytics event types
    TrackAlarmAnalyticsEvent.RegisterEventType();
}
```

## Expected Result

Once implemented, track alarms will:

1. ? **Appear in Smart Client Alarm List**
   - Red/yellow indicator based on priority
   - Click to view full details
   - Acknowledge, resolve, or ignore options

2. ? **Show in Management Client**
   - Alarms tab
   - Can create rules based on these events
   - Can trigger actions (email, bookmarks, etc.)

3. ? **Support XProtect Rules**
   - "When Track Alarm event occurs..."
   - Filter by Track ID, Classification, Site, etc.
   - Trigger cameras, bookmarks, notifications

4. ? **Custom Field Filtering**
   - Filter alarms by Track ID
   - Filter by Classification (Drone, Vehicle, etc.)
   - Filter by Site name
   - Filter by Velocity/Altitude thresholds

## Alarm Appearance in Smart Client

```
????????????????????????????????????????????????????????????
? Alarm Manager                                            ?
????????????????????????????????????????????????????????????
? ?? Track 13528 - Drone                    [High]         ?
?    TestSite - 38.7866, -104.7886          10:36:17 AM    ?
?    Track ID: 13528, Type: Drone (85% confidence)         ?
?    [Acknowledge] [Resolve] [Ignore]                      ?
????????????????????????????????????????????????????????????
? ?? Track 6485 - Vehicle                   [Medium]       ?
?    MainSite - 45.5089, -122.7743          10:35:12 AM    ?
?    Track ID: 6485, Type: Vehicle (92% confidence)        ?
?    [Acknowledge] [Resolve] [Ignore]                      ?
????????????????????????????????????????????????????????????
```

## Benefits Over Simple Logging

| Feature | Logging | Analytics Events |
|---------|---------|------------------|
| Appears in Alarm List | ? | ? |
| Can Acknowledge/Resolve | ? | ? |
| Triggers Rules | ? | ? |
| Custom Fields/Filtering | ? | ? |
| Creates Bookmarks | ? | ? (via rules) |
| Sends Notifications | ? | ? (via rules) |
| Alarm History | ? | ? |
| Integration with Cameras | ? | ? (via rules) |

## Next Steps

1. **Update CoreCommandMIPDefinition.cs** to implement `IAnalyticsDefinition`
2. **Update TrackAlarmManager.cs** to use `TrackAlarmAnalyticsEvent.CreateEvent()`
3. **Test** by viewing Smart Client Alarm Manager when `"Alarming":true`
4. **Create Rules** in Management Client to handle track alarms

## Notes

- Analytics events persist in XProtect database
- Can query historical alarms
- Can export alarm reports
- Alarms can trigger recording on associated cameras
- Can integrate with third-party alarm systems via XProtect integration

## Documentation References

- XProtect MIP SDK Documentation
- `VideoOS.Platform.Data` namespace
- `IAnalyticsDefinition` interface
- `AnalyticsEvent` class
