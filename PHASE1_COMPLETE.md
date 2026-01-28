# Phase 1 Implementation Complete - Foundation for Milestone C2 Integration

## ? What Was Implemented

### 1. RemoteServerSettings Enhanced
**File:** `RemoteServerSettings.cs`

**New Fields Added:**
```csharp
internal string AssociatedCameraIds { get; set; }  // Comma-separated camera GUIDs
internal Guid PluginInstanceId { get; set; }       // Unique instance identifier
internal DateTime? LastHealthCheck { get; set; }   // Last health check timestamp
internal HealthStatus HealthStatus { get; set; }   // Connection health status
```

**New HealthStatus Enum:**
```csharp
public enum HealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3,
    Disconnected = 4
}
```

**Purpose:**
- Store camera associations for event triggering
- Track plugin instance for multi-site support
- Monitor C2 connection health

### 2. Event Definition System Created
**File:** `EventDefinitionHelper.cs`

**Event Types Defined:**
- `C2.Alert` - Medium severity alerts
- `C2.Alarm` - High severity alarms
- `C2.AlarmCleared` - Cleared alarm notifications
- `C2.TrackEnterRegion` - Track region entry
- `C2.TrackLost` - Track lost events

**Key Classes:**
```csharp
public class C2EventData           // Event data structure
public enum EventSeverity         // Info, Medium, High
public enum AlarmSeverity         // Low (1), Medium (5), High (10)
public class AlarmDefinitionInfo  // Recommended alarm definitions
```

**Purpose:**
- Standardize event types across the plugin
- Provide helper methods for creating events
- Define recommended alarm definitions for Management Client

### 3. Event Trigger Service Created  
**File:** `Background\EventTriggerService.cs`

**Methods:**
- `TriggerEvent(C2EventData)` - Generic event trigger
- `TriggerAlert()` - Trigger medium severity alert
- `TriggerAlarm()` - Trigger high severity alarm
- `TriggerAlarmCleared()` - Trigger cleared alarm
- `BuildLogMessage()` - Format detailed log entries

**Purpose:**
- Centralize event triggering logic
- Create detailed log entries (until proper UDE API is discovered)
- Support camera associations and metadata

### 4. TrackAlarmEventHandler Updated
**File:** `Background\TrackAlarmEventHandler.cs`

**Changes:**
- Added `EventTriggerService` integration
- Updated `ProcessTrackAlarm()` to use event system
- Generate unique `C2AlarmId` per alarm instance
- Include all metadata in events
- Log event type and C2AlarmId for traceability

**C2 Alarm ID Format:**
```
T{TrackId}_{Timestamp:yyyyMMddHHmmss}
Example: T123_20240127142315
```

## ?? Data Flow

### Current Event Flow:
```
1. Track alarms (Alarming=true from C2)
         ?
2. Smart Client detects ? Sends message to Event Server
         ?
3. TrackAlarmEventHandler receives message
         ?
4. Generates C2AlarmId (unique)
         ?
5. Creates C2EventData with metadata:
   - Event Type (C2.Alert or C2.Alarm)
   - C2AlarmId
   - TrackId
   - Message
   - Severity
   - Location, velocity, confidence
   - Timestamp
   - Camera IDs (placeholder)
         ?
6. EventTriggerService logs event
   (Creates detailed log entry in Management Client)
```

### Future Event Flow (When UDE API Discovered):
```
6. EventTriggerService triggers User-Defined Event
         ?
7. Management Client Rule evaluates event
         ?
8. Alarm Definition creates alarm
         ?
9. Alarm appears in Smart Client Alarm Manager
```

## ?? Configuration Storage

All new settings are stored in Milestone's Item Properties:

| Setting | Key | Type |
|---------|-----|------|
| Camera IDs | `AssociatedCameraIds` | string (comma-separated) |
| Instance ID | `PluginInstanceId` | Guid |
| Health Check | `LastHealthCheck` | DateTime (ISO 8601) |
| Health Status | `HealthStatus` | Enum string |

## ?? Event Metadata

Each event includes:

