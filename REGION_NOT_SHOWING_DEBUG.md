# Region Display Issues - Debugging Guide

## Important: Regions Are POLYGONS, Not Icons

**Regions appear as colored areas/boundaries on the map, NOT as icon markers.**

### What You Should See:
- **Colored polygons** (filled areas) with borders
- **Solid lines** for Alarm regions
- **Dashed lines** for Exclusion regions
- **Semi-transparent fill** (20% opacity by default)
- **Popup on click** showing region name and type

### What You Won't See:
- ? Icon markers (like pins or symbols)
- ? Labels at specific points
- ? Small symbols at vertices

---

## Step-by-Step Debugging

### Step 1: Check Region Loading in Management Client

1. Open **Management Client** (Admin mode)
2. Configure server settings
3. Click **"Refresh Regions"** button
4. Check the debug popup that appears - should show:
   ```
   Base URL: https://yourserver.com/rest/regions/list
   Regions Count: X
   
   ID: 1, Name: RegionName, Type: Alarm/Exclusion, Active: True
   ```

**Expected Result:** You should see a list of regions with their names and types.

**If you see "No regions found":**
- Server doesn't have regions configured
- Wrong URL/credentials
- Server is not returning data

### Step 2: Check Selection and Save

1. In Management Client, **check/select** at least one region
2. Click **Save** configuration
3. Verify the region shows with `[Alarm]` or `[Exclusion]` label

**Expected Result:** Selected regions are checked in the list

### Step 3: Check Region Loading in Smart Client

1. Open **Smart Client**
2. Add the CoreCommandMIP workspace/view
3. Watch the **status text** at bottom of view - should show progression:
   ```
   Loading regions from server...
   Found X regions
   Fetching details for region: RegionName (guid)
   Added region 'RegionName' with X vertices to render list
   Rendering X regions...
   Successfully rendered X region(s)
   ```

**If status stops at "Loading regions...":**
- Credentials not stored/available
- Server not reachable from Smart Client

**If status shows "Found 0 regions":**
- No regions were selected in Management Client
- Or selection wasn't saved

**If status shows "Skipping unselected region":**
- Regions exist but weren't selected in Management Client
- Select ALL or select specific regions in Management Client

### Step 4: Check Debug Output Window

Open **Output Window** in Visual Studio while Smart Client is running:
1. **View ? Output** (or Ctrl+W, O)
2. Select **"Debug"** from dropdown
3. Look for these messages:

**Good signs:**
```
=== Starting region load ===
Found 9 GUID regions
Fetching details for region: Exclusion1 (8f237879-...)
Added region 'Exclusion1' with 8 vertices to render list
Region script: window.addRegion && window.addRegion({name:"Exclusion1",vertices:[...],...})
Successfully rendered X region(s)
```

**Bad signs:**
```
Skipping inactive region: RegionName
Skipping unselected region: RegionName (ID: 123)
No regions returned from server
Failed to fetch details for region...
```

### Step 5: Check Map Console (WebView2 DevTools)

**Open WebView2 Developer Tools:**

Option A - If developer mode enabled:
- Right-click on map ? Select **"Inspect"**

Option B - Add temporary debug button:
1. Add this to your XAML temporarily:
```xaml
<Button Content="Open DevTools" Click="OpenDevTools_Click" />
```
2. Add this to code-behind:
```csharp
private void OpenDevTools_Click(object sender, RoutedEventArgs e)
{
    _mapView?.CoreWebView2?.OpenDevToolsWindow();
}
```

**In the Console tab, check:**

1. **Are JavaScript functions defined?**
```javascript
typeof window.addRegion  // Should return "function"
typeof window.clearRegions  // Should return "function"
```

2. **Check for region array:**
```javascript
regionPolygons  // Should show array
regionPolygons.length  // Should show number of regions
```

3. **Look for JavaScript errors:**
- Red error messages in console
- Errors mentioning "addRegion" or "polygon"
- Leaflet errors

4. **Test adding a region manually:**
```javascript
window.addRegion({
  name: 'Test',
  vertices: [
    {lat: 38.7866, lng: -104.7886},
    {lat: 38.7863, lng: -104.7886},
    {lat: 38.7863, lng: -104.7883},
    {lat: 38.7866, lng: -104.7883}
  ],
  color: '#ff0000',
  fill: 0.3,
  exclusion: false
});
```

**Expected Result:** A red polygon should appear on the map

### Step 6: Check Map View Bounds

**Regions might be outside the visible map area!**

**Try:**
1. **Zoom out** on the map significantly
2. **Pan around** the map area
3. Check if regions are far from your default location

**To check programmatically in DevTools:**
```javascript
// Get map bounds
map.getBounds()

// Center map on specific coordinates (use your region's coordinates)
map.setView([38.7866, -104.7886], 15);
```

### Step 7: Verify Region Has Valid Vertices

**In Debug Output, check:**
```
Loaded region 'RegionName' with X vertices
```

**X should be:**
- Minimum 3 vertices (triangle)
- Typically 4-8 vertices for most regions
- All vertices must have valid lat/lng coordinates

