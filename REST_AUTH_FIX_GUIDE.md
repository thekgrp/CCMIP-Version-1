# REST API Authentication Fix

## Problem
The Milestone REST API is returning "401 Unauthorized" errors when trying to create User-Defined Events and Alarm Definitions. The error occurs because:

1. The REST API requires **Basic Authentication** (username:password in Base64)
2. The current implementation uses `UseDefaultCredentials = true` which uses Windows authentication
3. The Milestone Management Server REST API doesn't accept Windows authentication in the same way

## Error Log
```
ERROR in REST API Error - C2_Alert_NRK:
  Message: Failed to create User-Defined Event 'C2_Alert_NRK'. 
  Status: Unauthorized, Response: {"Message":"Authorization has been denied for this request."}
```

## Solution

### 1. MilestoneRestClient.cs (DONE)
Added two constructors:
- One that takes `username` and `password` for Basic Authentication
- One that uses Windows authentication as fallback

### 2. C2AlarmWiringRest.cs (DONE)
Modified `EnsureUdeAndAlarmDefinitionAsync` to accept optional `username` and `password` parameters and pass them to the REST client.

### 3. Required: Pass Credentials from UI
The credentials need to be passed from wherever the alarm creation is initiated. This typically happens when:

- Saving configuration in the Admin UI
- Initializing alarms for a new site

## How to Fix Calls to EnsureUdeAndAlarmDefinitionAsync

You need to find where `EnsureUdeAndAlarmDefinitionAsync` is being called and pass credentials:

```csharp
// OLD:
var result = await C2AlarmWiringRest.EnsureUdeAndAlarmDefinitionAsync(
    udeName: "C2_Alert_NRK",
    alarmDefinitionName: "C2 Alert - NRK",
    alarmDescription: "CoreCommand track alert for NRK site",
    alarmPriority: "5");

// NEW:
var settings = RemoteServerSettings.LoadFromConfiguration(/* ... */);
var result = await C2AlarmWiringRest.EnsureUdeAndAlarmDefinitionAsync(
    udeName: "C2_Alert_NRK",
    alarmDefinitionName: "C2 Alert - NRK",
    alarmDescription: "CoreCommand track alert for NRK site",
    alarmPriority: "5",
    cameraPaths: null,
    username: settings.DefaultUsername,  // From configuration
    password: settings.DefaultPassword);  // From configuration
```

## Configuration Requirements

Make sure the Admin UI configuration has:
1. **Username** field filled in (textBoxUsername)
2. **Password** field filled in (textBoxPassword)

These credentials must be for a Milestone user that has permission to:
- Create User-Defined Events
- Create Alarm Definitions

Typically this would be an Administrator account.

## Testing the Fix

1. Open Management Client
2. Go to the CoreCommandMIP plugin configuration
3. Enter Milestone administrator username and password
4. Save the configuration
5. The REST API should now successfully create events and alarms

## Alternative: Windows Authentication

If you want to use Windows authentication instead:
- The Windows user running the Management Client must have appropriate permissions
- Pass `null` for both username and password
- The REST client will fall back to Windows credentials

However, Basic Authentication with explicit credentials is more reliable for the Milestone REST API.