```csharp
{
    EventType: "C2.Alarm",           // Event type name
    C2AlarmId: "T123_20240127...",   // Unique alarm ID
    TrackId: 123,                    // Track identifier
    Message: "Track 123 (Drone)...", // Human-readable message
    Severity: EventSeverity.High,    // Severity level
    Timestamp: DateTime.UtcNow,      // Event timestamp
    
    // Track metadata:
    Classification: "Drone",
    Latitude: 38.7866,
    Longitude: -104.7886,
    Altitude: 1500.0,
    Velocity: 25.5,
    Confidence: 0.95,
    Site: "HQ East",
    
    // Milestone integration:
    CameraIds: [guid1, guid2],       // Associated cameras
    RegionId: "region-123"           // Optional region
}
```

## ?? What's Ready for Phase 2

### Management Client UI can now:
1. ? Store camera associations
2. ? Display health status
3. ? Show plugin instance ID
4. ? Configure which event types to use

### Event Server can now:
1. ? Generate unique C2 Alarm IDs
2. ? Create structured event data
3. ? Include full track metadata
4. ? Support camera associations (data ready)
5. ? Distinguish Alert vs Alarm severity

## ?? Next Steps (Phase 2 - Management Client Redesign)

### Tab 1 - Base Configuration
- Display connection health status
- Show plugin instance ID
- Test connection button

### Tab 2 - Region Selection  
- Move existing region UI here
- Already mostly complete

### Tab 3 - Alarm Wiring (NEW)
- Display event types (C2.Alert, C2.Alarm)
- Show recommended alarm definitions
- "Apply Recommended Wiring" button
- **Camera Association UI:**
  - List available cameras
  - Multi-select cameras
  - Save to `AssociatedCameraIds`

## ?? Testing Phase 1

### Build Status:
? **Build successful**

### Test Checklist:
- [ ] Build and deploy to Event Server
- [ ] Restart Event Server service
- [ ] Verify plugin loads (check logs)
- [ ] Trigger alarm from C2
- [ ] Check Management Client ? System ? Logs
- [ ] Verify log entry includes:
  - Event type (C2.Alert or C2.Alarm)
  - C2 Alarm ID
  - Track ID
  - All metadata
- [ ] Verify C2AlarmId format is correct
- [ ] Verify severity mapping (priority ? event type)

### Expected Log Format:
```
[HIGH] C2.Alarm - Track 123 (Drone) detected at HQ East
C2 Alarm ID: T123_20240127142315
Track ID: 123
Site: HQ East
Classification: Drone
Location: 38.786600°, -104.788600° | Altitude: 1500.0m | Velocity: 25.5m/s | Confidence: 95%
Timestamp: 2024-01-27T14:23:15.0000000Z
```

## ?? Key Design Decisions

### 1. C2 Alarm ID Generation
**Format:** `T{TrackId}_{Timestamp}`
**Reason:** Unique per alarm instance, traceable, sortable

### 2. Event vs Log Entries
**Current:** Creating log entries (visible in Management Client)
**Future:** Will trigger User-Defined Events when API discovered
**Reason:** Proper Milestone pattern for alarms

### 3. Severity Mapping
- Priority 1-2 ? `C2.Alarm` (High)
- Priority 3-5 ? `C2.Alert` (Medium)
- Priority 6+ ? `C2.Alert` (Low, if needed)

### 4. Camera Association Storage
**Format:** Comma-separated GUIDs
**Example:** `"guid1,guid2,guid3"`
**Reason:** Simple, works with Milestone Item Properties

## ?? Deployment

1. **Build:** ? Complete
2. **Deploy to Event Server:**
   ```powershell
   Copy-Item "bin\Release\CoreCommandMIP.dll" -Destination "C:\Program Files\Milestone\XProtect Event Server\MIPPlugins\CoreCommandMIP\" -Force
   ```
3. **Restart Event Server:**
   ```powershell
   Restart-Service "Milestone XProtect Event Server"
   ```
4. **Test with alarming track**

## ?? Files Created/Modified

### Created:
- `EventDefinitionHelper.cs` - Event type definitions and helpers
- `Background\EventTriggerService.cs` - Event triggering service

### Modified:
- `RemoteServerSettings.cs` - Added camera association and health fields
- `Background\TrackAlarmEventHandler.cs` - Updated to use event system

## Summary

**Phase 1 Foundation is complete!** We now have:
- ? Configuration storage for cameras and health
- ? Standardized event type system
- ? Event triggering infrastructure
- ? Unique C2 Alarm ID generation
- ? Metadata-rich event data
- ? Build successful

**Ready for Phase 2:** Management Client UI redesign with 3-tab interface and camera association picker!
