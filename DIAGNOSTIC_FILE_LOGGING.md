# ? DIAGNOSTIC FILE LOGGING IMPLEMENTED

## Build Status: ? SUCCESSFUL

## What Was Added

A comprehensive diagnostic logging system that writes to a file on the Desktop of the remote machine, so you can troubleshoot issues without needing Visual Studio Debug Output.

---

## For the User (Remote Machine)

### How to Use It:

1. **Open Management Client** on the remote machine
2. **Configure the plugin** - enter site name, etc.
3. **Click any of these buttons:**
   - "Step 1: Create Events"
   - "Step 2: Create Alarms"
   - "Refresh Status"
   
4. **A log file is automatically created on your Desktop:**
   ```
   CoreCommandMIP_Diagnostics_YYYYMMDD_HHmmss.log
   ```

5. **Click the "View Log" button** in the UI to open it in Notepad
   - OR manually open it from Desktop

6. **Send the log file** to the developer for analysis

---

## What's Logged

### Every Time You Click "Refresh Status":

```
========================================
  UPDATE WIRING STATUS
========================================
[12:34:56.123] Site name: 'NRK'
[12:34:56.234] Creating UserDefinedEventFolder...
[12:34:56.345] Cleared UDE cache
[12:34:56.456] FillChildren complete. Count: 2
[12:34:56.567] Searching for events:
[12:34:56.678]   'C2.Alert - NRK'
[12:34:56.789]   'C2.Alarm - NRK'
[12:34:56.890] All UDEs in system:
[12:34:56.901]   - 'C2.Alert - NRK'
[12:34:56.912]   - 'C2.Alarm - NRK'
[12:34:56.923]   - 'SomeOtherEvent'
[12:34:56.934] Alert event found: True
[12:34:56.945] Alarm event found: True
[12:34:56.956] Creating AlarmDefinitionFolder...
[12:34:56.967] Cleared alarm cache
[12:34:56.978] FillChildren complete. Count: 0
[12:34:56.989] Searching for alarms:
[12:34:56.990]   'C2 Alert - NRK'
[12:34:56.991]   'C2 Alarm - NRK'
[12:34:56.992] Alert alarm found: False
[12:34:56.993] Alarm alarm found: False
========================================
  FINAL STATUS
========================================
[12:34:56.994] Events exist: True
[12:34:56.995] Alarms exist: False
[12:34:56.996] ? EVENTS EXIST - Step 2 button ENABLED
[12:34:56.997] Button states: Step1=True, Step2=True
[12:34:56.998] DataGrid updated with 4 rows
[12:34:56.999] Log file: C:\Users\YourName\Desktop\CoreCommandMIP_Diagnostics_20240127_123456.log
[12:34:57.000] UpdateWiringStatus() complete
```

### Key Information Captured:

? **Site name** - What you entered
? **Cache clearing** - Whether it happened
? **Total items found** - How many UDEs and Alarms exist
? **ALL items listed** - Shows every event/alarm by name
? **Search results** - Which specific ones were found
? **Button states** - Whether Step 2 is enabled or disabled
? **Errors** - Full exception details if anything fails

---

## UI Changes

### New Button Added:

In the "Recommended Alarm Definitions" section:
```
[Step 1: Create Events] [Step 2: Create Alarms] [Refresh Status]

[View Log]  Create events first (Step 1), then alarms (Step 2). 
            Both unique to site name.
            Diagnostic log saved to Desktop - click 'View Log' to see details.
```

**Click "View Log"** to:
- Open the log file in Notepad
- See all diagnostic information
- Copy and send to developer

---

## For the Developer

### Analyzing the Log File:

**Look for these key sections:**

1. **UDE Count Check:**
   ```
   FillChildren complete. Count: 2
   ```
   If Count is 0 ? Events not created or cache issue

2. **All UDEs Listed:**
   ```
   All UDEs in system:
     - 'C2.Alert - NRK'
     - 'C2.Alarm - NRK'
   ```
   Should show the events we're looking for

