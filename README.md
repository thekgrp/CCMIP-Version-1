# CoreCommandMIP Version 1

**Core Command MIP Plugin for Milestone XProtect**

A comprehensive MIP (Milestone Integration Platform) plugin for XProtect Smart Client and Event Server that provides real-time track monitoring, mapping, and alarm management capabilities.

## Features

### ??? Interactive Mapping
- **Dual Map Support**: Leaflet (OpenStreetMap) and Mapbox satellite imagery
- **Real-time Track Visualization**: Display moving targets with custom icons
- **Track Classification Icons**: Person, Vehicle, Drone, Aerial, Bird, Unknown
- **Track History Trails**: Configurable tail length for movement history
- **Region/Zone Display**: Geofenced areas with customizable colors and opacity
- **Auto-Zoom**: Intelligent zoom to track selected targets

### ?? Track List View
- **Live Track Table**: Real-time updating data grid
- **Track Details**: ID, Classification, Confidence, Position, Velocity, Altitude
- **Source Display**: Shows detection sources for each track
- **Click-to-Select**: Click track in list to focus on map
- **Broadcast Selection**: Share track selection across view items

### ?? Alarm System
- **Automatic Alarm Generation**: Triggered by server-side alarming tracks
- **Event Server Integration**: Alarms logged to XProtect Event Manager
- **Priority Levels**: High (Drone/Aerial), Medium (Person/Vehicle), Low (Others)
- **Alarm Deduplication**: Prevents spam, allows re-alarm after timeout
- **XProtect Rules Integration**: Can trigger notifications and actions

### ?? Configuration
- **Multiple Site Support**: Configure multiple remote tracking servers
- **Per-Site Settings**: Independent configuration for each site
- **Region Selection**: Choose which geofenced regions to display
- **Polling Interval**: Configurable update frequency (1-60 seconds)
- **Map Provider Choice**: Select between Leaflet or Mapbox
- **Credentials Management**: Secure storage of server credentials

## Architecture

### Components

**Smart Client Plugin (`Client/`)**
- `CoreCommandMIPViewItemWpfUserControl`: Main map view
- `CoreCommandMIPTrackListViewItemWpfUserControl`: Track list view
- `MapTemplate.cs`: Leaflet map HTML generator
- `MapboxTemplate.cs`: Mapbox map HTML generator
- `TrackAlarmManager.cs`: Client-side alarm detection

**Event Server Plugin (`Background/`)**
- `CoreCommandMIPBackgroundPlugin.cs`: Event Server entry point
- `TrackAlarmEventHandler.cs`: Server-side alarm processing

**Configuration (`Admin/`)**
- `CoreCommandMIPUserControl`: Management Client settings UI
- `RemoteServerSettings.cs`: Configuration data model

**Data Models**
- `SmartMapLocation.cs`: Track data structure
- `RegionModels.cs`: Geofence definitions
- `TrackAlarmData.cs`: Alarm event structure

## Requirements

- **Milestone XProtect**: 2020 R2 or later
- **Development**: Visual Studio 2019+ with .NET Framework 4.8
- **Runtime**: Milestone MIPSDK installed
- **Optional**: Mapbox account for satellite imagery

## Installation

### Smart Client

1. Build solution in Release mode
2. Copy `CoreCommandMIP.dll` to:
   ```
   C:\Program Files\Milestone\MIPSDK\MIPPlugins\CoreCommandMIP\
   ```
3. Copy `assets\` folder (icon images) to same location
4. Restart Smart Client

### Event Server

1. Copy `CoreCommandMIP.dll` to:
   ```
   C:\Program Files\Milestone\XProtect Event Server\MIPPlugins\CoreCommandMIP\
   ```
2. Restart "Milestone XProtect Event Server" service:
   ```powershell
   Restart-Service "Milestone XProtect Event Server"
   ```

## Configuration

### Adding a Site

1. Open **Management Client**
2. Navigate to **MIP Plugin** ? **CoreCommandMIP**
3. Add new site configuration:
   - **Name**: Site identifier
   - **Base URL**: Remote server endpoint (e.g., `https://server.com/api`)
   - **Username/Password**: API credentials
   - **Default Position**: Latitude, Longitude, Zoom level
   - **Polling Interval**: Update frequency in seconds

