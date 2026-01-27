# Track Icons - Assets Deployment Fix

## Problem
The icon files exist in the project's `assets` folder, but the MIP plugin runs from a different location (Milestone's plugin directory), and the assets aren't being copied there.

## Where Does the Plugin Actually Run?

**Build the project and check Debug Output.** You'll see something like:

```
Assembly path: C:\Program Files\Milestone\MIPSDK\MIPPlugins\CoreCommandMIP.dll
```

OR

```
Assembly path: C:\Program Files\Milestone\XProtect Smart Client\MIPPlugins\CoreCommandMIP.dll
```

## Solution Options

### Option 1: Manual Copy (Quick Test)

1. **Check Debug Output** for the actual path
2. **Navigate to that folder** (e.g., `C:\Program Files\Milestone\...`)
3. **Copy the entire `assets` folder** from your project to that location
4. **Restart Smart Client**

Example:
```
Copy from: C:\Users\YourName\source\repos\CoreCommandMIP\assets\
Copy to:   C:\Program Files\Milestone\MIPSDK\MIPPlugins\CoreCommandMIP\assets\
```

### Option 2: Automatic Post-Build Copy

Add this to your `.csproj` file (before the closing `</Project>` tag):

```xml
<Target Name="CopyAssetsToMIPPlugin" AfterTargets="Build">
  <PropertyGroup>
    <!-- Adjust this path to match your Milestone installation -->
    <MIPPluginPath>C:\Program Files\Milestone\MIPSDK\MIPPlugins\CoreCommandMIP</MIPPluginPath>
  </PropertyGroup>
  
  <ItemGroup>
    <AssetsFiles Include="$(ProjectDir)assets\**\*.*" />
  </ItemGroup>
  
  <Copy SourceFiles="@(AssetsFiles)" 
        DestinationFolder="$(MIPPluginPath)\assets\%(RecursiveDir)" 
        SkipUnchangedFiles="true"
        Condition="Exists('$(MIPPluginPath)')" />
  
  <Message Text="Copied assets to MIP plugin directory" Importance="high" 
           Condition="Exists('$(MIPPluginPath)')" />
</Target>
```

### Option 3: Embed Icons as Resources (Best Long-Term Solution)

Instead of loading from disk, embed icons in the DLL itself.

**Steps:**

1. **In Visual Studio, select all icon files in `assets` folder**
2. **Right-click ? Properties**
3. **Build Action: Embedded Resource**
4. **Build**

Then modify `GetIconDataUri` to read from embedded resources instead of disk:

```csharp
private static string GetIconDataUri(string iconName)
{
    if (_iconCache.ContainsKey(iconName))
    {
        return _iconCache[iconName];
    }

    try
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var resourceName = $"CoreCommandMIP.assets.{iconName}";
        
        System.Diagnostics.Debug.WriteLine($"Loading embedded resource: {resourceName}");
        
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                
                var base64 = Convert.ToBase64String(bytes);
                var extension = Path.GetExtension(iconName).ToLowerInvariant();
                var mimeType = extension == ".png" ? "image/png" : "image/jpeg";
                var dataUri = $"data:{mimeType};base64,{base64}";
                _iconCache[iconName] = dataUri;
                
                System.Diagnostics.Debug.WriteLine($"? Loaded embedded {iconName}: {bytes.Length} bytes");
                return dataUri;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"? Embedded resource not found: {resourceName}");
                
                // List available resources for debugging
                var names = assembly.GetManifestResourceNames();
                System.Diagnostics.Debug.WriteLine($"Available resources: {string.Join(", ", names)}");
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"? Error loading embedded icon {iconName}: {ex.Message}");
    }

    System.Diagnostics.Debug.WriteLine($"Using fallback SVG icon for {iconName}");
    return GetFallbackIcon();
}
```

## Recommended Approach

**For now (quick fix):**
1. Build project
2. Check Debug Output for the assembly path
3. Manually copy `assets` folder to that location
4. Restart Smart Client
5. Icons should work!

**For permanent fix:**
- Use Option 3 (Embedded Resources) - icons will always be available because they're inside the DLL itself

## Verification

After copying assets, rebuild and check Debug Output:

**Should see:**
```
=== Searching for person.png ===
Assembly dir: C:\...\MIPPlugins
Looking for icon: C:\...\MIPPlugins\assets\person.png
File exists: True
? Successfully loaded person.png: 12345 bytes
Person icon loaded: 16789 chars
```

**Not:**
```
File exists: False
Assets folder exists: False
? File NOT FOUND
Using fallback SVG icon
```

## Current Status

? Code compiles successfully
? Icon loading logic works
? Fallback SVG works
? Need to copy assets to MIP plugin directory

**Next Step:** Build, check Debug Output for path, copy assets folder there!
