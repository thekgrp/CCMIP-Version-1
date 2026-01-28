# ?? OFFLINE MAP DATA DEPLOYMENT GUIDE

## Overview

Smart Clients running on remote machines need local map tile caches for:
- ? Optimal performance
- ? Offline capability
- ? Reduced bandwidth usage
- ? Faster map loading

**?? IMPORTANT:** Milestone XProtect **DOES NOT** automatically push WebView2 cache data to Smart Clients. You must deploy map data manually.

---

## Understanding the Cache

### Cache Location
```
%LocalAppData%\CoreCommandMIP\WebView2Cache\
C:\Users\[Username]\AppData\Local\CoreCommandMIP\WebView2Cache\
```

### What's Cached
- Map tiles (satellite/street imagery)
- 3D building data (if Mapbox + 3D enabled)
- 3D terrain elevation data (if Mapbox + 3D enabled)
- Vector tiles (Mapbox only)
- Fonts and icons

### Cache Sizes

| Configuration | Zoom 8-12 | Zoom 8-16 | Zoom 8-18 |
|---------------|-----------|-----------|-----------|
| **Leaflet (OSM)** | 50 MB | 500 MB | 2 GB |
| **Mapbox 2D** | 100 MB | 800 MB | 3 GB |
| **Mapbox 3D** | 200 MB | 2 GB | 8 GB |

**Recommendation:** Cache zoom levels 8-16 for best balance (covers typical site views).

---

## Deployment Methods

### Option 1: Manual Deployment ? (Simplest)

**Best for:** 1-5 Smart Client machines

**Steps:**
1. **Generate Cache on ONE machine:**
   ```powershell
   # Run Smart Client with internet access
   # Open CoreCommandMIP view
   # Pan and zoom around the site area
   # Enable 3D if needed
   # Let run for 5-10 minutes
   ```

2. **Locate Cache:**
   ```powershell
   # Open File Explorer, paste:
   %LOCALAPPDATA%\CoreCommandMIP\WebView2Cache\
   ```

3. **Package Cache:**
   ```powershell
   # Right-click WebView2Cache folder
   # Send to ? Compressed (zipped) folder
   # Name: MapCache-20240127.zip
   ```

4. **Deploy to Remote Machines:**
   ```powershell
   # Copy zip to each remote machine
   # Extract to:
   C:\Users\[Username]\AppData\Local\CoreCommandMIP\WebView2Cache\
   # Overwrite existing files
   ```

---

### Option 2: Automated Deployment with PowerShell ?? (Recommended)

**Best for:** 5-50 Smart Client machines

**Use the provided script:**

```powershell
# Create package from current machine's cache
.\Deploy-MapCache.ps1 -Action Package

# Package and deploy to multiple computers
.\Deploy-MapCache.ps1 -TargetComputers @('SC-01','SC-02','SC-03')

# Deploy existing package
.\Deploy-MapCache.ps1 -Action Deploy `
    -PackagePath .\MapCache-20240127.zip `
    -TargetComputers @('SC-01','SC-02')
```

**Requirements:**
- Admin rights on target machines
- Admin$ share accessible (firewall rules)
- Smart Clients closed during deployment

**Script Features:**
- ? Validates source cache exists
- ? Creates compressed package
- ? Tests remote machine connectivity
- ? Clears old cache
- ? Deploys new cache
- ? Reports success/failure

---

### Option 3: Network Share Deployment ??? (Centralized)

**Best for:** Any number of machines, centralized management

**Setup:**

1. **Create Network Share:**
   ```powershell
   # On file server:
   New-Item -Path "E:\XProtect\MapCache" -ItemType Directory
   New-SmbShare -Name "MapCache" -Path "E:\XProtect\MapCache" `
       -ReadAccess "Domain Users"
   ```

2. **Populate Share:**
   ```powershell
   # Copy WebView2Cache folder to share
   Copy-Item "$env:LOCALAPPDATA\CoreCommandMIP\WebView2Cache\*" `
       -Destination "\\fileserver\MapCache\" -Recurse
   ```

