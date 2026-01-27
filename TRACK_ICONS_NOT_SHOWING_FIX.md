# Track Icons Not Showing - Fix Guide

## Problem
Track tails are showing on the map but the track icons (markers) are not visible.

## Root Cause
The `MapTemplate.GetIconDataUri()` method is returning `null` when icon files can't be loaded, and this null is being replaced with empty string `""`, resulting in markers with no iconUrl.

## Solution

### File: `Client\MapTemplate.cs`

**Need to modify the `GetIconDataUri` method around line 20-42:**

### Step 1: Add Debug Logging

**Find these lines (around line 24):**
```csharp
var iconPath = Path.Combine(assemblyDir, "assets", iconName);

if (File.Exists(iconPath))
```

**Change to:**
```csharp
var iconPath = Path.Combine(assemblyDir, "assets", iconName);

System.Diagnostics.Debug.WriteLine($"Looking for icon: {iconPath}");

if (File.Exists(iconPath))
```

### Step 2: Add Success Logging

**Find this line (around line 33):**
```csharp
_iconCache[iconName] = dataUri;
return dataUri;
```

**Change to:**
```csharp
_iconCache[iconName] = dataUri;
System.Diagnostics.Debug.WriteLine($"Loaded icon {iconName}: {dataUri.Substring(0, Math.Min(50, dataUri.Length))}...");
return dataUri;
```

### Step 3: Add Failure Logging

**Find this block (around line 35-40):**
```csharp
			}
		}
		catch
		{
			// Fallback to arrow if image can't be loaded
		}

		return null;
```

**Change to:**
```csharp
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"Icon file not found: {iconPath}");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error loading icon {iconName}: {ex.Message}");
		}

		// Return fallback SVG icon
		return GetFallbackIcon();
```

### Step 4: Add Fallback Icon Method

**Add this new method AFTER `GetIconDataUri` and BEFORE `GetMapHtml`:**

```csharp
	private static string GetFallbackIcon()
	{
		// Simple SVG arrow as fallback
		const string svgArrow = "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32' viewBox='0 0 32 32'><circle cx='16' cy='16' r='14' fill='#1e88e5' stroke='white' stroke-width='2'/><path d='M16 8 L16 20 M11 15 L16 20 L21 15' stroke='white' stroke-width='2' fill='none'/></svg>";
		var bytes = System.Text.Encoding.UTF8.GetBytes(svgArrow);
		var base64 = Convert.ToBase64String(bytes);
		return $"data:image/svg+xml;base64,{base64}";
	}
```

## Why This Fixes It

1. **Debug Logging**: Shows where the code is looking for icons and whether they're found
2. **Fallback Icon**: Instead of returning `null` (which becomes ``), returns a simple SVG arrow icon
3. **Better Error Handling**: Catches and logs specific errors

## After Making Changes

1. **Build** the project
2. **Run** Smart Client
3. **Check Debug Output** (View ? Output ? Debug)
4. **Look for messages** like:
   ```
   Looking for icon: C:\...\assets\person.png
   Loaded icon person.png: data:image/png;base64,iVBORw0KGgoAAAANSU...
   ```

## Expected Result

- If icon files ARE found: PNG/JPG icons will display
- If icon files are NOT found: Blue circle with arrow (SVG fallback) will display
- Either way, you'll see SOMETHING instead of nothing

## Possible Issues

### Issue 1: Icon files not being copied to output
**Check:** `assets` folder should be in the same directory as the DLL

**Fix:** In Visual Studio, select all icon files in Solution Explorer:
1. Right-click each file in `assets` folder
2. Properties
3. Set "Copy to Output Directory" to "Copy if newer"
4. Rebuild

### Issue 2: DLL is in different location
**Check Debug Output** for the path it's checking

**Solution:** Copy `assets` folder to the location shown in debug output

## Test Steps

1. Make the code changes above
2. Build
3. Run Smart Client
4. Open Output Window (View ? Output, select "Debug")
5. Look for icon loading messages
6. Check if track markers appear on map
7. If you see "Icon file not found", check Issue 1 above

## Current Icon Files

Should be in `assets` folder:
- `person.png` - Person classification
- `vehicle.png` - Vehicle classification  
- `drone.png` - Drone classification
- `aerial.jpg` - Aerial classification
- `bird.png` - Animal/Bird classification
- `arrow.png` - Unknown/default classification

All these exist in your project, so the issue is likely they're not being copied to the build output directory.
