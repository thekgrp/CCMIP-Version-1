# Region Display and Rendering Troubleshooting Guide

## Changes Made

### Fix 1: Admin Region List Display ?
**File: `Admin\CoreCommandMIPUserControl.cs`**

**Problem:** The CheckedListBox was creating new `RegionListItem` objects with only Id and modified Name, losing the `Exclusion` property and other metadata.

**Solution:** Now passes the complete `RegionListItem` object to the CheckedListBox and updates the Name property to include the type label.

**What you should now see:**
- Region names followed by `[Alarm]` or `[Exclusion]` in brackets
- Debug output includes Type information

### Fix 2: Region Rendering with Better Debugging ?
**File: `Client\CoreCommandMIPViewItemWpfUserControl.xaml.cs`**

**Changes:**
1. Added comprehensive debug logging at each step
2. Improved GUID-based region selection logic
3. Better logging to track which regions are loaded vs skipped

**Debug Output to Check:**
Open the **Output Window** in Visual Studio (View ? Output) and filter by "Debug". You should see:
```
=== Starting region load ===
Loading regions from server...
Found X regions
Fetching details for region: RegionName (guid)
Added region 'RegionName' with X vertices to render list
Rendering X region(s)...
Region script: window.addRegion && window.addRegion(...)
Successfully rendered X region(s)
```

## How Regions Appear on Map

Regions are **polygons** (not icons/markers) that appear as:

### Alarm Regions:
- **Solid border** line
- Fill color specified by server (or default #ff6b6b)
- Semi-transparent fill (20% opacity by default)
- Popup on click shows: "RegionName - Inclusion Zone"

### Exclusion Regions:
- **Dashed border** line (dash pattern: 5px line, 10px gap)
- Fill color specified by server
- Semi-transparent fill
- Popup on click shows: "RegionName - Exclusion Zone"

## Troubleshooting Steps

### Step 1: Verify Regions Load in Admin
1. Open Management Client
2. Configure server settings (URL, username, password)
3. Click "Refresh Regions" button
4. Check the debug message box - should show:
   - Count of regions found
   - Each region's ID, Name, Type (Alarm/Exclusion), Active status
5. Verify regions appear in the CheckedListBox with `[Alarm]` or `[Exclusion]` labels
6. Check/select at least one region
7. Save configuration

### Step 2: Verify Regions Load in Smart Client
1. Open Smart Client
2. Add the CoreCommandMIP workspace/view
3. Check the status text at bottom of view - should show:
   - "Loading regions from server..."
   - "Found X regions"
   - "Loaded region 'Name' with X vertices"
   - "Rendering X regions..."
   - "Successfully rendered X region(s)"

### Step 3: Check Debug Output
Open **Output Window** in Visual Studio:
1. **View ? Output** (or Ctrl+W, O)
2. Select "Debug" from the dropdown
3. Look for region-related messages:
   ```
   === Starting region load ===
   Fetching details for region: ExampleRegion (guid)
   Added region 'ExampleRegion' with 8 vertices to render list
   Region script: window.addRegion && window.addRegion({...})
   ```

### Step 4: Check Browser Console (WebView2)
The map uses WebView2, which has a browser console:

**To open WebView2 DevTools:**
1. In Smart Client, right-click on the map view
2. If developer mode is enabled, you should see "Inspect" option
3. Or add this temporary button to your XAML for debugging:
   ```xaml
   <Button Content="Open DevTools" Click="OpenDevTools_Click"/>
   ```
   And in code-behind:
   ```csharp
   private void OpenDevTools_Click(object sender, RoutedEventArgs e)
   {
       _mapView?.CoreWebView2?.OpenDevToolsWindow();
   }
   ```

**What to check in Console:**
```javascript
// Check if functions exist
typeof window.addRegion  // should be "function"
typeof window.clearRegions  // should be "function"

// Check region array
regionPolygons  // should show array of polygons
regionPolygons.length  // should match number of regions

// Test adding a region manually
window.addRegion({
  name: 'Test',
  vertices: [{lat: 38.7866, lng: -104.7886}, {lat: 38.7863, lng: -104.7886}, {lat: 38.7863, lng: -104.7883}, {lat: 38.7866, lng: -104.7883}],
  color: '#ff0000',
  fill: 0.3,
  exclusion: false
});
```

## Common Issues and Solutions

### Issue: "No regions found on server"
**Causes:**
- Server doesn't have any regions configured
- Login failed (check credentials)
- Wrong URL

**Solution:**
- Verify server URL ends without `/rest` (e.g., `https://server.com` not `https://server.com/rest`)
- Check username/password
- Use browser to access `https://server.com/rest/regions/list` (should show JSON)

### Issue: "No regions returned from server"
**Causes:**
- Server returned empty array
- All regions are inactive

**Solution:**
- Check server has active regions
- Look at debug message box in Admin - shows what was received

### Issue: Regions load but don't render
**Causes:**
- Regions don't have enough vertices (need 3+)
- Regions are outside map view bounds
- JavaScript error in browser

**Solutions:**
1. Check Debug Output for "Added region" messages
2. Check status text shows "Successfully rendered X region(s)"
3. Try zooming out on map to see if regions are outside view
4. Check WebView2 console for JavaScript errors

### Issue: Regions were visible but disappeared
**Causes:**
- Map refreshed and regions weren't reloaded
- Configuration changed

**Solution:**
- This should now be fixed with the auto-reload after clearing
- Try switching tabs away and back to the map view
- Check Debug Output to see if regions are being reloaded

### Issue: Regions show but are the wrong color/type
**Causes:**
- Server returning incorrect Exclusion flag
- Color not being sent from server

**Solution:**
- Check debug message in Admin shows correct Type
- Verify server's `/rest/regions/{guid}` returns Exclusion field correctly

## Testing Checklist

- [ ] Regions load in Admin with correct names and [Type] labels
- [ ] Regions can be selected/deselected in Admin
- [ ] Selected regions are saved to configuration
- [ ] In Smart Client, status shows "Loading regions..."
- [ ] Status shows "Found X regions"
- [ ] Status shows "Loaded region 'Name' with X vertices" for each region
- [ ] Status shows "Rendering X regions"
- [ ] Status shows "Successfully rendered X region(s)"
- [ ] Regions appear as polygons on map
- [ ] Alarm regions have solid borders
- [ ] Exclusion regions have dashed borders
- [ ] Clicking region shows popup with name and type
- [ ] Regions persist when switching tabs and returning
- [ ] Regions reload when changing map/site configurations

## Next Steps

If regions still don't appear after following this guide:
1. Capture the Debug Output and send it
2. Take screenshot of Admin region list
3. Check WebView2 console output
4. Verify server returns valid data with browser/Postman
