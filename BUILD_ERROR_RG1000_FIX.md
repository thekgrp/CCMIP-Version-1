# Build Error Fix - RG1000 "An item with the same key has already been added"

## Problem
The build is failing with error:
```
error RG1000: Unknown build error, 'An item with the same key has already been added.'
```

This is a known WPF XAML compilation issue where the generated `.g.cs` files conflict.

## Root Cause
The error occurs when:
1. WPF's MSBuild targets generate a `.g.cs` file for XAML pages
2. The build system tries to compile it twice due to caching or project structure issues
3. This specifically affects `CoreCommandMIPTrackListViewItemWpfUserControl.xaml`

## Solution Steps

### Step 1: Clean Everything
I've already cleaned:
- `obj` folder
- `bin` folder  
- `.vs` folder (Visual Studio cache)

### Step 2: Verify Project File Structure

The `.csproj` should have:

**Compile Entry (REQUIRED):**
```xml
<Compile Include="Client\CoreCommandMIPTrackListViewItemWpfUserControl.xaml.cs">
  <DependentUpon>CoreCommandMIPTrackListViewItemWpfUserControl.xaml</DependentUpon>
</Compile>
```

**Page Entry (REQUIRED):**
```xml
<Page Include="Client\CoreCommandMIPTrackListViewItemWpfUserControl.xaml">
  <SubType>Designer</SubType>
  <Generator>MSBuild:Compile</Generator>
</Page>
```

### Step 3: Rebuild in Visual Studio

1. **Close Visual Studio** completely
2. **Delete these folders manually** if they still exist:
   - `CoreCommandMIP\obj`
   - `CoreCommandMIP\bin`
   - `CoreCommandMIP\.vs`
3. **Reopen Visual Studio**
4. **Build ? Clean Solution**
5. **Build ? Rebuild Solution**

### Step 4: If Still Failing

If the error persists after a clean rebuild, try these in order:

**Option A: Restart Visual Studio**
- Close Visual Studio
- Delete `.vs` folder again
- Reopen and rebuild

**Option B: Check for Directory.Build.props conflicts**
```powershell
Get-Content Directory.Build.props
```
Should be empty or minimal:
```xml
<Project>
</Project>
```

**Option C: Manually fix the Page element**
If the Page element for TrackListViewItemWpfUserControl is missing `<SubType>Designer</SubType>`, add it:

Before:
```xml
<Page Include="Client\CoreCommandMIPTrackListViewItemWpfUserControl.xaml">
  <Generator>MSBuild:Compile</Generator>
</Page>
```

After:
```xml
<Page Include="Client\CoreCommandMIPTrackListViewItemWpfUserControl.xaml">
  <SubType>Designer</SubType>
  <Generator>MSBuild:Compile</Generator>
</Page>
```

**Option D: Check obj folder after failed build**
After a failed build, check:
```
obj\Release\Client\CoreCommandMIPTrackListViewItemWpfUserControl.g.cs
```
If this file exists with errors, delete the entire `obj` folder and rebuild.

**Option E: Nuclear option - Delete and re-add the XAML file**
1. In Solution Explorer, remove `CoreCommandMIPTrackListViewItemWpfUserControl.xaml` (don't delete from disk)
2. Remove `CoreCommandMIPTrackListViewItemWpfUserControl.xaml.cs` 
3. Save project
4. Add both files back (Add ? Existing Item)
5. Visual Studio should auto-configure the DependentUpon relationship
6. Rebuild

## Current Status

? All build artifacts cleaned
? Project XML structure verified as valid
?? Need to rebuild in Visual Studio

## Next Steps

**Please try rebuilding the project now in Visual Studio.** 

If it still fails:
1. Copy the exact error message from the Output window
2. Check if there are any other warnings or errors before the RG1000 error
3. Try Option A (restart Visual Studio) first
4. Then try Options B-E if needed

## Prevention

To prevent this in the future:
- Always clean before switching between Debug/Release configurations
- Don't manually edit generated `.g.cs` files in the `obj` folder
- Keep `Directory.Build.props` minimal or empty
- Use "Rebuild Solution" instead of "Build" when making XAML changes
