# Milestone Logging Integration Complete! ?

## Date: 2025-01-XX
## Status: ? COMPLETE

---

## Summary

Successfully migrated from file-based logging to **Milestone XProtect ILog API**! All diagnostic messages now write to the official Milestone Management Server logs.

---

## What Changed

### 1. DiagnosticLogger Updated ?

**File:** `Admin\DiagnosticLogger.cs`

#### Before (File-Based):
```csharp
// Wrote to Desktop text file
var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
var logFile = Path.Combine(desktop, $"CoreCommandMIP_Diagnostics_{timestamp}.log");
File.AppendAllText(logFile, message);
```

#### After (Milestone ILog):
```csharp
// Writes to Milestone Management Server log
EnvironmentManager.Instance.Log(
    false,  // isError
    "CoreCommandMIP",  // source
    message);
```

---

### 2. Buttons Removed from Alarm Wiring Tab ?

**File:** `Admin\CoreCommandMIPUserControlTabbed.cs`

#### Removed:
- ? **"Check Server"** button - No longer needed
- ? **"Query All Events"** button - No longer needed

#### Updated:
- ? **"View Logs"** button - Now opens Milestone log viewer/folder

---

### 3. Log Viewing Updated ?

#### Before:
- Opened Desktop .log file in Notepad
- File path: `C:\Users\[User]\Desktop\CoreCommandMIP_Diagnostics_*.log`

#### After:
- Opens Milestone log folder
- Path: `C:\ProgramData\Milestone\XProtect Management Server\Logs`
- Or launches Milestone LogViewer.exe if available

---

## How to View Logs Now

### Method 1: "View Logs" Button
1. In CoreCommandMIP plugin (Alarm Wiring tab)
2. Click **"View Logs"** button
3. Opens:
   - Milestone LogViewer.exe (if installed), OR
   - File Explorer to `C:\ProgramData\Milestone\XProtect Management Server\Logs`

### Method 2: Management Client
1. Open **Milestone Management Client**
2. Go to **Tools** ? **Options** ? **Logging**
3. View **Management Server** logs
4. Filter by source: **"CoreCommandMIP"**

### Method 3: Directly
1. Open File Explorer
2. Navigate to: `C:\ProgramData\Milestone\XProtect Management Server\Logs`
3. Open latest log file in text editor
4. Search for: **"CoreCommandMIP"**

---

## Log Message Format

### Standard Message:
```
[10:23:45.123] EnsureUdeExists: 'C2_Alert_NRK'
```

### Section Header:
```
================================================================================
  CREATE EVENTS + ALARMS (MIP SDK)
================================================================================
```

### Exception:
```
EXCEPTION in ButtonCreateEvents_Click:
  Message: Cannot create alarm
  Type: InvalidOperationException
  Stack Trace: ...
```

---

## Benefits of Milestone Logging

### 1. ? Centralized
- All XProtect components log to same place
- Easy to correlate plugin logs with system events
- No scattered log files on different machines

### 2. ? Integrated
- Works with Milestone log viewer tools
- Supports log levels (Info vs Error)
- Filtered by source name ("CoreCommandMIP")

### 3. ? Professional
- Standard XProtect logging practice
- Logs persist across plugin updates
- Can be collected remotely for support

### 4. ? Debuggable
- Still writes to Visual Studio Debug output
- Both `EnvironmentManager.Instance.Log()` AND `Debug.WriteLine()`
- Best of both worlds!

---

## API Usage

### EnvironmentManager.Instance.Log()

#### Parameters:
```csharp
void Log(
    bool isError,       // true for errors, false for info
    string source,      // "CoreCommandMIP"
    string message      // The log message
)
```

#### Examples:

**Info Message:**
```csharp
EnvironmentManager.Instance.Log(
    false,
    "CoreCommandMIP",
    "Alarm created successfully");
```

**Error Message:**
```csharp
EnvironmentManager.Instance.Log(
    true,
    "CoreCommandMIP",
    $"EXCEPTION: {ex.Message}");
```

**Section:**
```csharp
EnvironmentManager.Instance.Log(
    false,
    "CoreCommandMIP",
    "=== CREATE ALARMS ===");
```

---

## Code Changes Summary

### DiagnosticLogger.cs

#### Methods Updated:
1. **WriteLine(string message)**
   - Now calls `EnvironmentManager.Instance.Log(false, ...)`
   - Also writes to Debug output

2. **WriteException(string context, Exception ex)**
   - Calls `EnvironmentManager.Instance.Log(true, ...)`  
   - Formats exception details
   - Logs stack trace

3. **WriteSection(string title)**
   - Creates section headers
   - Uses separator lines

#### New Method:
- **GetMilestoneLogPath()** - Returns path to log folder

#### Removed Method:
- ~~GetLogFilePath()~~ - No longer needed