3. **Configure Plugin (Code Change Required):**
   
   Edit `Client\CoreCommandMIPViewItemWpfUserControl.xaml.cs`:
   
   ```csharp
   private void PrepareWebViewHost()
   {
       try
       {
           // Try network share first
           var networkCache = @"\\fileserver\MapCache";
           if (Directory.Exists(networkCache))
           {
               _webViewUserDataFolder = networkCache;
               System.Diagnostics.Debug.WriteLine($"Using network cache: {networkCache}");
           }
           else
           {
               // Fallback to local cache
               _webViewUserDataFolder = RemoteServerSettings.GetWebView2CacheFolder();
               Directory.CreateDirectory(_webViewUserDataFolder);
               System.Diagnostics.Debug.WriteLine($"Using local cache: {_webViewUserDataFolder}");
           }
       }
       catch (Exception ex)
       {
           _statusTextBlock.Text = $"Unable to prepare map control: {ex.Message}";
       }
   }
   ```

**PROS:**
- ? Centralized updates (update once, all clients get it)
- ? No per-machine deployment
- ? Easy to manage

**CONS:**
- ? Requires network access (slower than local)
- ? Single point of failure
- ? Bandwidth usage on network

---

### Option 4: SCCM/Intune Deployment ??? (Enterprise)

**Best for:** Large enterprises with existing deployment tools

**SCCM Package:**

1. **Create Application:**
   ```
   Name: CoreCommandMIP Map Cache
   Version: 2024.01.27
   Source: \\sccm-share\MapCache-20240127.zip
   ```

2. **Installation Script:**
   ```powershell
   $targetPath = "$env:LOCALAPPDATA\CoreCommandMIP\WebView2Cache"
   $sourcePath = "$PSScriptRoot\WebView2Cache"
   
   # Remove old cache
   if (Test-Path $targetPath) {
       Remove-Item "$targetPath\*" -Recurse -Force
   }
   else {
       New-Item -Path $targetPath -ItemType Directory -Force
   }
   
   # Copy new cache
   Copy-Item "$sourcePath\*" -Destination $targetPath -Recurse -Force
   
   Write-Host "Map cache deployed successfully"
   exit 0
   ```

3. **Detection Method:**
   ```powershell
   $targetPath = "$env:LOCALAPPDATA\CoreCommandMIP\WebView2Cache"
   $markerFile = "$targetPath\.version"
   
   if ((Test-Path $markerFile) -and 
       ((Get-Content $markerFile) -eq "2024.01.27")) {
       Write-Host "Installed"
       exit 0
   }
   exit 1
   ```

4. **Deploy to Collection:**
   - Target: "Smart Client Machines"
   - Purpose: Required
   - Schedule: Business hours

---

## Offline Tile Generation

For completely offline/air-gapped environments:

### Using MapTiler Desktop

1. **Download MapTiler:**
   - https://www.maptiler.com/desktop/
   - Free version available

2. **Create Offline Tileset:**
   ```
   Center Point: [Your site lat/lon]
   Zoom Levels: 8-16
   Format: MBTiles
   Coverage: Draw bounding box around site
   ```

3. **Export:**
   - Size estimate shown before export
   - Can take 30 minutes to hours depending on area
   - Outputs: tileset.mbtiles

4. **Convert to WebView2 Format:**
   ```powershell
   # Use mb-util or similar
   mb-util tileset.mbtiles tiles/
   # Results in folder structure:
   # tiles/
   #   8/
   #   9/
   #   ...
   #   16/
   ```

5. **Deploy:**
   - Copy tiles folder to WebView2Cache location
   - Follow deployment methods above

### Using Mapbox Offline (Commercial)

**Requires:** Mapbox commercial license with offline support

1. **Mapbox Maps SDK:**
   - https://docs.mapbox.com/mapbox-gl-js/guides/install/

2. **Download Region:**
   ```javascript
   // In browser console with Mapbox loaded
   const bounds = [
       [-104.8, 38.7],  // Southwest
       [-104.7, 38.8]   // Northeast
   ];
   
   map.offline.addRegion({
       id: 'site-region',
       bounds: bounds,
       minZoom: 8,
       maxZoom: 16
   });
   ```

3. **Export:**
   - Downloads to browser cache
   - Extract from cache or use export API

---

## Maintenance & Updates

### When to Update Cache

? **Update cache when:**
- New construction/buildings in site area
- Terrain changes
- New roads/infrastructure
- Switching map providers
- Enabling/disabling 3D

?? **Recommended schedule:**
- **Urban areas:** Quarterly
- **Rural areas:** Annually
- **Static sites:** As needed

### Cache Versioning

Create a version marker file:

