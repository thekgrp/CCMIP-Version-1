# Event Server Plugin Deployment Guide

## What Needs to Be Deployed

**The entire `CoreCommandMIP.dll`** contains both:
- Smart Client components
- Event Server components (Background plugin)

## Event Server Plugin Components

### Main Plugin File
**`Background\CoreCommandMIPBackgroundPlugin.cs`**
- Entry point for Event Server
- Initializes `TrackAlarmEventHandler`
- Runs automatically on Event Server due to `TargetEnvironments`

### Alarm Handler
**`Background\TrackAlarmEventHandler.cs`**
- Listens for alarm messages from Smart Client
- Creates XProtect log entries
- Processes track alarms

## Deployment Locations

### Check These Folders on Event Server:

**Option 1 (Most Common):**
```
C:\Program Files\Milestone\XProtect Event Server\MIPPlugins\CoreCommandMIP\
```

**Option 2:**
```
C:\ProgramData\Milestone\XProtect Event Server\MIPPlugins\CoreCommandMIP\
```

**Option 3 (MIPSDK Dev):**
```
C:\Program Files\Milestone\MIPSDK\MIPPlugins\CoreCommandMIP\
```

### What to Copy:
```
CoreCommandMIP.dll          (Required - contains everything)
CoreCommandMIP.pdb          (Optional - for debugging)
assets\*.png                (Optional - if icons needed on server)
```

## Deployment Steps

### Manual Deployment

1. **Build in Release mode:**
   ```
   Build ? Configuration Manager ? Release
   Build ? Build Solution
   ```

2. **Locate built DLL:**
   ```
   bin\Release\CoreCommandMIP.dll
   ```

3. **Copy to Event Server:**
   - If Event Server is on **same machine**: Copy to plugin folder
   - If Event Server is on **different machine**: Remote Desktop or network share

4. **Stop Event Server service:**
   ```powershell
   Stop-Service "Milestone XProtect Event Server"
   ```

5. **Replace DLL:**
   ```
   Copy bin\Release\CoreCommandMIP.dll to Event Server plugin folder
   ```

6. **Start Event Server service:**
   ```powershell
   Start-Service "Milestone XProtect Event Server"
   ```

### Automatic Deployment (MIP SDK)

If you're using the MIP SDK properly, it should auto-deploy when you build.

**Check if auto-deploy is working:**
1. Build your project
2. Check Event Server plugin folder - DLL should update automatically
3. If not, service may need restart

## Verification

### Method 1: Check Windows Event Viewer

1. Open **Event Viewer** on Event Server machine
2. Go to **Windows Logs ? Application**
3. Look for source "Milestone XProtect Event Server"
4. Should see:
   ```
   CoreCommandMIP background thread: Now starting...
   Track Alarm Event Handler initialized on Event Server
   ```

### Method 2: Check XProtect Logs

1. Open **Management Client**
2. Go to **System ? Logs**
3. Filter by source: "CoreCommandMIP"
4. Should see:
   ```
   Track Alarm Event Handler initialized on Event Server
   ```

### Method 3: Check Service Status

```powershell
Get-Service "Milestone XProtect Event Server"
```

Should show: **Running**

## Troubleshooting

### Plugin Not Loading

**Check:**
1. DLL is in correct folder
2. Event Server service is running
3. DLL is not blocked (right-click ? Properties ? Unblock)
4. All dependencies are present

**View Event Server logs:**
```
C:\ProgramData\Milestone\XProtect Event Server\Logs\
```

### No Alarm Messages Received

**Debug on Event Server:**
1. Attach debugger to Event Server process (if possible)
2. Or add more logging to `TrackAlarmEventHandler`
3. Check if message receiver is registered

**Test message sending:**
- Smart Client sends: `CoreCommandMIPDefinition.TrackAlarmMessageId`
- Event Server receives: Same message ID
- If mismatch, messages won't arrive

### Restart Required

**Always restart Event Server service after DLL changes:**
```powershell
Restart-Service "Milestone XProtect Event Server"
```

## Development Tips

### Quick Deploy Script

**File: `deploy-to-eventserver.ps1`**
```powershell
# Configuration
$EventServerPath = "C:\Program Files\Milestone\XProtect Event Server\MIPPlugins\CoreCommandMIP"
$BuildPath = "bin\Release"

# Stop service
Write-Host "Stopping Event Server..."
Stop-Service "Milestone XProtect Event Server" -Force

# Copy DLL
Write-Host "Copying plugin..."
Copy-Item "$BuildPath\CoreCommandMIP.dll" -Destination $EventServerPath -Force

# Start service
Write-Host "Starting Event Server..."
Start-Service "Milestone XProtect Event Server"

Write-Host "Deployment complete!"
```

### Remote Event Server

If Event Server is on **different machine**:

1. **Share the plugin folder** on Event Server
2. **Map network drive** on development machine
3. **Modify deploy script** to use network path
4. **Use remote PowerShell** to restart service:
   ```powershell
   Invoke-Command -ComputerName EventServerName -ScriptBlock {
       Restart-Service "Milestone XProtect Event Server"
   }
   ```

## Current Status

? Background plugin exists: `CoreCommandMIPBackgroundPlugin.cs`
? Alarm handler exists: `TrackAlarmEventHandler.cs`
? Plugin targets Event Server: `TargetEnvironments = Service`
? Code has debug logging for troubleshooting
? **Needs deployment to Event Server**
? **Needs Event Server service restart**

## Next Steps

1. **Build the project** (Release mode)
2. **Locate the Event Server plugin folder**
3. **Copy CoreCommandMIP.dll** to that folder
4. **Restart Event Server service**
5. **Check logs** for initialization messages
6. **Test with alarming track**
7. **Monitor Debug Output and Management Client logs**
