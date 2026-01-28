# UDE and Alarm Creation - Implementation Notes

## Reality Check: Milestone Configuration API

After implementing, I discovered that Milestone's Configuration API has limitations:

### What Works:
- ? Reading existing items via `Configuration.Instance.GetItems()`
- ? Deleting items via `Configuration.Instance.DeleteItem()`
- ? Creating basic configuration items

### What Doesn't Work / Needs Manual Setup:
- ? `Kind.UserDefinedEvent` doesn't exist in the Kind enum
- ? `Kind.AlarmDefinition` doesn't exist in the Kind enum
- ? `Configuration.Instance.SaveItem()` method doesn't exist
- ? Programmatic UDE/Alarm creation not exposed in public MIP SDK

### The Solution:
The "Apply Recommended Wiring" button now:
1. Shows a confirmation dialog with what will be created
2. Provides step-by-step instructions for manual creation
3. Lists the exact event names and settings needed
4. Guides user through Management Client setup
5. Provides a "Load Existing" feature to verify after manual creation

### User Workflow:
1. User clicks "Apply Recommended Wiring"
2. Plugin shows detailed instructions
3. User creates definitions manually in Management Client
4. User clicks "Refresh" to see newly created definitions
5. Plugin lists all C2-related events and alarms
6. User can delete or navigate to modify them

This is the standard Milestone pattern - plugins guide users through setup rather than automate everything.

## Files Modified:
- `Admin\CoreCommandMIPUserControlTabbed.cs` - Full implementation with instructions and list management
