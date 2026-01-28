# ? EVENT LOADING & MAP PREVIEW FIXES

## Build Status: ? SUCCESSFUL

## What Was Fixed:

### Issue 1: Events Not Showing in List ?

**Problem:**
- Created events/alarms not appearing in "Existing Event & Alarm Definitions" list
- No visibility into what was actually created

**Root Cause:**
- No logging to debug what items were being found
- Search pattern might miss variations (e.g., "C2-" vs "C2.")
- Silent failures if listbox not found

**Solution:**
1. **Added comprehensive logging:**
   - Logs total number of UDEs and Alarms found
   - Logs each item name as it's checked
   - Logs which items match the C2 pattern
   - Logs when listbox not found

2. **Expanded search pattern:**
   ```csharp
   // OLD: Only matched "C2." or "C2 "
   ude.Name.StartsWith("C2.") || ude.Name.Contains("C2 ")
   
   // NEW: Also matches "C2-"
   ude.Name.StartsWith("C2.") || ude.Name.Contains("C2 ") || ude.Name.Contains("C2-")
   ```

3. **Better error handling:**
   - Shows message box if error occurs
   - Full stack trace in debug output
   - Checks for null collections

**Debug Output Now Shows:**
```
=== Loading Existing Event Definitions ===
Total UDEs found: 10
  UDE: C2.Alert - Site Alpha
    ? Added C2 UDE: C2.Alert - Site Alpha
  UDE: C2.Alarm - Site Alpha
    ? Added C2 UDE: C2.Alarm - Site Alpha
  UDE: SomeOtherEvent
Total Alarms found: 8
  Alarm: C2 Alert - Site Alpha
    ? Added C2 Alarm: C2 Alert - Site Alpha
  Alarm: C2 Alarm - Site Alpha
    ? Added C2 Alarm: C2 Alarm - Site Alpha
=== Loaded 2 C2 events and 2 C2 alarms ===
```

### Issue 2: WebView2 Map Preview Not Initializing ?

**Problem:**
- Map preview shows blank
- Lat/Lon/Zoom not applied
- No feedback on what's wrong

**Root Causes:**
1. Method called before tab is visible
2. WebView2 might not be initialized yet
3. No error logging
4. No refresh when switching to Map tab

**Solutions:**

#### 1. Added Visibility Check
```csharp
// Check if tab is visible before trying to update
if (!tabControl.TabPages[1].Visible)
{
    System.Diagnostics.Debug.WriteLine("Tab 2 not visible yet, skipping");
    return;
}
```

#### 2. Enhanced Error Logging
```csharp
System.Diagnostics.Debug.WriteLine($"UpdateSitePreview: Lat={lat}, Lon={lon}, Zoom={zoom}");
System.Diagnostics.Debug.WriteLine("WebView2 initialized");
System.Diagnostics.Debug.WriteLine("Map HTML loaded");
```

#### 3. Added Tab Change Handler
```csharp
tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
{
    // When Map & Regions tab is selected, refresh preview
    if (tabControl.SelectedIndex == 1 && _item != null)
    {
        var settings = CollectCurrentSettings();
        UpdateSitePreview(settings);
    }
}
```

#### 4. Added JavaScript Console Logging
```javascript
console.log('Creating map at lat=..., lon=..., zoom=...');
var map = L.map('map').setView([lat, lon], zoom);
console.log('Map created successfully');
```

#### 5. Better Null Handling
```csharp
if (webViewSitePreview == null)
{
    Debug.WriteLine("WebView2 control is null");
    return;
}

if (settings == null)
{
    Debug.WriteLine("Settings are null");
    return;
}
```

## How to Debug Now:

### For Event Loading Issues:
1. Open Debug Output window (View ? Output)
2. Click "Refresh" button in Existing Definitions section
3. Look for:
   ```
   === Loading Existing Event Definitions ===
   Total UDEs found: X
   ? Added C2 UDE: ...
   ```
4. If no items found, check Management Client ? Rules and Events ? User-Defined Events

### For Map Preview Issues:
1. Open Debug Output window
2. Switch to "Map & Regions" tab
3. Look for:
   ```
   UpdateSitePreview: Lat=..., Lon=..., Zoom=...
   UpdateSitePreview: WebView2 initialized
   UpdateSitePreview: Map HTML loaded
   ```
4. If preview still blank:
   - Check WebView2 Runtime is installed
   - Check internet connection (needs to load Leaflet.js)
   - Check browser console (F12 if you can open DevTools)

## Testing Checklist:

### Event Loading:
- [ ] Create events using "Apply Recommended Wiring"
- [ ] Click "Refresh" button
- [ ] Check Debug Output for "=== Loading Existing Event Definitions ==="
- [ ] Verify events appear in list with correct names
- [ ] Click "Info" to see event details
- [ ] Verify both Events and Alarms are listed

### Map Preview:
- [ ] Enter Lat/Lon/Zoom values in Tab 2
- [ ] Switch away from Tab 2
- [ ] Switch back to Tab 2
- [ ] Check Debug Output for "UpdateSitePreview" messages
- [ ] Verify map loads with correct center
- [ ] Verify zoom level is correct
- [ ] Verify marker appears at center
- [ ] Verify popup shows site name

## Common Issues & Solutions:

### Events Not Loading:
**Symptom:** List shows empty even after creating events
**Check:**
1. Debug output: "Total UDEs found: 0"
   - Events weren't created successfully
   - Check earlier logs for creation errors
2. Debug output: "UDE: EventName" but not "? Added"
   - Event name doesn't match C2 pattern
   - Check event name in Management Client
3. Debug output: "ListBox not found"
   - Tab 3 not initialized properly
   - Try switching tabs

### Map Preview Not Loading:
**Symptom:** Blank preview in Tab 2
**Check:**
1. Debug output: "WebView2 control is null"
   - Tab 2 not created yet
   - Try switching to Tab 2 first
2. Debug output: "Tab 2 not visible yet"
   - UpdateSitePreview called too early
   - Should auto-update when you switch to tab
3. Debug output: "Error updating site preview: ..."
   - WebView2 Runtime not installed
   - Install from https://developer.microsoft.com/microsoft-edge/webview2/
4. No debug output at all:
   - UpdateSitePreview not being called
   - Check if OnMapSettingChanged fires when changing values

### Map Shows Wrong Location:
**Symptom:** Map loads but at wrong location
**Check:**
1. Debug output shows correct values:
   ```
   UpdateSitePreview: Lat=38.5, Lon=-122.3, Zoom=12
   ```
2. If values are wrong:
   - Check what's in textBoxLatitude.Text
   - Check ParseDoubleOrDefault is working
   - Check CollectCurrentSettings()
3. If values are right but map wrong:
   - Check JavaScript console in WebView2
   - Might be Leaflet.js initialization issue

## Files Modified:

1. ? `Admin\CoreCommandMIPUserControlTabbed.cs`
   - Enhanced LoadExistingEventDefinitions with logging
   - Enhanced UpdateSitePreview with checks and logging
   - Added TabControl_SelectedIndexChanged handler
   - Expanded search pattern for C2 items

## Summary:

**Event Loading:** Now has comprehensive debug logging to show:
- What items exist
- Which items match C2 pattern
- What gets added to list
- Any errors that occur

**Map Preview:** Now has:
- Visibility check before updating
- Auto-refresh when switching to tab
- Comprehensive logging at each step
- Better error handling and reporting

Both issues should now be much easier to diagnose and fix!

**Build:** ? Successful  
**Ready:** To test with debug output visible! ??