3. **Search Results:**
   ```
   Alert event found: True
   Alarm event found: True
   ```
   If False ? Name mismatch or events don't exist

4. **Button States:**
   ```
   Button states: Step1=True, Step2=True
   ```
   Step2 should be True when events exist

---

## Common Issues Revealed by Log

### Issue 1: No Events Found

**Log shows:**
```
FillChildren complete. Count: 0
All UDEs in system:
NO UDEs found!
Alert event found: False
```

**Diagnosis:** Events not created OR Configuration API cache not clearing

**Solution:** 
- Check if events exist in Management Client manually
- Try longer delay after cache clear
- Create new folder instance instead of clearing cache

### Issue 2: Events Exist But Not Found

**Log shows:**
```
FillChildren complete. Count: 5
All UDEs in system:
  - 'C2 Alert-NRK'    ? Missing space before dash!
  - 'C2 Alarm-NRK'
Alert event found: False
```

**Diagnosis:** Name mismatch (spacing/formatting)

**Solution:**
- Events created with wrong format
- Need to match exact format: "C2.Alert - NRK"

### Issue 3: Step 2 Button Disabled Despite Events

**Log shows:**
```
Alert event found: True
Alarm event found: True
Events exist: True
? EVENTS EXIST - Step 2 button ENABLED
Button states: Step1=True, Step2=False  ? Mismatch!
```

**Diagnosis:** Button state set but not taking effect (UI thread issue?)

**Solution:**
- Force UI refresh
- Use BeginInvoke to set button state
- Check if button is enabled in UI initializer

---

## File Location

**Log file saved to:**
```
C:\Users\[Username]\Desktop\CoreCommandMIP_Diagnostics_YYYYMMDD_HHmmss.log
```

**New log created each time Management Client starts**

**Old logs remain** - you can compare between sessions

---

## How to Share Log File

### Option 1: Copy from Desktop
1. Go to Desktop
2. Find `CoreCommandMIP_Diagnostics_*.log`
3. Email or attach to support ticket

### Option 2: Click "View Log" Button
1. Click button in Management Client
2. Log opens in Notepad
3. Ctrl+A (select all)
4. Ctrl+C (copy)
5. Paste into email or ticket

### Option 3: Remote Desktop Screenshot
1. Click "View Log"
2. Take screenshot of Notepad window
3. Send screenshot

---

## Testing Steps

### Test 1: Log File Creation
1. Open Management Client
2. Check Desktop - log file should appear immediately
3. Note filename with timestamp

### Test 2: Event Status Logging
1. Enter site name: "NRK"
2. Click "Refresh Status"
3. Click "View Log"
4. Should see section "UPDATE WIRING STATUS"
5. Should show site name and search results

### Test 3: Event Creation Logging
1. Click "Step 1: Create Events"
2. Wait for completion
3. Click "View Log"
4. Should show creation attempts and results

### Test 4: Error Logging
1. Enter invalid/empty site name
2. Click "Refresh Status"
3. Check log for error details

---

## Troubleshooting

### Log File Not Created

**If no log file on Desktop:**
- Check code is deployed correctly
- Verify Management Client is running new version
- Check permissions to write to Desktop
- Look for log in: `%USERPROFILE%\Desktop\`

### "View Log" Button Does Nothing

**If button click fails:**
- Log file may not exist yet
- Click "Refresh Status" first to create log
- Check message box that appears

### Log File Empty

**If file exists but empty:**
- No actions performed yet
- Click buttons to trigger logging
- Check file is not locked/in use

---

## Summary

**Added:** Comprehensive file logging to Desktop
**Button:** "View Log" to open log in Notepad  
**Captures:** All status checks, cache clears, search results, button states
**Purpose:** Remote troubleshooting without Visual Studio
**Build:** ? Successful
**Ready:** Log file will be created on next Management Client launch! ??

**Next Steps:**
1. Deploy to remote machine
2. Run Management Client
3. Perform actions (create events, refresh status, etc.)
4. Click "View Log"
5. Send log file for analysis