**If you see "Skipped region: too few vertices":**
- Region has less than 3 vertices
- Region data is corrupted on server

---

## Common Issues and Solutions

### Issue 1: "Regions were working, now they're gone"
**Cause:** Map refreshed without reloading regions

**Solution:**
- Switch tabs away and back to map view
- Should trigger auto-reload (we added this fix)
- Check Debug Output for "=== Starting region load ==="

### Issue 2: "I can select regions in Admin but they don't show in Smart Client"
**Possible causes:**
1. **Regions not saved:** Click Save in Management Client
2. **Wrong configuration selected:** Make sure Smart Client is using the right configuration
3. **All regions are inactive:** Check server, regions must have Active=true

**Solution:**
- Re-save configuration in Management Client
- Check Output Window for "Skipping inactive region" messages
- Verify server regions are Active

### Issue 3: "Debug says regions rendered but I see nothing"
**Possible causes:**
1. **Regions outside map bounds:** Zoom out and pan around
2. **Regions too small:** Try zooming in to region location
3. **Fill opacity 0:** Region has no visible fill
4. **Map not initialized:** Map WebView not ready

**Solution:**
```javascript
// In DevTools Console:
// 1. Check if regions were added
console.log(regionPolygons.length);

// 2. Get first region's bounds
if (regionPolygons.length > 0) {
  var bounds = regionPolygons[0].getBounds();
  console.log('Region bounds:', bounds);
  map.fitBounds(bounds);
}
```

### Issue 4: "Getting JavaScript errors in console"
**Common errors:**
- `Uncaught TypeError: Cannot read property 'addRegion' of undefined`
  - JavaScript functions not defined yet
  - Map not fully initialized
  
- `L is not defined`
  - Leaflet library not loaded
  - Check MapTemplate has correct Leaflet CDN links

**Solution:**
- Wait for map to fully load before rendering regions
- Check MapTemplate.cs has proper Leaflet initialization

### Issue 5: "Regions show once, disappear on tab switch"
**Status:** This should be FIXED with recent changes

**If still happening:**
- Check Debug Output when switching tabs
- Should see "=== Starting region load ===" when returning to map
- If not, the auto-reload isn't triggering

---

## Quick Test Checklist

Run through this checklist to identify the issue:

- [ ] Management Client shows regions with [Alarm]/[Exclusion] labels
- [ ] At least one region is selected (checked) in Management Client
- [ ] Configuration is saved in Management Client
- [ ] Smart Client status shows "Loading regions from server..."
- [ ] Smart Client status shows "Found X regions" (X > 0)
- [ ] Smart Client status shows "Rendering X regions"
- [ ] Smart Client status shows "Successfully rendered X region(s)"
- [ ] Debug Output shows "Added region 'Name' with X vertices"
- [ ] Debug Output shows "Region script: window.addRegion..."
- [ ] No red errors in Debug Output
- [ ] Map is initialized and showing base map tiles
- [ ] Map is centered near region coordinates
- [ ] Tried zooming out to see if regions are outside view
- [ ] WebView2 Console shows `regionPolygons.length > 0`
- [ ] No JavaScript errors in WebView2 Console

---

## Expected Visual Appearance

### Alarm Region (Inclusion Zone):
- **Border:** Solid line, colored (default: #ff6b6b red)
- **Fill:** Semi-transparent fill matching border color
- **Opacity:** ~20% (can see through to map below)
- **On Hover:** Polygon highlights slightly
- **On Click:** Popup shows "RegionName - Inclusion Zone"

### Exclusion Region:
- **Border:** Dashed line (5px line, 10px gap)
- **Fill:** Semi-transparent fill matching border color
- **Opacity:** ~20%
- **On Hover:** Polygon highlights slightly
- **On Click:** Popup shows "RegionName - Exclusion Zone"

### Example of What You Should See:
```
?????????????????????????????????????
?  MAP VIEW                         ?
?                                   ?
?     ???????????                  ?
?     ? ALARM   ?  ? Solid border  ?
?     ? REGION  ?     Red fill     ?
?     ???????????     20% opacity  ?
?                                   ?
?     ? ? ? ? ? ?                  ?
?     ¦EXCLUSION¦  ? Dashed border ?
?     ¦ REGION  ¦     Red fill     ?
?     ? ? ? ? ? ?     20% opacity  ?
?                                   ?
?????????????????????????????????????
```

---

## Still Not Working?

If you've gone through all steps and regions still aren't showing:

1. **Capture Debug Output** - Copy all Debug messages from Output Window
2. **Capture Status Messages** - Note what the status text says
3. **Check WebView2 Console** - Copy any errors or warnings
4. **Verify Server Data** - Use browser/Postman to check:
   - `https://yourserver.com/rest/regions/list` - Should return JSON array
   - `https://yourserver.com/rest/regions/{guid}` - Should return region details with Vertices array

5. **Test Manual Region** - In WebView2 console, manually add a region (see Step 5) to verify the map can display regions at all

6. **Provide Info:**
   - What does Management Client show? (screenshot of region list)
   - What does Smart Client status say?
   - What's in Debug Output?
   - Any errors in WebView2 Console?
