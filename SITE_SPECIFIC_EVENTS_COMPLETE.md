# ? SITE-SPECIFIC EVENT NAMES IMPLEMENTED

## The Problem
User-Defined Events and Alarm Definitions were being created with generic names:
- ? "C2.Alert" (shared by all sites)
- ? "C2.Alarm" (shared by all sites)

This meant:
- Multiple C2 sites would trigger the same events
- No way to distinguish which site triggered which alarm
- Couldn't route alarms to site-specific cameras
- Operators couldn't filter by site

## The Solution
Events and Alarms are now **site-specific**:
- ? "C2.Alert - Site Alpha"
- ? "C2.Alarm - Site Alpha"
- ? "C2.Alert - Site Bravo"
- ? "C2.Alarm - Site Bravo"

## What Changed

### 1. Admin UI - Alarm Creation
**File:** `Admin\CoreCommandMIPUserControlTabbed.cs`

**Before:**
```csharp
udeName: "C2.Alert",
alarmDefinitionName: "C2 Alert Alarm",
```

**After:**
```csharp
var siteName = textBoxName.Text?.Trim();
if (string.IsNullOrWhiteSpace(siteName))
    siteName = "Unnamed Site";

udeName: $"C2.Alert - {siteName}",
alarmDefinitionName: $"C2 Alert - {siteName}",
```

### 2. Event Server - Event Triggering
**File:** `Background\TrackAlarmEventHandler.cs`

**Before:**
```csharp
EventType = alarmData.Priority <= 2 
    ? EventDefinitionHelper.C2AlarmEventName   // "C2.Alarm"
    : EventDefinitionHelper.C2AlertEventName,  // "C2.Alert"
```

**After:**
```csharp
var alertEventName = $"C2.Alert - {alarmData.Site}";
var alarmEventName = $"C2.Alarm - {alarmData.Site}";

EventType = alarmData.Priority <= 2 
    ? alarmEventName   // "C2.Alarm - Site Alpha"
    : alertEventName,  // "C2.Alert - Site Alpha"
```

### 3. Configuration Grid
**File:** `Admin\CoreCommandMIPUserControlTabbed.cs`

**Before:**
```
?? Recommended Alarm Definitions ???????
? C2 Alert Alarm | C2.Alert | Medium   ?
? C2 Alarm       | C2.Alarm | High     ?
????????????????????????????????????????
```

**After:**
```
?? Recommended Alarm Definitions ?????????????????
? C2 Alert - Site Alpha | C2.Alert | Medium     ?
? C2 Alarm - Site Alpha | C2.Alarm | High       ?
??????????????????????????????????????????????????
```

## Benefits

### ? Multi-Site Support
Each C2 site gets its own events and alarms:
- Site Alpha: C2.Alert - Site Alpha, C2.Alarm - Site Alpha
- Site Bravo: C2.Alert - Site Bravo, C2.Alarm - Site Bravo
- Site Charlie: C2.Alert - Site Charlie, C2.Alarm - Site Charlie

### ? Site-Specific Routing
Each alarm can be configured independently:
- Site Alpha alarms ? Alpha cameras
- Site Bravo alarms ? Bravo cameras
- Different operators can monitor different sites

### ? Clear Identification
Operators see which site triggered the alarm:
- "C2 Alarm - Site Alpha" (clear!)
- vs "C2.Alarm" (which site?)

### ? Independent Configuration
Each site can have different:
- Camera associations
- Priority levels (if customized)
- Alarm routing rules
- Notification settings

## Workflow

### 1. Configure First Site
```
Site Name: "Site Alpha"
Cameras: Camera 1, Camera 2
Click "Apply Recommended Wiring"
```

**Creates:**
- Event: C2.Alert - Site Alpha
- Event: C2.Alarm - Site Alpha
- Alarm: C2 Alert - Site Alpha (linked to C2.Alert - Site Alpha)
- Alarm: C2 Alarm - Site Alpha (linked to C2.Alarm - Site Alpha)

### 2. Configure Second Site
```
Site Name: "Site Bravo"
Cameras: Camera 3, Camera 4
Click "Apply Recommended Wiring"
```

**Creates:**
- Event: C2.Alert - Site Bravo
- Event: C2.Alarm - Site Bravo
- Alarm: C2 Alert - Site Bravo (linked to C2.Alert - Site Bravo)
- Alarm: C2 Alarm - Site Bravo (linked to C2.Alarm - Site Bravo)

