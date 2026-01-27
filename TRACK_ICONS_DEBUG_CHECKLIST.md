# Track Icons Debug - Comprehensive Checklist

## Current Situation
- ? Regions ARE working (you see region add/delete in console)
- ? No icon debug messages showing
- ? Track icons not visible

## Diagnostic Steps

### Step 1: Check if Tracks Are Being Received

**In Debug Output window, search for:**
- "Polling remote server"
- "Track" messages
- "HandleTrackUpdate"

**You should see:**
```
Polling remote server for track updates...
Track 123 retrieved from remote server.
```

**If you see NO track messages:**
- Server isn't sending tracks
- Credentials might be wrong
- Server might not have active tracks

### Step 2: Force Map Reload

The map HTML might be cached. To force reload:

**Option A: Clear WebView2 cache**
1. Close Smart Client completely
2. Delete this folder: `C:\Users\[YourName]\AppData\Local\CoreCommandMIP\WebView2`
3. Reopen Smart Client

**Option B: Change a parameter**
In Management Client, change any map setting (like zoom level), save, then reload Smart Client.

### Step 3: Verify Debug Code Is in Generated HTML

Add this TEMPORARY code to see the actual HTML:

**File: `Client\CoreCommandMIPViewItemWpfUserControl.xaml.cs`**

Find the `BuildMapDocument` method (around line 670) and add this at the END:

```csharp
// TEMPORARY DEBUG - Save HTML to file
try
{
    var debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "map-debug.html");
    File.WriteAllText(debugPath, html);
    System.Diagnostics.Debug.WriteLine($"Saved map HTML to: {debugPath}");
}
catch { }

return html;
```

Then:
1. Build and run
2. Check your Desktop for `map-debug.html`
3. Open it in Notepad
4. Search for "=== Icon Map Loaded ===" 
5. Search for "console.log('Person icon length:"

**If these are NOT in the file:**
- The new code didn't get into the HTML
- Need to rebuild properly

**If these ARE in the file:**
- The HTML is correct
- Problem is with track creation or JavaScript execution

### Step 4: Check if Tracks Exist

**In Smart Client status bar (bottom of map view), you should see:**
- "Track 123 - Person" (or similar)
- NOT "No active tracks"
- NOT "Waiting for remote targets"

**If it says "No active tracks":**
- Server has no tracks to display
- Icons won't show because there are no tracks!

### Step 5: Manual Track Creation Test

If no real tracks exist, we can test with fake data.

**Add this TEMPORARY button to XAML:**

File: `Client\CoreCommandMIPViewItemWpfUserControl.xaml`

Add after the other buttons:
```xaml
<Button Content="Test Track Icon" Click="TestTrackIcon_Click" Margin="5"/>
```

**Add this code to code-behind:**

File: `Client\CoreCommandMIPViewItemWpfUserControl.xaml.cs`

```csharp
private async void TestTrackIcon_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var testScript = @"
            console.log('=== MANUAL TRACK TEST ===');
            window.updateTracks([{
                id: 999,
                lat: 38.7866,
                lng: -104.7886,
                label: 'Test Track',
                details: 'This is a test',
                color: '#00ff00',
                tail: 200,
                classification: 'person'
            }]);
            console.log('=== TEST TRACK CREATED ===');
        ";
        
        await _mapView.ExecuteScriptAsync(testScript);
        _statusTextBlock.Text = "Test track created - check console and map";
    }
    catch (Exception ex)
    {
        _statusTextBlock.Text = $"Test failed: {ex.Message}";
    }
}
```

Click this button and check:
1. Console should show "=== MANUAL TRACK TEST ==="
2. Console should show icon debug messages
3. Map should show a green track marker

## Quick Diagnosis Matrix

| What You See | What It Means | Solution |
|--------------|---------------|----------|
| "No active tracks" in status | Server has no tracks | Wait for server to have tracks, or use test button |
| No "Polling" messages in Debug | Credentials wrong | Check Management Client settings |
| Console shows ONLY region messages | Map HTML is old/cached | Delete WebView2 cache folder |
| Console shows "Icon Map Loaded" but icons are empty | Icon files not found | Check assets folder in bin directory |
| No console messages at all except regions | Tracks not being created | Check if server has tracks |

## Most Likely Issues

### Issue #1: No Tracks from Server (Most Common)
**Check:** Status bar says "No active tracks" or "Waiting for remote targets"
**Solution:** 
- Server needs to have active tracks
- Or use the Test Track button to manually create one

### Issue #2: Cached Map HTML
**Check:** map-debug.html doesn't have "console.log" statements
**Solution:** 
- Delete WebView2 cache
- Rebuild project (Clean Solution, then Build)

### Issue #3: Icons Not Loading from Disk
**Check:** Debug Output shows "Icon file not found"
**Solution:**
- Check `bin\Release\assets\` folder has PNG files
- Right-click asset files in Visual Studio ? Properties ? Copy to Output Directory = "Copy if newer"

## Next Steps

1. **First:** Check Smart Client status bar - does it show tracks?
2. **Second:** Check Debug Output for "Track" messages
3. **Third:** Delete WebView2 cache and restart
4. **Fourth:** Add the test button and try manual track creation
5. **Fifth:** Save map-debug.html and check if it has the console.log code

**Report back what you find!**