### Selecting Regions

1. In site configuration, click **Select Regions**
2. Check regions to display on map
3. Regions must be active on remote server
4. Save configuration

### Using in Smart Client

1. Create new **View** or open existing
2. Add **CoreCommandMIP View Item** from toolbox
3. Select configured site from dropdown
4. Map displays with real-time tracks
5. Optional: Add **Track List** view item for tabular display

## Remote Server API

The plugin expects a REST API with these endpoints:

### Get Tracks (with Change Tracking)
```
GET /tracks?counter={last_counter}
```

**Response:**
```json
{
  "changeCounter": 12345,
  "hasChanges": true,
  "tracks": [
    {
      "trackId": 123,
      "latitude": 38.7866,
      "longitude": -104.7886,
      "altitude": 1500.0,
      "velocity": 25.5,
      "classificationLabel": "Drone",
      "classificationConfidence": 0.95,
      "alarming": true,
      "sources": ["Sensor-1", "Sensor-2"],
      "timestamp": "2024-01-27T12:00:00Z"
    }
  ]
}
```

### Get Region List
```
GET /regions
```

**Response:**
```json
[
  {
    "id": 1,
    "guidId": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Test Zone",
    "active": true
  }
]
```

### Get Region Details
```
GET /regions/{id}
```

**Response:**
```json
{
  "id": 1,
  "name": "Test Zone",
  "active": true,
  "exclusion": false,
  "color": "#ff6b6b",
  "fill": 0.2,
  "vertices": [
    {"latitude": 38.7866, "longitude": -104.7886},
    {"latitude": 38.7900, "longitude": -104.7886},
    {"latitude": 38.7900, "longitude": -104.7850}
  ]
}
```

## Development

### Building

```bash
# Restore NuGet packages
nuget restore CoreCommandMIP.sln

# Build in Release mode
msbuild CoreCommandMIP.sln /p:Configuration=Release
```

### Debugging

**Smart Client:**
1. Set `CoreCommandMIP` as startup project
2. Debug Configuration ? Command: `Smart Client.exe` path
3. F5 to debug

**Event Server:**
- Attach to `VideoOS.Server.EventServer.exe` process
- Set breakpoints in `Background/` code

### Logging

**Debug Output:**
- All components log to Visual Studio Output window
- Prefix: `[TrackAlarm]`, `[MapTemplate]`, etc.

**XProtect Logs:**
- Management Client ? System ? Logs
- Filter by source: `CoreCommandMIP`

## Troubleshooting

### Icons Not Showing
- Ensure `assets/` folder is in plugin directory
- Check icon files: `person.png`, `vehicle.png`, `drone.png`, `aerial.jpg`, `bird.png`, `arrow.png`
- Icons automatically fall back to SVG if images not found

### Regions Not Displaying
- Verify regions are selected in configuration
- Check region has `active: true`
- Check region has ?3 vertices
- View browser console for JavaScript errors

### Alarms Not Appearing
- Verify Event Server plugin loaded (check logs)
- Ensure Event Server service is running
- Check track has `alarming: true`
- Configure XProtect Rules to display alarms in Smart Client

### Map Not Loading
- Check WebView2 is installed
- Clear WebView2 cache: `%LocalAppData%\CoreCommandMIP\WebView2\`
- Check browser console (F12 in map view)

## Technology Stack

- **.NET Framework 4.8**: Core framework
- **WPF**: User interface
- **WebView2**: Embedded browser for maps
- **Leaflet 1.9.4**: Open-source mapping library
- **Mapbox GL JS 3.0.1**: Satellite imagery provider
- **Milestone MIP SDK**: XProtect integration

## License

Proprietary - Contact for licensing information

## Version History

### Version 1.0.0 (2024-01-27)
- Initial release
- Leaflet and Mapbox map support
- Track visualization with custom icons
- Region/geofence display
- Track list data grid
- Automatic alarm generation
- Event Server integration
- Multi-site configuration
- Change-based polling optimization

## Support

For issues, questions, or feature requests, contact your administrator.

## Credits

Developed for integration with Milestone XProtect VMS.
