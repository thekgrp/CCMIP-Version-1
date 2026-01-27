# ICONS MISSING - MANUAL FIX REQUIRED

## Problem:
Asset icon files exist but aren't being copied to output directory.

## Root Cause:
The `CoreCommandMIP.csproj` file has assets listed but without `<CopyToOutputDirectory>Always</CopyToOutputDirectory>`.

## Manual Fix Steps:

### 1. Open CoreCommandMIP.csproj in a text editor

### 2. Find lines 174-180 (should look like this):
```xml
<Content Include="assets\aerial.jpg" />
<Content Include="assets\arrow.png" />
<Content Include="assets\bird.png" />
<Content Include="assets\drone.png" />
<Content Include="assets\logo.jpg" />
<Content Include="assets\person.png" />
<Content Include="assets\vehicle.png" />
```

### 3. Replace with this (add CopyToOutputDirectory for all except logo.jpg):
```xml
<Content Include="assets\aerial.jpg">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
<Content Include="assets\arrow.png">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
<Content Include="assets\bird.png">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
<Content Include="assets\drone.png">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
<Content Include="assets\logo.jpg" />
<Content Include="assets\person.png">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
<Content Include="assets\vehicle.png">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
```

### 4. Save the file

### 5. Rebuild the solution

### 6. Verify assets copied:
Check that these files exist:
- `bin\Debug\assets\aerial.jpg`
- `bin\Debug\assets\arrow.png`
- `bin\Debug\assets\bird.png`
- `bin\Debug\assets\drone.png`
- `bin\Debug\assets\person.png`
- `bin\Debug\assets\vehicle.png`

## Alternative: Use Visual Studio Properties

For each asset file (aerial.jpg, arrow.png, bird.png, drone.png, person.png, vehicle.png):

1. Right-click in Solution Explorer
2. Properties
3. **Build Action:** Content
4. **Copy to Output Directory:** Copy always

## After Fix:

Once assets are copying to output, icons will show:
- ?? Drone icon for drone tracks
- ?? Vehicle icon for vehicle tracks
- ?? Person icon for person tracks
- ?? Bird icon for bird/animal tracks
- ?? Aerial icon for aerial tracks
- ?? Arrow icon for unknown types

---

**This is the ONLY remaining fix needed for icons to work!**
