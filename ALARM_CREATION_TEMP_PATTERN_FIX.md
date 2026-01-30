# Alarm Creation Fixed - Using Temp AlarmDefinition Pattern

## Summary
Updated alarm creation in `C2AlarmWiringVerified.cs` to follow the correct Milestone pattern using a temp `AlarmDefinition` to get valid `EventTypeGroup` and `EventType` values, instead of using `FillInChildren` which wasn't working.

## Problem Solved
**Previous Issue**: Using hardcoded strings for `EventTypeGroup` and `EventType` which may not match the internal enum values that Milestone expects.

**Solution**: Create a temporary `AlarmDefinition` object, set the event type group, validate it, then use `EventTypeGroupValues` and `EventTypeValues` dictionaries to lookup the proper internal values.

## Changes Made

### 1. Updated EnsureAlarmDefinitionExists Method

**Key Changes**:
- Query alarms from array (same pattern as events) ?
- Create temp `AlarmDefinition` to get valid values ?
- Use `FindValueByDisplayName` helper to lookup values ?
- Use `ms.AlarmDefinitionFolder` instead of passed folder ?

**New Pattern**:
```csharp
// Create temp AlarmDefinition to get valid values (NOT using FillInChildren)
var tempAlarmDef = new AlarmDefinition(serverId, "/AlarmDefinitionFolder");

// Get proper EventTypeGroup value (External Events)
string eventTypeGroupValue = FindValueByDisplayName(
    tempAlarmDef.EventTypeGroupValues, 
    "External Events");

tempAlarmDef.EventTypeGroup = eventTypeGroupValue;
tempAlarmDef.ValidateItem();

// Get proper EventType value
string eventTypeValue = FindValueByDisplayName(
    tempAlarmDef.EventTypeValues, 
    "External Event");

// Create alarm with proper values
var addAlarmTask = ms.AlarmDefinitionFolder.AddAlarmDefinition(
    name: alarmDefinitionName,
    description: $"Auto-created. Triggered by UDE '{ude.Name}'.",
    eventTypeGroup: eventTypeGroupValue,  // ? Proper internal value
    eventType: eventTypeValue,            // ? Proper internal value
    sourceList: udePath,
    // ... other parameters
);
```

### 2. Added FindValueByDisplayName Helper

Looks up the internal enum value from a display name in Milestone's value dictionaries:

```csharp
private static string FindValueByDisplayName(
    Dictionary<string, string> values, 
    string displayName)
{
    foreach (var kvp in values)
    {
        if (string.Equals(kvp.Key, displayName, StringComparison.OrdinalIgnoreCase))
        {
            return kvp.Value;  // Return internal enum value
        }
    }
    return null;
}
```

### 3. Enhanced Diagnostic Logging

Added detailed logging to help troubleshoot value lookups:
- Logs which EventTypeGroup value is found
- Logs which EventType value is found
- Lists all available values if lookup fails
- Logs all parameters being passed to AddAlarmDefinition

## Value Lookup Fallbacks

The code tries multiple display names as fallbacks:

**EventTypeGroup**:
1. Try "External Events" first
2. Fall back to "User-defined Events" if not found

**EventType**:
1. Try "External Event" first
2. Fall back to "User-defined Event" if not found

This ensures compatibility across different Milestone versions.

## Benefits

1. **Correct Values**: Uses proper internal enum values instead of hardcoded strings
2. **More Reliable**: Follows Milestone's recommended pattern
3. **Better Diagnostics**: Logs all available values if lookup fails
4. **Version Compatible**: Tries multiple fallback names

## Diagnostic Log Output

Expected log output when creating alarms:

```
EnsureAlarmDefinitionExists: 'C2_Alert_NRK'
Checking via ManagementServer.AlarmDefinitionFolder...
  Total alarms: 5
  Alarm not found, creating...
  Wiring to UDE path: UserDefinedEvent[guid-here]
  Looking up EventTypeGroup values...
    Found: 'External Events' -> 'ExternalEvents'
  EventTypeGroup: 'ExternalEvents'
  Looking up EventType values...
    Found: 'External Event' -> 'ExternalEvent'
  EventType: 'ExternalEvent'
  Creating alarm via AddAlarmDefinition...
    Name: C2_Alert_NRK
    EventTypeGroup: ExternalEvents
    EventType: ExternalEvent
    SourceList: UserDefinedEvent[guid-here]
    Priority: Medium
    Cameras: 0
  ? Alarm creation task completed with state: Success
```

If values are not found, all available options will be logged:

```
  Looking up EventTypeGroup values...
    Not found: 'External Events'
    Available values:
      - User-defined Events = UserDefinedEvents
      - Analytics Events = AnalyticsEvents
      - Generic Events = GenericEvents
```

## Testing Steps

1. Open Management Client
2. Configure CoreCommandMIP plugin
3. Go to Tab 3: Alarm Wiring
4. Enter site name (e.g., "NRK")
5. Click "Create Events + Alarms"
6. Check diagnostic log for proper value lookups
7. Verify alarms are created in Management Client

### Expected Results:
? Events created successfully  
? Alarms created successfully  
? Alarms properly wired to events  
? Alarms trigger when events fire  
? Alarms appear in Alarm Manager  

## Files Modified
- `Admin/C2AlarmWiringVerified.cs` - Updated alarm creation pattern

## Build Status
? **Build successful** - No errors

## Comparison: Before vs After

### Before (Hardcoded Strings):
```csharp
var eventTypeGroup = "User-defined Events";  // ? May not match internal value
var eventType = "User-defined Event";        // ? May not match internal value

var addAlarmTask = alarmFolder.AddAlarmDefinition(
    eventTypeGroup: eventTypeGroup,
    eventType: eventType,
    // ...
);
```

### After (Temp AlarmDefinition Pattern):
```csharp
var tempAlarmDef = new AlarmDefinition(serverId, "/AlarmDefinitionFolder");
string eventTypeGroupValue = FindValueByDisplayName(
    tempAlarmDef.EventTypeGroupValues, "External Events");
tempAlarmDef.EventTypeGroup = eventTypeGroupValue;
tempAlarmDef.ValidateItem();

string eventTypeValue = FindValueByDisplayName(
    tempAlarmDef.EventTypeValues, "External Event");

var addAlarmTask = ms.AlarmDefinitionFolder.AddAlarmDefinition(
    eventTypeGroup: eventTypeGroupValue,  // ? Proper internal value
    eventType: eventTypeValue,            // ? Proper internal value
    // ...
);
```

## Why This Works

1. **Temp AlarmDefinition**: Creates a temporary object to access the valid values
2. **EventTypeGroupValues**: Dictionary of display names ? internal values
3. **ValidateItem**: Populates the EventTypeValues based on selected group
4. **FindValueByDisplayName**: Looks up the internal enum value safely
5. **Fallback Logic**: Tries multiple names for compatibility

This pattern ensures we're passing the exact values Milestone expects, rather than guessing at string matches.

## Next Steps
1. ? Code updated
2. ? Build successful
3. ? Test alarm creation
4. ? Verify alarms are wired correctly
5. ? Confirm alarms trigger on events

---

**Status**: ? IMPLEMENTATION COMPLETE - READY FOR TESTING  
**Pattern**: Temp AlarmDefinition with value lookup  
**Build**: ? Successful  
**Date**: 2025-06-XX  