### 3. Track Alarm Triggered
When a track alarm is sent from Smart Client:
```csharp
TrackAlarmData {
    Site = "Site Alpha",
    Priority = 1,  // High
    ...
}
```

**Event Server receives and triggers:**
- Event: "C2.Alarm - Site Alpha" ?
- Alarm Manager receives
- Opens alarm: "C2 Alarm - Site Alpha"
- Shows Camera 1, Camera 2 (Alpha's cameras)

## Confirmation Dialog

**Before:**
```
This will create User-Defined Events and Alarm Definitions

Events to create:
  • C2.Alert (Medium severity)
  • C2.Alarm (High severity)
```

**After:**
```
This will create site-specific User-Defined Events and Alarm Definitions for:

Site: Site Alpha

Events to create:
  • C2.Alert - Site Alpha (Medium severity)
  • C2.Alarm - Site Alpha (High severity)

Alarm Definitions to create:
  • C2 Alert - Site Alpha (linked to C2.Alert event, Priority: Medium)
  • C2 Alarm - Site Alpha (linked to C2.Alarm event, Priority: High)

Associated cameras: 2
```

## Success Message

**Before:**
```
Wiring Complete!

Created: 4 items
Already existed: 0 items
```

**After:**
```
Wiring Complete for Site: Site Alpha

Created: 4 items
Already existed: 0 items

Events created:
  • C2.Alert - Site Alpha
  • C2.Alarm - Site Alpha

Alarms created:
  • C2 Alert - Site Alpha
  • C2 Alarm - Site Alpha

These are unique to this site and will only trigger
when track alarms are sent with Site='Site Alpha'.
```

## Management Client View

### Rules and Events ? User-Defined Events
```
User-Defined Events
?? C2.Alert - Site Alpha
?? C2.Alarm - Site Alpha
?? C2.Alert - Site Bravo
?? C2.Alarm - Site Bravo
```

### Alarms ? Alarm Definitions
```
Alarm Definitions
?? C2 Alert - Site Alpha
?  Trigger: C2.Alert - Site Alpha
?  Priority: Medium
?  Cameras: Camera 1, Camera 2
?
?? C2 Alarm - Site Alpha
?  Trigger: C2.Alarm - Site Alpha
?  Priority: High
?  Cameras: Camera 1, Camera 2
?
?? C2 Alert - Site Bravo
?  Trigger: C2.Alert - Site Bravo
?  Priority: Medium
?  Cameras: Camera 3, Camera 4
?
?? C2 Alarm - Site Bravo
   Trigger: C2.Alarm - Site Bravo
   Priority: High
   Cameras: Camera 3, Camera 4
```

## Testing Checklist

### Single Site:
- [ ] Create site "Test Site 1"
- [ ] Apply wiring
- [ ] Verify events created: "C2.Alert - Test Site 1", "C2.Alarm - Test Site 1"
- [ ] Verify alarms created with same naming
- [ ] Send test alarm from Smart Client with Site="Test Site 1"
- [ ] Verify alarm triggers with site name in event

### Multiple Sites:
- [ ] Create site "Site A"
- [ ] Apply wiring
- [ ] Create site "Site B"
- [ ] Apply wiring
- [ ] Verify 4 events created (2 per site)
- [ ] Verify 4 alarms created (2 per site)
- [ ] Send alarm from Site A ? Only Site A alarm triggers
- [ ] Send alarm from Site B ? Only Site B alarm triggers

### Edge Cases:
- [ ] Empty site name ? Uses "Unnamed Site"
- [ ] Special characters in site name ? Handled correctly
- [ ] Very long site name ? Truncated if needed
- [ ] Re-apply wiring ? Detects existing and skips

## Files Modified

1. ? `Admin\CoreCommandMIPUserControlTabbed.cs`
   - Made event/alarm names site-specific
   - Updated confirmation dialog
   - Updated success message
   - Updated data grid display

2. ? `Background\TrackAlarmEventHandler.cs`
   - Generate site-specific event names when triggering
   - Use Site field from TrackAlarmData

## Build Status

? **Build successful**
? **Site-specific naming implemented**
? **Multi-site support enabled**

## Summary

Events and alarms are now **unique per site**, enabling:
- ? Multi-site deployments
- ? Site-specific camera routing
- ? Clear alarm identification
- ? Independent configuration per site

**Ready to test with multiple C2 sites!** ??
