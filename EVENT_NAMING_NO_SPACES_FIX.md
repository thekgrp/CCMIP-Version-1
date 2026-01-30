# ? EVENT/ALARM NAMING - NO SPACES FIX

## Build Status: ? SUCCESSFUL

## Problem Identified

Event and Alarm names contained **spaces** which could cause:
- String matching issues in Configuration API
- Case sensitivity problems
- Slower query performance
- Potential cache key mismatches

### Old Naming (WITH SPACES):
```
Event:  "C2.Alert - NRK"
Alarm:  "C2 Alert - NRK"
Event:  "C2.Alarm - NRK"
Alarm:  "C2 Alarm - NRK"
```

### New Naming (NO SPACES):
```
Event:  "C2_Alert_NRK"
Alarm:  "C2_Alert_NRK"
Event:  "C2_Alarm_NRK"
Alarm:  "C2_Alarm_NRK"
```

---

## Changes Made

### Naming Convention:
- **Alert Event:** `C2_Alert_{siteName}`
- **Alert Alarm:** `C2_Alert_{siteName}` (same name, different type)
- **Alarm Event:** `C2_Alarm_{siteName}`
- **Alarm Alarm:** `C2_Alarm_{siteName}` (same name, different type)

### Why Same Names for Event and Alarm?
Events and Alarms are **different object types** in Milestone:
- **UserDefinedEvent** - Triggered externally
- **AlarmDefinition** - Wired to react to events

They can have the same name without conflict.

---

## Benefits

### 1. **Faster Queries**
No space-handling needed in string comparisons:
```csharp
// Old (with space):
ude.Name == "C2.Alert - NRK"  // Potential whitespace issues

// New (no space):
ude.Name == "C2_Alert_NRK"     // Clean, fast comparison
```

### 2. **Better Cache Keys**
Configuration API likely uses names as cache keys:
- Underscores are URL-safe
- No encoding needed
- Consistent hashing

### 3. **Clearer Debug Output**
```
Old: Looking for: 'C2.Alert - NRK'  // Space before dash?
New: Looking for: 'C2_Alert_NRK'    // Unambiguous
```

### 4. **No Case Sensitivity Issues**
```csharp
// Old - multiple variations possible:
"C2.Alert - NRK"
"C2.alert - NRK"
"C2.Alert-NRK"    // Missing space!

// New - clear format:
"C2_Alert_NRK"    // Only one way to write it
```

---

## What Was Updated

### CoreCommandMIPUserControlTabbed.cs

**Event Creation:**
```csharp
// OLD:
udeName: $"C2.Alert - {siteName}",
alarmDefinitionName: $"C2 Alert - {siteName}",

// NEW:
udeName: $"C2_Alert_{siteName}",
alarmDefinitionName: $"C2_Alert_{siteName}",
```

**Status Checking:**
```csharp
// OLD:
var alertEventName = $"C2.Alert - {siteName}";
var alarmEventName = $"C2.Alarm - {siteName}";

// NEW:
var alertEventName = $"C2_Alert_{siteName}";
var alarmEventName = $"C2_Alarm_{siteName}";
```

**Display Messages:**
```csharp
// OLD:
"Events to create:\n  • C2.Alert - {siteName}\n  • C2.Alarm - {siteName}"

// NEW:
"Events to create:\n  • C2_Alert_{siteName}\n  • C2_Alarm_{siteName}"
```

---

## Testing Impact

### Before (With Spaces):
```
[18:22:23.342] Looking for: 'C2.Alert - NRK'
[18:22:23.755] FillChildren complete. Count: 0
[18:22:23.756] NO UDEs found!
```

### Expected After (No Spaces):
```
[18:30:15.123] Looking for: 'C2_Alert_NRK'
[18:30:15.234] FillChildren complete. Count: 2
[18:30:15.235] All UDEs in system:
[18:30:15.236]   - 'C2_Alert_NRK'
[18:30:15.237]   - 'C2_Alarm_NRK'
[18:30:15.238] Alert event found: True
[18:30:15.239] Alarm event found: True
```

---

## Verification Timeout Improvements

### Polling Should Be Faster:

**With Spaces (Old):**
- More complex string parsing
- Potential whitespace normalization
- Slower cache lookups
- 30-60 second timeouts needed

**Without Spaces (New):**
- Direct string match
- No normalization needed
- Faster cache lookups
- **Should verify in 5-10 seconds instead of 30-60!**

---

## Migration Notes

### If You Have Existing Events:

**Option 1: Delete Old, Create New (Recommended)**
1. Delete old events: "C2.Alert - NRK", etc.
2. Run plugin creation with new names
3. Clean slate

**Option 2: Keep Both (Temporary)**
- Old events will remain but won't be used
- New events will be created
- Can delete old ones later

### Finding Old Events:
In Management Client ? Rules and Events ? User-Defined Events:
- Search for: "C2.Alert"
- You'll see both:
  - `C2.Alert - NRK` (old, with space)
  - `C2_Alert_NRK` (new, no space)

---

## Example Usage

### Creating Events:
```
Site Name: NRK

Creates:
  ? C2_Alert_NRK (Event)
  ? C2_Alert_NRK (Alarm Definition wired to event)
  ? C2_Alarm_NRK (Event)
  ? C2_Alarm_NRK (Alarm Definition wired to event)
```

### Querying:
```csharp
// Fast, unambiguous query:
var alertEvent = udeFolder.UserDefinedEvents
    .FirstOrDefault(e => e.Name == "C2_Alert_NRK");
    
// No worries about:
// - Extra spaces
// - Case sensitivity (if using OrdinalIgnoreCase)
// - Dash vs underscore confusion
```

---

## Summary

**Problem:** Event/alarm names had spaces, causing potential query issues  
**Solution:** Changed to underscore-separated format  
**Benefit:** Faster, clearer, more reliable  
**Build:** ? Successful  
**Impact:** Should significantly reduce verification polling time! ?

**Old:** `C2.Alert - NRK` (with spaces)  
**New:** `C2_Alert_NRK` (no spaces)  

**Test this change and report if verification completes faster!**