```powershell
# After creating/updating cache package
$version = Get-Date -Format "yyyy.MM.dd"
Set-Content "$env:LOCALAPPDATA\CoreCommandMIP\WebView2Cache\.version" -Value $version

# Check version on remote machines
$version = Get-Content "\\SC-01\C$\Users\operator\AppData\Local\CoreCommandMIP\WebView2Cache\.version"
Write-Host "SC-01 cache version: $version"
```

### Monitoring Cache Usage

```powershell
# Check cache size on multiple machines
$computers = @('SC-01','SC-02','SC-03')
foreach ($pc in $computers) {
    $path = "\\$pc\C$\Users\operator\AppData\Local\CoreCommandMIP\WebView2Cache"
    if (Test-Path $path) {
        $size = (Get-ChildItem $path -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
        Write-Host "$pc : $([Math]::Round($size, 2)) MB"
    }
}
```

---

## Troubleshooting

### Cache Not Used

**Symptoms:**
- Smart Client downloading tiles from internet
- Slow map loading

**Solutions:**
1. Check cache folder exists:
   ```powershell
   Test-Path "$env:LOCALAPPDATA\CoreCommandMIP\WebView2Cache"
   ```

2. Check folder permissions:
   ```powershell
   icacls "$env:LOCALAPPDATA\CoreCommandMIP\WebView2Cache"
   ```

3. Check Smart Client user:
   - Cache must be in correct user's AppData
   - Smart Client must run as same user

### Cache Corruption

**Symptoms:**
- Black map tiles
- Missing 3D buildings
- JavaScript errors in console

**Solutions:**
```powershell
# Clear cache completely
Remove-Item "$env:LOCALAPPDATA\CoreCommandMIP\WebView2Cache\*" -Recurse -Force

# Restart Smart Client
# Let it rebuild cache naturally
```

### Deployment Failures

**Error:** "Access Denied"
- **Solution:** Run PowerShell as Administrator

**Error:** "Path not found"
- **Solution:** Create parent directories first:
  ```powershell
  New-Item -Path "$env:LOCALAPPDATA\CoreCommandMIP" -ItemType Directory -Force
  ```

**Error:** "File in use"
- **Solution:** Close Smart Client before deployment

---

## Best Practices

### Security

? **DO:**
- Use NTFS permissions to protect cache folder
- Deploy over secure networks only
- Verify package integrity (checksums)
- Use least-privilege accounts

? **DON'T:**
- Share cache packages via public internet
- Use world-writable network shares
- Include sensitive site data in offline packages

### Performance

? **DO:**
- Cache appropriate zoom levels (8-16 typical)
- Use local cache when possible
- Pre-deploy before go-live
- Test on pilot machine first

? **DON'T:**
- Over-cache (zoom 1-22 = huge)
- Use network share for 3D (too slow)
- Deploy during business hours (bandwidth)

### Documentation

Keep records of:
- Cache version deployed
- Date deployed
- Target machines
- Zoom levels cached
- Map provider/style used

---

## Quick Reference

### File Locations

| Item | Path |
|------|------|
| Local Cache | `%LOCALAPPDATA%\CoreCommandMIP\WebView2Cache\` |
| Package Script | `Deploy-MapCache.ps1` |
| Cache Marker | `.version` file in cache folder |

### Common Commands

```powershell
# Check cache size
(Get-ChildItem "$env:LOCALAPPDATA\CoreCommandMIP\WebView2Cache" -Recurse | 
    Measure-Object -Property Length -Sum).Sum / 1MB

# Clear cache
Remove-Item "$env:LOCALAPPDATA\CoreCommandMIP\WebView2Cache\*" -Recurse -Force

# Package cache
.\Deploy-MapCache.ps1 -Action Package

# Deploy to machines
.\Deploy-MapCache.ps1 -TargetComputers @('SC-01','SC-02')
```

---

## Support

For issues with:
- **Deployment:** Contact XProtect administrator
- **Cache generation:** Review map provider docs
- **Plugin behavior:** Check plugin logs
- **This guide:** Review with integrator

---

## Summary

? **Milestone does NOT auto-deploy cache data**
? **Use Deploy-MapCache.ps1 for automation**
? **Plan 100MB-5GB per machine**
? **Update quarterly for urban, annually for rural**
? **Test on pilot machine first**

**Next Steps:**
1. Generate cache on reference machine
2. Run Deploy-MapCache.ps1 to package
3. Deploy to pilot machine
4. Test map loading
5. Deploy to remaining machines
6. Document deployment for future updates
