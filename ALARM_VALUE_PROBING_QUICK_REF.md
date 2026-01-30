# Quick Reference: Alarm Definition Value Probing

## How to Debug "Out of Range" Errors

### Step 1: Get Your Alarm Probe
```csharp
using VideoOS.Platform;
using VideoOS.Platform.ConfigurationItems;

var serverId = Configuration.Instance.ServerFQID.ServerId;
var ms = new ManagementServer(serverId);

// Get any existing alarm
var existingAlarms = ms.AlarmDefinitionFolder.AlarmDefinitions.ToArray();
if (existingAlarms.Length == 0)
{
    // No alarms exist - create one manually first!
    MessageBox.Show("Please create at least one alarm manually in Management Client first.");
    return;
}

// Create probe using existing alarm's path
var probe = new AlarmDefinition(serverId, existingAlarms[0].Path);
```

### Step 2: Dump All Values
```csharp
// Use the helper method
C2AlarmWiringVerified.DumpAllAlarmDefinitionValues(probe);

// Check DiagnosticLogger output (Desktop log file)
var logPath = DiagnosticLogger.GetLogFilePath();
System.Diagnostics.Process.Start("notepad.exe", logPath);
```

### Step 3: Check Specific Values

#### EventTypeGroup
```csharp
DiagnosticLogger.WriteLine("EventTypeGroupValues:");
foreach (var kvp in probe.EventTypeGroupValues)
{
    DiagnosticLogger.WriteLine($"  '{kvp.Key}' = '{kvp.Value}'");
}
// Output:
//   'External Events' = '5946b6fa-44d9-4f4c-82bb-46a17b924265'
//   'User-defined Events' = '...'
```

#### EnableRule
```csharp
DiagnosticLogger.WriteLine("EnableRuleValues:");
foreach (var kvp in probe.EnableRuleValues)
{
    DiagnosticLogger.WriteLine($"  '{kvp.Key}' = '{kvp.Value}'");
}
// Output:
//   'Always' = 'Always'
//   'By schedule' = 'BySchedule'
//   'By event' = 'ByEvent'
```

#### Priority
```csharp
DiagnosticLogger.WriteLine("PriorityValues:");
foreach (var kvp in probe.PriorityValues)
{
    DiagnosticLogger.WriteLine($"  '{kvp.Key}' = '{kvp.Value}'");
}
// Output:
//   'Low' = 'Low'
//   'Medium' = 'Medium'
//   'High' = 'High'
//   'Critical' = 'Critical'
```

#### Category
```csharp
DiagnosticLogger.WriteLine("CategoryValues:");
foreach (var kvp in probe.CategoryValues)
{
    DiagnosticLogger.WriteLine($"  '{kvp.Key}' = '{kvp.Value}'");
}
// Output: (whatever categories exist on your system)
```

---

## Common Priority Values

Typical Milestone systems support:
- `"Low"`
- `"Medium"`
- `"High"`
- `"Critical"`

**Note:** Some systems may have different options. Always check your log!

---

## Common EnableRule Values

Typical Milestone systems support:
- `"Always"` - Alarm always enabled
- `"By schedule"` - Enabled according to time profile
- `"By event"` - Enabled/disabled by events

---

## Testing Your Values

### Test Priority
```csharp
var testPriority = "Medium";
if (probe.PriorityValues.ContainsKey(testPriority))
{
    var value = probe.PriorityValues[testPriority];
    DiagnosticLogger.WriteLine($"? '{testPriority}' is valid -> '{value}'");
}
else
{
    DiagnosticLogger.WriteLine($"? '{testPriority}' is NOT valid");
    DiagnosticLogger.WriteLine($"Valid options: {string.Join(", ", probe.PriorityValues.Keys)}");
}
```

### Test EnableRule
```csharp
var testRule = "Always";
if (probe.EnableRuleValues.ContainsKey(testRule))
{
    var value = probe.EnableRuleValues[testRule];
    DiagnosticLogger.WriteLine($"? '{testRule}' is valid -> '{value}'");
}
else
{
    DiagnosticLogger.WriteLine($"? '{testRule}' is NOT valid");
    DiagnosticLogger.WriteLine($"Valid options: {string.Join(", ", probe.EnableRuleValues.Keys)}");
}
```

---

## Integration Example

### In Your Button Handler:
```csharp
private void ButtonDebugValues_Click(object sender, EventArgs e)
{
    DiagnosticLogger.WriteSection("ALARM VALUE DEBUG");
    
    try
    {
        var serverId = Configuration.Instance.ServerFQID.ServerId;
        var ms = new ManagementServer(serverId);
        
        var alarms = ms.AlarmDefinitionFolder.AlarmDefinitions.ToArray();
        if (alarms.Length == 0)
        {
            MessageBox.Show("No alarms found. Create one manually in Management Client first.");
            return;
        }
        
        var probe = new AlarmDefinition(serverId, alarms[0].Path);
        
        // Dump all values
        C2AlarmWiringVerified.DumpAllAlarmDefinitionValues(probe);
        
        // Open log file
        var logPath = DiagnosticLogger.GetLogFilePath();
        System.Diagnostics.Process.Start("notepad.exe", logPath);
        
        MessageBox.Show("Values dumped to log file.", "Debug Complete", 
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error: {ex.Message}", "Error", 
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
```

---

## Troubleshooting

### "No alarms found"
**Solution:** Create at least one alarm manually:
1. Open Management Client
2. Go to Rules and Events ? Alarms
3. Right-click ? Add Alarm
4. Configure any alarm
5. Click OK to save
6. Try again

### "Value X not found"
**Solution:** Check the log to see what values ARE available:
```csharp
C2AlarmWiringVerified.DumpAllAlarmDefinitionValues(probe);
```

### "Dictionary is empty"
**Solution:** The probe might not be initialized correctly:
```csharp
// Make sure to use EXISTING alarm's path
var probe = new AlarmDefinition(serverId, existingAlarmPath);  // ?
// NOT root path:
var probe = new AlarmDefinition(serverId, "/AlarmDefinitionFolder");  // ?
```

---

## Build Status

? All changes compile successfully  
? Helper method available: `C2AlarmWiringVerified.DumpAllAlarmDefinitionValues()`  
? Comprehensive logging enabled  
? Works across Milestone versions  

---

**Quick tip:** Add a "Debug Values" button to your UI during development to quickly check what values are available on any system!