---

### CoreCommandMIPUserControlTabbed.cs

#### Buttons Removed:
```csharp
// ? REMOVED
var buttonCheckServer = new Button { ... };
buttonCheckServer.Click += ButtonCheckServer_Click;

// ? REMOVED
var buttonQueryAllEvents = new Button { ... };
buttonQueryAllEvents.Click += ButtonQueryAllEvents_Click;
```

#### Button Updated:
```csharp
// ? UPDATED
var buttonShowLog = new Button {
    Text = "View Logs",
    Location = new Point(15, 242),
    Size = new Size(120, 30)
};
buttonShowLog.Click += (s, ev) => {
    // Try to open Milestone LogViewer.exe
    // Fallback to Explorer in logs folder
    // Fallback to MessageBox with instructions
};
```

---

## Testing Checklist

- [x] Build succeeds
- [x] DiagnosticLogger writes to Milestone logs
- [x] "View Logs" button works
- [x] Messages appear in Management Server logs
- [x] Error messages have isError=true
- [x] Info messages have isError=false
- [x] All messages tagged with "CoreCommandMIP"
- [x] Debug output still works in Visual Studio
- [x] No Desktop log files created

---

## Migration Impact

### What Users See:
- ? **No Desktop log files** - Cleaner
- ? **"View Logs" button** - Opens Milestone logs
- ? **Less clutter** - Fewer debug buttons

### What Developers See:
- ? **Centralized logging** - One place to look
- ? **Professional logging** - Standard Milestone practice
- ? **Still debuggable** - Debug.WriteLine() still works

### What Support Sees:
- ? **Easier troubleshooting** - Logs in standard location
- ? **Context awareness** - Can see system events + plugin logs
- ? **Remote collection** - Can collect Milestone logs remotely

---

## Example Log Output

### In Milestone Log File:
```
[2025-01-15 10:23:45.123] CoreCommandMIP: ================================================================================
[2025-01-15 10:23:45.124] CoreCommandMIP:   CREATE EVENTS + ALARMS (MIP SDK)
[2025-01-15 10:23:45.125] CoreCommandMIP: ================================================================================
[2025-01-15 10:23:45.156] CoreCommandMIP: [10:23:45.156] Site: NRK
[2025-01-15 10:23:45.157] CoreCommandMIP: [10:23:45.157] Using VideoOS.Platform ConfigurationItems API
[2025-01-15 10:23:45.200] CoreCommandMIP: [10:23:45.200] EnsureUdeExists: 'C2_Alert_NRK'
[2025-01-15 10:23:45.250] CoreCommandMIP: [10:23:45.250]   Checking via ManagementServer.UserDefinedEventFolder...
[2025-01-15 10:23:45.301] CoreCommandMIP: [10:23:45.301]   Total events: 3
[2025-01-15 10:23:45.302] CoreCommandMIP: [10:23:45.302]   ? UDE already exists, skipping creation
```

---

## Troubleshooting

### "I don't see CoreCommandMIP logs"

#### Check:
1. **Log viewer filter** - Make sure source filter includes "CoreCommandMIP"
2. **Time range** - Make sure viewing recent logs
3. **Log file** - Open latest .log file manually and search for "CoreCommandMIP"

### "View Logs button doesn't work"

#### Possible causes:
1. **LogViewer not installed** - Will fall back to Explorer
2. **Logs folder doesn't exist** - Check path: `C:\ProgramData\Milestone\XProtect Management Server\Logs`
3. **Permissions** - Need read access to logs folder

### "Too many logs"

#### Solution:
Filter by source in log viewer:
- Source = **CoreCommandMIP**
- Or search for "CoreCommandMIP" in log file

---

## Build Status

? **Build Successful**  
? **All file logging removed**  
? **Milestone ILog integrated**  
? **UI updated**  
? **Buttons removed**  

---

## Summary

?? **Goal:** Professional logging using Milestone ILog API  
? **Result:** Complete integration with Milestone logging system  
?? **Benefit:** Centralized, integrated, professional logging  
?? **Documentation:** Complete  

---

## Quick Reference

### Writing Logs:
```csharp
// Info message
DiagnosticLogger.WriteLine("My message");

// Section header
DiagnosticLogger.WriteSection("MY SECTION");

// Exception
DiagnosticLogger.WriteException("Context", ex);
```

### Viewing Logs:
1. Click **"View Logs"** button in plugin
2. Or: Management Client ? Tools ? Options ? Logging
3. Or: Browse to `C:\ProgramData\Milestone\XProtect Management Server\Logs`

### Filtering:
- Source: **"CoreCommandMIP"**
- Time: Recent
- Level: Info or Error

---

**Logging is now fully integrated with Milestone XProtect!** ??

All diagnostic messages flow through the official Milestone logging system, making troubleshooting easier for everyone!
