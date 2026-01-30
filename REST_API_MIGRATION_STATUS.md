# REST API Migration - Event & Alarm Creation

## ? COMPLETED

Created REST API-based alarm creation system to replace .NET ConfigurationItems API.

## Files Created

### 1. **`Admin/MilestoneRestClient.cs`**
- Full REST API client for Milestone XProtect Management Server
- Uses Windows Authentication (no credentials needed!)
- Methods:
  - `CreateUserDefinedEventAsync()` - POST to `/api/rest/v1/userDefinedEvents`
  - `GetUserDefinedEventByNameAsync()` - GET from `/api/rest/v1/userDefinedEvents`
  - `CreateAlarmDefinitionAsync()` - POST to `/api/rest/v1/alarmDefinitions`
  - `GetAlarmDefinitionByNameAsync()` - GET from `/api/rest/v1/alarmDefinitions`

### 2. **`Admin/C2AlarmWiringRest.cs`**
- REST-based wrapper for event + alarm creation
- Single method: `EnsureUdeAndAlarmDefinitionAsync()`
- Creates both Event and Alarm in one call
- Integrated with DiagnosticLogger

### 3. **Updated `Admin/CoreCommandMIPUserControlTabbed.cs`**
- `ButtonCreateEvents_Click()` now uses REST API
- Cleaner async/await pattern
- Better error messages

## Required NuGet Packages

**NEED TO ADD:**
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

**NEED TO REFERENCE:**
```xml
<Reference Include="System.Net.Http" />
```

## Benefits of REST Approach

? **No .NET API Dependencies** - Just HTTP!  
? **Standard Authentication** - Uses current Windows identity  
? **Better Error Handling** - HTTP status codes  
? **More Portable** - Can be used from any language  
? **Cleaner Code** - No ServerTask waiting loops  
? **Immediate** - No cache refresh needed  

## How It Works

### Authentication
```csharp
var handler = new HttpClientHandler
{
    UseDefaultCredentials = true  // Uses Management Client's Windows identity!
};
var client = new HttpClient(handler);
```

**You're already authenticated!** The Management Client process is running under your Windows account, so REST API calls use that identity automatically.

### Creating a UDE
```http
POST /api/rest/v1/userDefinedEvents
Content-Type: application/json

{
  "name": "C2_Alert_NRK"
}
```

### Creating an Alarm
```http
POST /api/rest/v1/alarmDefinitions
Content-Type: application/json

{
  "name": "C2_Alert_NRK",
  "description": "Alert from C2",
  "eventTypeGroup": "User-defined Events",
  "eventType": "User-defined Event",
  "sourceList": ["userDefinedEvents/abc-123"],
  "priority": "Medium",
  "category": "C2 Alarms",
  "relatedCameraList": ["cameras/cam-guid"]
}
```

## Next Steps

1. **Fix .csproj** - Add Newtonsoft.Json and System.Net.Http references
2. **Test** - Run in Management Client
3. **Remove Old Code** - Delete `C2AlarmWiringVerified.cs` (no longer needed)

## Troubleshooting

### "HttpClient not found"
- Add reference to `System.Net.Http` in .csproj
- It's part of .NET Framework 4.8

### "Newtonsoft not found"
- Run: `dotnet add package Newtonsoft.Json`
- Or manually add PackageReference to .csproj

### "401 Unauthorized"
- Check that Management Client is running as current Windows user
- Ensure user has permissions to create events/alarms

### "404 Not Found"
- Check Management Server URL construction
- Verify API Gateway is installed and running

## REST API Documentation

See Milestone docs:
- https://doc.developer.milestonesys.com/mipvmsapi/api/config-rest/v1/
- POST `/api/rest/v1/userDefinedEvents`
- POST `/api/rest/v1/alarmDefinitions`

## Code Comparison

### OLD (.NET API):
```csharp
var folder = new UserDefinedEventFolder();
var task = folder.AddUserDefinedEvent("C2_Alert");
WaitForTaskOrThrow(task, "UDE creation failed");

// Wait for cache refresh...
Thread.Sleep(500);
folder.ClearChildrenCache();
```

### NEW (REST API):
```csharp
using (var client = new MilestoneRestClient(msUri))
{
    var result = await client.CreateUserDefinedEventAsync("C2_Alert");
    // Done! No waiting, no cache clearing!
}
```

**Much cleaner!** ??
