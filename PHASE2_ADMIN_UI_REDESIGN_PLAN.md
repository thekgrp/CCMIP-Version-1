# Phase 2: Management Client Admin UI Redesign to Tabbed Interface
# This guide explains the redesign from single-panel to 3-tab interface

## Current Structure
Single scrollable panel with all controls mixed together

## New Structure (3 Tabs)

### Tab 1 - Base Configuration
**Purpose:** Core connection settings and health status

**Controls:**
- Name (textBoxName)
- Server Address (textBoxServerAddress)
- Port (numericUpDownPort)
- Use HTTPS (checkBoxUseHttps)
- Username (textBoxUsername)
- Password (textBoxPassword)
- API Key (textBoxApiKey)
- **NEW: Plugin Instance ID** (labelInstanceId - read-only display)
- **NEW: Health Status** (labelHealthStatus with colored indicator)
- **NEW: Last Health Check** (labelLastHealthCheck)
- Test Connection (buttonTestConnection)

### Tab 2 - Map & Region Configuration
**Purpose:** Map settings and region selection (existing functionality)

**Controls:**
- Map Provider (comboBoxMapProvider)
- Mapbox Token (textBoxMapboxToken)
- Get Token Link (linkLabelGetMapboxToken)
- Enable Caching (checkBoxEnableMapCaching)
- Default Latitude (textBoxLatitude)
- Default Longitude (textBoxLongitude)
- Default Zoom (textBoxZoom)
- Polling Interval (numericUpDownPollingInterval)
- **Region Selection:**
  - Regions Label (labelRegions)
  - Regions List (checkedListBoxRegions)
  - Refresh Button (buttonRefreshRegions)
- **Site Preview:**
  - Preview Label (labelSitePreview)
  - WebView2 (webViewSitePreview)

### Tab 3 - Alarm Wiring & Cameras (NEW)
**Purpose:** Event/Alarm configuration and camera association

**Controls to Add:**
- **Event Types Group:**
  - Label: "Available Event Types"
  - ListBox showing: C2.Alert, C2.Alarm
  - Description label for each
  
- **Alarm Definitions Group:**
  - Label: "Recommended Alarm Definitions"
  - DataGridView showing:
    - Alarm Name
    - Source Event
    - Severity
    - Status (Created/Not Created)
  
- **Apply Wiring Button:**
  - buttonApplyWiring
  - "Apply Recommended Wiring"
  - Creates event definitions and alarm definitions
  
- **Camera Association Group:**
  - Label: "Associated Cameras"
  - CheckedListBox: checkedListBoxCameras
  - Shows all cameras in system
  - Multi-select enabled
  - Refresh Cameras button
  - Selected count display

## Implementation Steps

### Step 1: Backup Current Designer
Copy current Designer.cs to Designer.cs.backup

### Step 2: Use Visual Studio Designer
1. Open Admin\CoreCommandMIPUserControl.cs in Visual Studio
2. Open in Designer view
3. Delete existing controls (keep note of names/properties)
4. Add TabControl (name: tabControl1)
5. Add 3 TabPages:
   - tabPage1 (Text: "Base Configuration")
   - tabPage2 (Text: "Map & Regions")
   - tabPage3 (Text: "Alarm Wiring")

### Step 3: Rebuild Tab 1
Add all connection-related controls to tabPage1:
- Use GroupBox "Connection Settings"
- Use GroupBox "Status" for health info
- Position controls in logical groups

### Step 4: Move Tab 2 Controls
Move map and region controls to tabPage2:
- Use GroupBox "Map Settings"
- Use GroupBox "Region Selection"
- Keep WebView2 preview

### Step 5: Create Tab 3
Add new controls for alarm wiring:
- Use GroupBox "Event Types"
- Use GroupBox "Alarm Definitions"  
- Use GroupBox "Camera Association"

### Step 6: Update Code-Behind
Update CoreCommandMIPUserControl.cs to:
- Handle new controls
- Load camera list
- Implement Apply Wiring button
- Display health status
- Show instance ID

## Alternative: Programmatic Creation

Since Designer editing is complex, we can create controls programmatically:

```csharp
private void InitializeTabControl()
{
    var tabControl = new TabControl();
    tabControl.Dock = DockStyle.Fill;
    
    // Tab 1: Base Configuration
    var tab1 = new TabPage("Base Configuration");
    CreateBaseConfigTab(tab1);
    tabControl.TabPages.Add(tab1);
    
    // Tab 2: Map & Regions
    var tab2 = new TabPage("Map & Regions");
    CreateMapRegionsTab(tab2);
    tabControl.TabPages.Add(tab2);
    
    // Tab 3: Alarm Wiring
    var tab3 = new TabPage("Alarm Wiring");
    CreateAlarmWiringTab(tab3);
    tabControl.TabPages.Add(tab3);
    
    this.Controls.Clear();
    this.Controls.Add(tabControl);
}
```

## Recommendation

For Phase 2, I recommend:
1. Creating a NEW UserControl (CoreCommandMIPUserControlV2.cs)
2. Building it with TabControl from scratch
3. Copying logic from old UserControl
4. Testing thoroughly
5. Then replacing old one

This avoids breaking existing configuration while developing new UI.

## Next Steps

Would you like me to:
A. Create a new UserControl with tabs from scratch (cleanest)
B. Modify existing Designer.cs programmatically (more complex)
C. Provide a Visual Studio Designer guide (manual steps)

Choose option A for best results!
