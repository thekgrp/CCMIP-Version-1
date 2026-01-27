# URGENT FIXES NEEDED - Regions and Icons

## Issue 1: Region Parser Doesn't Handle GUID Array

### Problem:
Server returns: `{"Results":["guid1","guid2",...]}`  
Parser expects: `{"Results":[{"Id":1,"Name":"Region",...}]}`

### Fix for `Client/RemoteServerDataProvider.cs` - Line 631-634:

**FIND THIS CODE (around line 631):**
```csharp
		var itemsText = listMatch.Groups[1].Value;
		var itemMatches = Regex.Matches(itemsText, "\\{([^}]+)\\}");
		
		System.Diagnostics.Debug.WriteLine($"Found {itemMatches.Count} region items");
```

**REPLACE WITH:**
```csharp
		var itemsText = listMatch.Groups[1].Value;
		var itemMatches = Regex.Matches(itemsText, "\\{([^}]+)\\}");
		
		// Check if array contains objects or GUID strings
		if (itemMatches.Count == 0)
		{
			// Parse as GUID array
			System.Diagnostics.Debug.WriteLine("No objects found, parsing as GUID array");
			var guidMatches = Regex.Matches(itemsText, "\"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})\"", RegexOptions.IgnoreCase);
			System.Diagnostics.Debug.WriteLine($"Found {guidMatches.Count} GUID regions");
			
			foreach (Match guidMatch in guidMatches)
			{
				var guid = guidMatch.Groups[1].Value;
				result.Add(new RegionListItem
				{
					Id = result.Count + 1,
					Name = guid.Substring(0, 8), // Use first 8 chars as display name
					Active = true,
					GuidId = guid // Store full GUID
				});
			}
			
			System.Diagnostics.Debug.WriteLine($"Parsed {result.Count} GUID regions");
			return result;
		}
		
		System.Diagnostics.Debug.WriteLine($"Found {itemMatches.Count} region items");
```

---

## Issue 2: Fetch Region Details Uses Wrong ID

### Fix for `Client/RemoteServerDataProvider.cs` - FetchRegionDetailsAsync (line 583):

**CHANGE METHOD SIGNATURE:**
```csharp
// OLD:
internal async Task<RegionDefinition> FetchRegionDetailsAsync(string baseUrl, string username, string password, long regionId, CancellationToken cancellationToken)

// NEW:
internal async Task<RegionDefinition> FetchRegionDetailsAsync(string baseUrl, string username, string password, string regionIdOrGuid, CancellationToken cancellationToken)
```

**CHANGE URL CONSTRUCTION (line 593):**
```csharp
// OLD:
var response = await SendAsync(CreateRequest(new Uri(string.Format(CultureInfo.InvariantCulture, "{0}/rest/regions/{1}", normalizedBaseUrl, regionId))), null, cancellationToken).ConfigureAwait(false);

// NEW:
var response = await SendAsync(CreateRequest(new Uri(string.Format(CultureInfo.InvariantCulture, "{0}/rest/regions/{1}", normalizedBaseUrl, regionIdOrGuid))), null, cancellationToken).ConfigureAwait(false);
```

---

## Issue 3: Smart Client Calls Wrong Method Parameter

### Fix for `Client/CoreCommandMIPViewItemWpfUserControl.xaml.cs` - Line 983:

**FIND:**
```csharp
var regionDef = await provider.FetchRegionDetailsAsync(baseUrl, username, password, regionItem.Id, CancellationToken.None).ConfigureAwait(false);
```

**REPLACE WITH:**
```csharp
var regionIdOrGuid = string.IsNullOrEmpty(regionItem.GuidId) ? regionItem.Id.ToString() : regionItem.GuidId;
var regionDef = await provider.FetchRegionDetailsAsync(baseUrl, username, password, regionIdOrGuid, CancellationToken.None).ConfigureAwait(false);
```

---

## Issue 4: Region Definition Parser Needs Update

The server returns:
```json
{"Error":false,"Results":{"Active":true,"Exclusion":true,"Name":"Exclusion1","Vertices":[...]}}
```

### Fix for `Client/RemoteServerDataProvider.cs` - ParseRegionDefinition (after line 661):

**ADD THIS CHECK at the beginning of ParseRegionDefinition:**
```csharp
private static RegionDefinition ParseRegionDefinition(string payload)
{
	if (string.IsNullOrWhiteSpace(payload))
	{
		return null;
	}

	System.Diagnostics.Debug.WriteLine($"ParseRegionDefinition payload: {payload}");

	// Check if Results contains an object (not array)
	var resultsMatch = Regex.Match(payload, "\"Results\"\\s*:\\s*\\{([^}]+)\\}", RegexOptions.Singleline);
	if (!resultsMatch.Success)
	{
		// Try original parsing
		// ... existing code continues ...
	}
	
	// Extract from Results object wrapper
	var objectContent = resultsMatch.Groups[1].Value;
	// Continue parsing...
```

---

## Issue 5: Icons Not Showing

### Problem:
Asset files not being copied to output directory.

### Fix in Visual Studio:

1. **For EACH icon file** (`arrow.png`, `bird.png`, `drone.png`, `person.png`, `vehicle.png`, `aerial.jpg`):
   - Right-click file in Solution Explorer
   - Properties
   - **Build Action:** `Content`
   - **Copy to Output Directory:** `Copy always`

OR manually edit `CoreCommandMIP.csproj`:

```xml
<ItemGroup>
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
  <Content Include="assets\person.png">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
  <Content Include="assets\vehicle.png">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

---

## Testing After Fixes:

1. **Build solution**
2. **Management Client:**
   - Click "Refresh" regions
   - Should show 9 regions with GUID names
3. **Smart Client:**
   - Regions should render on map
   - Icons should show for each track type

---

## Quick Verification Commands:

Check if assets copy:
```powershell
Get-ChildItem bin\Debug\assets\*.png, bin\Debug\assets\*.jpg
```

Should list all 6 icon files.

---

**Apply these fixes in order, then build and test!**
