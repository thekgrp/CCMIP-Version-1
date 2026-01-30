using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections;
using System.Reflection;
using VideoOS.Platform;
using VideoOS.Platform.Admin;
using VideoOS.Platform.ConfigurationItems;

namespace CoreCommandMIP.Admin
{
    /// <summary>
    /// Tabbed configuration interface for CoreCommandMIP (Phase 2 redesign)
    /// </summary>
    public partial class CoreCommandMIPUserControlTabbed : UserControl
    {
        internal event EventHandler ConfigurationChangedByUser;
        
        private TabControl tabControl;
        private Item _item;

        // Tab 1: Base Configuration
        private TextBox textBoxName;
        private TextBox textBoxServerAddress;
        private NumericUpDown numericUpDownPort;
        private CheckBox checkBoxUseHttps;
        private TextBox textBoxUsername;
        private TextBox textBoxPassword;
        private TextBox textBoxApiKey;
        private Label labelInstanceId;
        private Label labelHealthStatus;
        private Label labelLastHealthCheck;
        private Button buttonTestConnection;

        // Tab 2: Map & Regions
        private ComboBox comboBoxMapProvider;
        private TextBox textBoxMapboxToken;
        private LinkLabel linkLabelGetMapboxToken;
        private CheckBox checkBoxEnableMapCaching;
        private CheckBox checkBoxEnable3DMap;
        private CheckBox checkBoxEnable3DBuildings;
        private CheckBox checkBoxEnable3DTerrain;
        private TextBox textBoxLatitude;
        private TextBox textBoxLongitude;
        private TextBox textBoxZoom;
        private NumericUpDown numericUpDownPollingInterval;
        private CheckedListBox checkedListBoxRegions;
        private Button buttonRefreshRegions;
        private Microsoft.Web.WebView2.WinForms.WebView2 webViewSitePreview;

        // Tab 3: Alarm Wiring (NEW)
        private ListBox listBoxEventTypes;
        private DataGridView dataGridViewAlarms;
        private Button buttonCreateEvents;
        private Button buttonCreateAlarms;
        private Label labelWiringStatus;
        private CheckedListBox checkedListBoxCameras;
        private Button buttonRefreshCameras;
        private Label labelCameraCount;

        public CoreCommandMIPUserControlTabbed()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Display name property used by ItemManager
        /// </summary>
        internal string DisplayName
        {
            get { return textBoxName?.Text ?? string.Empty; }
            set { if (textBoxName != null) textBoxName.Text = value; }
        }

        /// <summary>
        /// Fill the control with item configuration (called by ItemManager)
        /// </summary>
        internal void FillContent(Item item)
        {
            Init(item);
        }

        /// <summary>
        /// Clear all fields (called by ItemManager)
        /// </summary>
        internal void ClearContent()
        {
            textBoxName.Text = string.Empty;
            textBoxServerAddress.Text = string.Empty;
            numericUpDownPort.Value = 443;
            checkBoxUseHttps.Checked = true;
            textBoxUsername.Text = string.Empty;
            textBoxPassword.Text = string.Empty;
            textBoxApiKey.Text = string.Empty;
            
            // Tab 2
            comboBoxMapProvider.SelectedIndex = 0;
            textBoxMapboxToken.Text = string.Empty;
            checkBoxEnableMapCaching.Checked = true;
            textBoxLatitude.Text = "45.5098";  // Oregon Zoo
            textBoxLongitude.Text = "-122.7161";  // Oregon Zoo
            textBoxZoom.Text = "14";  // Good zoom for viewing a specific location
            numericUpDownPollingInterval.Value = 1;
            checkedListBoxRegions.Items.Clear();
            
            // Tab 3
            checkedListBoxCameras.Items.Clear();
            
            labelInstanceId.Text = "-";
            labelHealthStatus.Text = "Unknown";
            labelHealthStatus.ForeColor = Color.Gray;
            labelLastHealthCheck.Text = "Never";
        }

        /// <summary>
        /// Update the item with current values (called by ItemManager for validation/save)
        /// </summary>
        internal void UpdateItem(Item item)
        {
            if (item == null) return;
            
            // Update _item reference to ensure we have latest
            _item = item;
            
            item.Name = DisplayName;
            SaveToItem();
        }

        /// <summary>
        /// Validate user entries
        /// </summary>
        internal bool TryValidate(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (string.IsNullOrWhiteSpace(textBoxServerAddress.Text))
            {
                errorMessage = "Server address is required.";
                tabControl.SelectedIndex = 0; // Switch to Tab 1
                textBoxServerAddress.Focus();
                return false;
            }

            if (!double.TryParse(textBoxLatitude.Text, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                errorMessage = "Latitude must be a valid number.";
                tabControl.SelectedIndex = 1; // Switch to Tab 2
                textBoxLatitude.Focus();
                return false;
            }

            if (!double.TryParse(textBoxLongitude.Text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                errorMessage = "Longitude must be a valid number.";
                tabControl.SelectedIndex = 1; // Switch to Tab 2
                textBoxLongitude.Focus();
                return false;
            }

            if (!double.TryParse(textBoxZoom.Text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                errorMessage = "Zoom must be a valid number.";
                tabControl.SelectedIndex = 1; // Switch to Tab 2
                textBoxZoom.Focus();
                return false;
            }

            return true;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Create TabControl
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 9F);

            // Create tabs
            var tab1 = new TabPage("Base Configuration");
            var tab2 = new TabPage("Map & Regions");
            var tab3 = new TabPage("Alarm Wiring");

            CreateBaseConfigTab(tab1);
            CreateMapRegionsTab(tab2);
            CreateAlarmWiringTab(tab3);

            tabControl.TabPages.Add(tab1);
            tabControl.TabPages.Add(tab2);
            tabControl.TabPages.Add(tab3);

            // Add event handler for tab changes
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            this.Controls.Add(tabControl);
            this.Size = new Size(800, 600);
            this.ResumeLayout(false);
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            // When Map & Regions tab is selected, refresh the map preview
            if (tabControl.SelectedIndex == 1 && _item != null)
            {
                var settings = CollectCurrentSettings();
                UpdateSitePreview(settings);
            }
        }

        private void CreateBaseConfigTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 20;
            int labelX = 20;
            int labelWidth = 150;  // Fixed label width
            int controlX = labelX + labelWidth + 10;  // 20 + 150 + 10 = 180
            int controlWidth = 350;  // Good width for text boxes
            int rowHeight = 35;  // Good spacing

            // Connection Settings Group
            var grpConnection = new GroupBox();
            grpConnection.Text = "Connection Settings";
            grpConnection.Location = new Point(10, y);
            grpConnection.Size = new Size(650, 300);  // Wider to accommodate controls
            tab.Controls.Add(grpConnection);

            int gy = 25;

            // Name
            var lblName = new Label { Text = "Configuration Name:", Location = new Point(labelX, gy), Size = new Size(labelWidth, 20), AutoSize = false };
            textBoxName = new TextBox { Location = new Point(controlX, gy), Size = new Size(controlWidth, 20) };
            textBoxName.TextChanged += OnUserChange;
            grpConnection.Controls.Add(lblName);
            grpConnection.Controls.Add(textBoxName);
            gy += rowHeight;

            // Server Address
            var lblServer = new Label { Text = "Server Address:", Location = new Point(labelX, gy), Size = new Size(labelWidth, 20), AutoSize = false };
            textBoxServerAddress = new TextBox { Location = new Point(controlX, gy), Size = new Size(controlWidth, 20) };
            textBoxServerAddress.TextChanged += OnUserChange;
            grpConnection.Controls.Add(lblServer);
            grpConnection.Controls.Add(textBoxServerAddress);
            gy += rowHeight;

            // Port + HTTPS
            var lblPort = new Label { Text = "Port:", Location = new Point(labelX, gy), Size = new Size(labelWidth, 20), AutoSize = false };
            numericUpDownPort = new NumericUpDown { Location = new Point(controlX, gy), Size = new Size(80, 20), Minimum = 1, Maximum = 65535, Value = 443 };
            numericUpDownPort.ValueChanged += OnUserChange;
            checkBoxUseHttps = new CheckBox { Text = "Use HTTPS", Location = new Point(controlX + 100, gy), Size = new Size(100, 20), Checked = true };
            checkBoxUseHttps.CheckedChanged += OnUserChange;
            grpConnection.Controls.Add(lblPort);
            grpConnection.Controls.Add(numericUpDownPort);
            grpConnection.Controls.Add(checkBoxUseHttps);
            gy += rowHeight;

            // Username
            var lblUser = new Label { Text = "Username:", Location = new Point(labelX, gy), Size = new Size(labelWidth, 20), AutoSize = false };
            textBoxUsername = new TextBox { Location = new Point(controlX, gy), Size = new Size(controlWidth, 20) };
            textBoxUsername.TextChanged += OnUserChange;
            grpConnection.Controls.Add(lblUser);
            grpConnection.Controls.Add(textBoxUsername);
            gy += rowHeight;

            // Password
            var lblPass = new Label { Text = "Password:", Location = new Point(labelX, gy), Size = new Size(labelWidth, 20), AutoSize = false };
            textBoxPassword = new TextBox { Location = new Point(controlX, gy), Size = new Size(controlWidth, 20), UseSystemPasswordChar = true };
            textBoxPassword.TextChanged += OnUserChange;
            grpConnection.Controls.Add(lblPass);
            grpConnection.Controls.Add(textBoxPassword);
            gy += rowHeight;

            // API Key
            var lblApi = new Label { Text = "API Key:", Location = new Point(labelX, gy), Size = new Size(labelWidth, 20), AutoSize = false };
            textBoxApiKey = new TextBox { Location = new Point(controlX, gy), Size = new Size(controlWidth, 20) };
            textBoxApiKey.TextChanged += OnUserChange;
            grpConnection.Controls.Add(lblApi);
            grpConnection.Controls.Add(textBoxApiKey);
            gy += rowHeight;

            // Test Connection Button
            buttonTestConnection = new Button { Text = "Test Connection", Location = new Point(controlX, gy), Size = new Size(150, 30) };
            buttonTestConnection.Click += ButtonTestConnection_Click;
            grpConnection.Controls.Add(buttonTestConnection);

            y += 310;

            // Status Group (NEW for Phase 2)
            var grpStatus = new GroupBox();
            grpStatus.Text = "Status & Health";
            grpStatus.Location = new Point(10, y);
            grpStatus.Size = new Size(650, 120);  // Match width
            tab.Controls.Add(grpStatus);

            gy = 25;

            // Plugin Instance ID
            var lblInstIdLabel = new Label { Text = "Instance ID:", Location = new Point(labelX, gy), Size = new Size(labelWidth, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), AutoSize = false };
            labelInstanceId = new Label { Text = "-", Location = new Point(controlX, gy), Size = new Size(430, 20), ForeColor = Color.DarkBlue };
            grpStatus.Controls.Add(lblInstIdLabel);
            grpStatus.Controls.Add(labelInstanceId);
            gy += rowHeight;

            // Health Status
            var lblHealthLabel = new Label { Text = "Health Status:", Location = new Point(labelX, gy), Size = new Size(labelWidth, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), AutoSize = false };
            labelHealthStatus = new Label { Text = "Unknown", Location = new Point(controlX, gy), Size = new Size(200, 20) };
            grpStatus.Controls.Add(lblHealthLabel);
            grpStatus.Controls.Add(labelHealthStatus);
            gy += rowHeight;

            // Last Health Check
            var lblLastCheckLabel = new Label { Text = "Last Checked:", Location = new Point(labelX, gy), Size = new Size(labelWidth, 20), AutoSize = false };
            labelLastHealthCheck = new Label { Text = "Never", Location = new Point(controlX, gy), Size = new Size(430, 20), ForeColor = Color.Gray };
            grpStatus.Controls.Add(lblLastCheckLabel);
            grpStatus.Controls.Add(labelLastHealthCheck);
        }

        private void CreateMapRegionsTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 20;
            int labelX = 20;
            int controlX = 170;
            int controlWidth = 280; // Reduced to fit two columns
            int rowHeight = 35;

            // Map Settings Group (Left Column)
            var grpMap = new GroupBox();
            grpMap.Text = "Map Settings";
            grpMap.Location = new Point(10, y);
            grpMap.Size = new Size(500, 330); // Narrower for two-column layout
            tab.Controls.Add(grpMap);

            // Map Preview Group (Right Column)
            var grpPreview = new GroupBox();
            grpPreview.Text = "Map Preview";
            grpPreview.Location = new Point(520, y); // Next to map settings
            grpPreview.Size = new Size(350, 330); // Match height
            tab.Controls.Add(grpPreview);

            var lblPreviewInfo = new Label { 
                Text = "Preview:", 
                Location = new Point(15, 25), 
                Size = new Size(320, 20) 
            };
            grpPreview.Controls.Add(lblPreviewInfo);

            webViewSitePreview = new Microsoft.Web.WebView2.WinForms.WebView2();
            webViewSitePreview.Location = new Point(15, 50);
            webViewSitePreview.Size = new Size(320, 265);
            
            // Initialize WebView2 when control is ready
            webViewSitePreview.HandleCreated += async (s, ev) =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("WebView2 HandleCreated event fired");
                    
                    // Set user data folder for WebView2
                    var userDataFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "CoreCommandMIP", "AdminWebView2");
                    
                    System.Diagnostics.Debug.WriteLine($"WebView2 user data folder: {userDataFolder}");
                    
                    // Ensure directory exists
                    System.IO.Directory.CreateDirectory(userDataFolder);
                    
                    // Initialize with user data folder
                    var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
                        userDataFolder: userDataFolder);
                    
                    await webViewSitePreview.EnsureCoreWebView2Async(env);
                    System.Diagnostics.Debug.WriteLine("WebView2 initialized successfully");
                    
                    // Load map preview if item is loaded
                    if (_item != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Item exists, loading map preview");
                        var settings = RemoteServerSettings.FromItem(_item);
                        UpdateSitePreview(settings);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Item is null, showing placeholder");
                        ShowMapPlaceholder();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WebView2 init error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    DiagnosticLogger.WriteException("WebView2 initialization", ex);
                }
            };
            
            grpPreview.Controls.Add(webViewSitePreview);

            // Map Settings Content (in left column)
            int gy = 25;

            // Map Provider
            var lblMapProvider = new Label { Text = "Map Provider:", Location = new Point(labelX, gy), Size = new Size(140, 20) };
            comboBoxMapProvider = new ComboBox { Location = new Point(controlX, gy), Size = new Size(controlWidth, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            comboBoxMapProvider.Items.AddRange(new object[] { "Leaflet (OpenStreetMap)", "Mapbox" });
            comboBoxMapProvider.SelectedIndex = 0;
            comboBoxMapProvider.SelectedIndexChanged += OnUserChange;
            grpMap.Controls.Add(lblMapProvider);
            grpMap.Controls.Add(comboBoxMapProvider);
            gy += rowHeight;

            // Mapbox Token
            var lblMapboxToken = new Label { Text = "Mapbox Token:", Location = new Point(labelX, gy), Size = new Size(140, 20) };
            textBoxMapboxToken = new TextBox { Location = new Point(controlX, gy), Size = new Size(controlWidth, 20) };
            textBoxMapboxToken.TextChanged += OnUserChange;
            linkLabelGetMapboxToken = new LinkLabel { Text = "Get token", Location = new Point(controlX + controlWidth + 10, gy), Size = new Size(80, 20) };
            linkLabelGetMapboxToken.LinkClicked += LinkLabelGetMapboxToken_LinkClicked;
            grpMap.Controls.Add(lblMapboxToken);
            grpMap.Controls.Add(textBoxMapboxToken);
            grpMap.Controls.Add(linkLabelGetMapboxToken);
            gy += rowHeight;

            // Enable Caching
            checkBoxEnableMapCaching = new CheckBox { Text = "Enable map tile caching", Location = new Point(controlX, gy), Size = new Size(220, 20), Checked = true };
            checkBoxEnableMapCaching.CheckedChanged += OnUserChange;
            grpMap.Controls.Add(checkBoxEnableMapCaching);
            gy += rowHeight;

            // Enable 3D Map
            checkBoxEnable3DMap = new CheckBox { Text = "Enable 3D Map (Mapbox only)", Location = new Point(controlX, gy), Size = new Size(220, 20), Checked = false };
            checkBoxEnable3DMap.CheckedChanged += OnUserChange;
            grpMap.Controls.Add(checkBoxEnable3DMap);
            gy += rowHeight;

            // 3D Features (side by side to save space)
            checkBoxEnable3DBuildings = new CheckBox { Text = "3D Buildings", Location = new Point(controlX + 20, gy), Size = new Size(120, 20), Checked = true };
            checkBoxEnable3DBuildings.CheckedChanged += OnUserChange;
            grpMap.Controls.Add(checkBoxEnable3DBuildings);
            
            checkBoxEnable3DTerrain = new CheckBox { Text = "3D Terrain", Location = new Point(controlX + 150, gy), Size = new Size(120, 20), Checked = true };
            checkBoxEnable3DTerrain.CheckedChanged += OnUserChange;
            grpMap.Controls.Add(checkBoxEnable3DTerrain);
            gy += rowHeight;

            // Latitude (Oregon Zoo default)
            var lblLat = new Label { Text = "Latitude:", Location = new Point(labelX, gy), Size = new Size(140, 20) };
            textBoxLatitude = new TextBox { Location = new Point(controlX, gy), Size = new Size(120, 20), Text = "45.5098" };
            textBoxLatitude.TextChanged += OnMapSettingChanged;
            grpMap.Controls.Add(lblLat);
            grpMap.Controls.Add(textBoxLatitude);
            gy += rowHeight;

            // Longitude (Oregon Zoo default)
            var lblLon = new Label { Text = "Longitude:", Location = new Point(labelX, gy), Size = new Size(140, 20) };
            textBoxLongitude = new TextBox { Location = new Point(controlX, gy), Size = new Size(120, 20), Text = "-122.7161" };
            textBoxLongitude.TextChanged += OnMapSettingChanged;
            grpMap.Controls.Add(lblLon);
            grpMap.Controls.Add(textBoxLongitude);
            gy += rowHeight;

            // Zoom (Oregon Zoo default)
            var lblZoom = new Label { Text = "Zoom:", Location = new Point(labelX, gy), Size = new Size(140, 20) };
            textBoxZoom = new TextBox { Location = new Point(controlX, gy), Size = new Size(120, 20), Text = "14" };
            textBoxZoom.TextChanged += OnMapSettingChanged;
            grpMap.Controls.Add(lblZoom);
            grpMap.Controls.Add(textBoxZoom);
            gy += rowHeight;

            // Polling Interval
            var lblPoll = new Label { Text = "Polling Interval (s):", Location = new Point(labelX, gy), Size = new Size(140, 20) };
            numericUpDownPollingInterval = new NumericUpDown { Location = new Point(controlX, gy), Size = new Size(90, 20), Minimum = 0.5m, Maximum = 60, DecimalPlaces = 1, Value = 1, Increment = 0.5m };
            numericUpDownPollingInterval.ValueChanged += OnUserChange;
            grpMap.Controls.Add(lblPoll);
            grpMap.Controls.Add(numericUpDownPollingInterval);

            y += 290;

            // Region Selection Group
            var grpRegions = new GroupBox();
            grpRegions.Text = "Region Selection";
            grpRegions.Location = new Point(10, y);
            grpRegions.Size = new Size(600, 250);
            tab.Controls.Add(grpRegions);

            var lblRegions = new Label { Text = "Select regions to display on map:", Location = new Point(15, 25), Size = new Size(400, 20) };
            grpRegions.Controls.Add(lblRegions);

            checkedListBoxRegions = new CheckedListBox();
            checkedListBoxRegions.Location = new Point(15, 50);
            checkedListBoxRegions.Size = new Size(570, 150);
            checkedListBoxRegions.CheckOnClick = true;
            checkedListBoxRegions.ItemCheck += CheckedListBoxRegions_ItemCheck;
            grpRegions.Controls.Add(checkedListBoxRegions);

            buttonRefreshRegions = new Button { Text = "Refresh Regions", Location = new Point(15, 210), Size = new Size(150, 30) };
            buttonRefreshRegions.Click += ButtonRefreshRegions_Click;
            grpRegions.Controls.Add(buttonRefreshRegions);

            var buttonOfflineMapData = new Button { Text = "Offline Map Setup", Location = new Point(175, 210), Size = new Size(150, 30) };
            buttonOfflineMapData.Click += ButtonOfflineMapData_Click;
            buttonOfflineMapData.ForeColor = Color.DarkBlue;
            grpRegions.Controls.Add(buttonOfflineMapData);
        }

        private void CreateAlarmWiringTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 20;

            // Event Types Group
            var grpEvents = new GroupBox();
            grpEvents.Text = "C2 Event Types (Reference Only)";
            grpEvents.Location = new Point(10, y);
            grpEvents.Size = new Size(360, 250);
            tab.Controls.Add(grpEvents);

            var lblEvents = new Label { 
                Text = "C2 can trigger these event types.\nAlarm creation below uses Alert and Alarm only.", 
                Location = new Point(15, 25), 
                Size = new Size(330, 35),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.DarkSlateGray
            };
            grpEvents.Controls.Add(lblEvents);

            listBoxEventTypes = new ListBox();
            listBoxEventTypes.Location = new Point(15, 65);
            listBoxEventTypes.Size = new Size(330, 170);
            listBoxEventTypes.SelectionMode = SelectionMode.None; // Read-only
            listBoxEventTypes.Items.AddRange(new object[] {
                "? C2.Alert (Medium) - Will create",
                "? C2.Alarm (High) - Will create",
                "  C2.AlarmCleared (Info)",
                "  C2.TrackEnterRegion (Info)",
                "  C2.TrackLost (Info)"
            });
            grpEvents.Controls.Add(listBoxEventTypes);

            // Alarm Definitions Group - EXPANDED
            var grpAlarms = new GroupBox();
            grpAlarms.Text = "Recommended Alarm Definitions";
            grpAlarms.Location = new Point(380, y);
            grpAlarms.Size = new Size(490, 300); // Increased size to fit all buttons
            tab.Controls.Add(grpAlarms);

            var lblAlarms = new Label {
                Text = "These events and alarms will be created:",
                Location = new Point(15, 30),
                Size = new Size(460, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            grpAlarms.Controls.Add(lblAlarms);

            dataGridViewAlarms = new DataGridView();
            dataGridViewAlarms.Location = new Point(15, 55);
            dataGridViewAlarms.Size = new Size(460, 90); // Taller to show 4 rows
            dataGridViewAlarms.AllowUserToAddRows = false;
            dataGridViewAlarms.ReadOnly = true;
            dataGridViewAlarms.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dataGridViewAlarms.RowHeadersVisible = false;
            dataGridViewAlarms.ScrollBars = ScrollBars.Vertical;
            dataGridViewAlarms.AllowUserToResizeColumns = true;
            
            dataGridViewAlarms.Columns.Add("Name", "Name");
            dataGridViewAlarms.Columns.Add("Type", "Type");
            dataGridViewAlarms.Columns.Add("Priority", "Priority");
            dataGridViewAlarms.Columns.Add("Status", "Status");
            
            // Set explicit column widths
            dataGridViewAlarms.Columns[0].Width = 200;  // Name
            dataGridViewAlarms.Columns[1].Width = 120;  // Type
            dataGridViewAlarms.Columns[2].Width = 60;   // Priority
            dataGridViewAlarms.Columns[3].Width = 80;   // Status
            
            grpAlarms.Controls.Add(dataGridViewAlarms);

            // Status label
            labelWiringStatus = new Label {
                Text = "Enter a site name to begin",
                Location = new Point(15, 155),
                Size = new Size(460, 35), // Taller for text wrapping
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
            grpAlarms.Controls.Add(labelWiringStatus);

            // Buttons in a clean row
            buttonCreateEvents = new Button { 
                Text = "Create Events + Alarms", 
                Location = new Point(15, 200), 
                Size = new Size(155, 32),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            buttonCreateEvents.Click += ButtonCreateEvents_Click;
            buttonCreateEvents.Enabled = false; // Disabled until site name entered
            grpAlarms.Controls.Add(buttonCreateEvents);

            // NOTE: Old "Step 2: Create Alarms" button removed - functionality combined into single button above

            var buttonRefreshStatus = new Button {
                Text = "Refresh Status",
                Location = new Point(325, 200),
                Size = new Size(145, 32),
                Font = new Font("Segoe UI", 8.5F)
            };
            buttonRefreshStatus.Click += (s, ev) => UpdateWiringStatus();
            grpAlarms.Controls.Add(buttonRefreshStatus);

            // View Logs button - opens Milestone log viewer
            var buttonShowLog = new Button {
                Text = "View Logs",
                Location = new Point(15, 242),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.DarkBlue
            };
            buttonShowLog.Click += (s, ev) => {
                try {
                    // Open Milestone Management Client log viewer
                    var logViewerPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "Milestone", "MIPSDK", "Bin", "LogViewer.exe");
                    
                    if (System.IO.File.Exists(logViewerPath))
                    {
                        System.Diagnostics.Process.Start(logViewerPath);
                    }
                    else
                    {
                        // Fallback: Open Milestone logs folder
                        var logsFolder = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                            "Milestone", "XProtect Management Server", "Logs");
                        
                        if (System.IO.Directory.Exists(logsFolder))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", logsFolder);
                        }
                        else
                        {
                            MessageBox.Show(
                                "Milestone logs are written to the XProtect Management Server log.\n\n" +
                                "To view logs:\n" +
                                "1. Open Milestone Management Client\n" +
                                "2. Go to Tools > Options > Logging\n" +
                                "3. Or check: C:\\ProgramData\\Milestone\\XProtect Management Server\\Logs",
                                "View Logs",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                } catch (Exception ex) {
                    MessageBox.Show($"Error opening logs: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            grpAlarms.Controls.Add(buttonShowLog);

            // Help text below buttons
            var lblHelp = new Label {
                Text = "Logs are written to Milestone XProtect Management Server log.\nClick 'View Logs' to open log folder or use Management Client log viewer.",
                Location = new Point(145, 242),
                Size = new Size(320, 40),
                ForeColor = Color.DarkSlateGray,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic)
            };
            grpAlarms.Controls.Add(lblHelp);

            y += 310;

            // Camera Association Group
            var grpCameras = new GroupBox();
            grpCameras.Text = "Associated Cameras";
            grpCameras.Location = new Point(10, y);
            grpCameras.Size = new Size(770, 320);  // Increased height
            tab.Controls.Add(grpCameras);

            var lblCameras = new Label { 
                Text = "Select cameras to associate with this C2 instance:", 
                Location = new Point(15, 30), 
                Size = new Size(700, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            grpCameras.Controls.Add(lblCameras);

            checkedListBoxCameras = new CheckedListBox();
            checkedListBoxCameras.Location = new Point(15, 55);
            checkedListBoxCameras.Size = new Size(740, 220);  // Increased height
            checkedListBoxCameras.CheckOnClick = true;
            checkedListBoxCameras.ItemCheck += CheckedListBoxCameras_ItemCheck;
            grpCameras.Controls.Add(checkedListBoxCameras);

            buttonRefreshCameras = new Button { 
                Text = "Refresh Camera List", 
                Location = new Point(15, 285), 
                Size = new Size(150, 30) 
            };
            buttonRefreshCameras.Click += ButtonRefreshCameras_Click;
            grpCameras.Controls.Add(buttonRefreshCameras);

            labelCameraCount = new Label { 
                Text = "0 cameras selected", 
                Location = new Point(175, 290), 
                Size = new Size(300, 20), 
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
            grpCameras.Controls.Add(labelCameraCount);
        }

        // OLD METHOD REMOVED: ButtonCreateAlarms_Click()
        // OLD METHOD REMOVED: CreateAlarmDefinition() with hardcoded GUID
        // 
        // Reason: Functionality replaced by C2AlarmWiringVerified.EnsureUdeAndAlarmDefinitionVerified()
        // which properly probes existing alarms for correct EventTypeGroup/EventType values.

        private void UpdateWiringStatus()
        {
            DiagnosticLogger.WriteSection("UPDATE WIRING STATUS");
            
            try
            {
                var siteName = textBoxName.Text?.Trim();
                DiagnosticLogger.WriteLine($"Site name: '{siteName}'");
                
                if (string.IsNullOrWhiteSpace(siteName))
                {
                    labelWiringStatus.Text = "Enter a site name to begin";
                    labelWiringStatus.ForeColor = Color.Gray;
                    buttonCreateEvents.Enabled = false;
                    
                    // Clear DataGrid
                    dataGridViewAlarms.Rows.Clear();
                    dataGridViewAlarms.Rows.Add("(Enter site name)", "", "", "N/A");
                    DiagnosticLogger.WriteLine("No site name - returning");
                    return;
                }

                buttonCreateEvents.Enabled = true;

                var alertEventName = $"C2_Alert_{siteName}";
                var alarmEventName = $"C2_Alarm_{siteName}";
                var alertAlarmName = $"C2_Alert_{siteName}";
                var alarmAlarmName = $"C2_Alarm_{siteName}";
                
                DiagnosticLogger.WriteLine($"Checking for events/alarms via ManagementServer:");
                DiagnosticLogger.WriteLine($"  Event: '{alertEventName}'");
                DiagnosticLogger.WriteLine($"  Event: '{alarmEventName}'");
                DiagnosticLogger.WriteLine($"  Alarm: '{alertAlarmName}'");
                DiagnosticLogger.WriteLine($"  Alarm: '{alarmAlarmName}'");
                
                var serverId = VideoOS.Platform.EnvironmentManager.Instance.MasterSite.ServerId;
                var ms = new VideoOS.Platform.ConfigurationItems.ManagementServer(serverId);
                
                // Use ManagementServer approach (WORKING!)
                UserDefinedEvent alertEvent = null;
                UserDefinedEvent alarmEvent = null;
                AlarmDefinition alertAlarm = null;
                AlarmDefinition alarmAlarm = null;
                
                try
                {
                    var events = ms.UserDefinedEventFolder.UserDefinedEvents.ToArray();
                    DiagnosticLogger.WriteLine($"Total events found: {events.Length}");
                    
                    alertEvent = events.FirstOrDefault(e => string.Equals(e.Name, alertEventName, StringComparison.OrdinalIgnoreCase));
                    alarmEvent = events.FirstOrDefault(e => string.Equals(e.Name, alarmEventName, StringComparison.OrdinalIgnoreCase));
                    
                    DiagnosticLogger.WriteLine($"Alert event found: {alertEvent != null}");
                    DiagnosticLogger.WriteLine($"Alarm event found: {alarmEvent != null}");
                }
                catch (Exception ex)
                {
                    DiagnosticLogger.WriteLine($"Error loading events: {ex.Message}");
                }
                
                try
                {
                    var alarms = ms.AlarmDefinitionFolder.AlarmDefinitions.ToArray();
                    DiagnosticLogger.WriteLine($"Total alarms found: {alarms.Length}");
                    
                    alertAlarm = alarms.FirstOrDefault(a => string.Equals(a.Name, alertAlarmName, StringComparison.OrdinalIgnoreCase));
                    alarmAlarm = alarms.FirstOrDefault(a => string.Equals(a.Name, alarmAlarmName, StringComparison.OrdinalIgnoreCase));
                    
                    DiagnosticLogger.WriteLine($"Alert alarm found: {alertAlarm != null}");
                    DiagnosticLogger.WriteLine($"Alarm alarm found: {alarmAlarm != null}");
                }
                catch (Exception ex)
                {
                    DiagnosticLogger.WriteLine($"Error loading alarms: {ex.Message}");
                }

                bool eventsExist = alertEvent != null && alarmEvent != null;
                bool alarmsExist = alertAlarm != null && alarmAlarm != null;

                // Update status
                DiagnosticLogger.WriteSection("FINAL STATUS");
                DiagnosticLogger.WriteLine($"Events exist: {eventsExist}");
                DiagnosticLogger.WriteLine($"Alarms exist: {alarmsExist}");
                
                if (eventsExist && alarmsExist)
                {
                    labelWiringStatus.Text = $"? All events and alarms exist for site '{siteName}'";
                    labelWiringStatus.ForeColor = Color.DarkGreen;
                    buttonCreateEvents.Enabled = true;
                    DiagnosticLogger.WriteLine("? ALL COMPLETE - button ENABLED");
                }
                else if (eventsExist)
                {
                    labelWiringStatus.Text = $"? Events exist. Click button again to recreate alarms if needed for site '{siteName}'";
                    labelWiringStatus.ForeColor = Color.DarkBlue;
                    buttonCreateEvents.Enabled = true;
                    DiagnosticLogger.WriteLine("? EVENTS EXIST - button ENABLED");
                }
                else
                {
                    labelWiringStatus.Text = $"No events created yet. Click 'Create Events + Alarms' to begin.";
                    labelWiringStatus.ForeColor = Color.OrangeRed;
                    buttonCreateEvents.Enabled = true;
                    DiagnosticLogger.WriteLine("? NO EVENTS - button ENABLED");
                }

                DiagnosticLogger.WriteLine($"Button state: CreateEventsAndAlarms={buttonCreateEvents.Enabled}");

                // Update DataGrid with exact names being checked
                dataGridViewAlarms.Rows.Clear();
                dataGridViewAlarms.Rows.Add(alertEventName, "User-Defined Event", "Medium", 
                    alertEvent != null ? "? Created" : "Not Created");
                dataGridViewAlarms.Rows.Add(alertAlarmName, "Alarm Definition", "Medium", 
                    alertAlarm != null ? "? Created" : "Not Created");
                dataGridViewAlarms.Rows.Add(alarmEventName, "User-Defined Event", "High", 
                    alarmEvent != null ? "? Created" : "Not Created");
                dataGridViewAlarms.Rows.Add(alarmAlarmName, "Alarm Definition", "High", 
                    alarmAlarm != null ? "? Created" : "Not Created");
                
                DiagnosticLogger.WriteLine("DataGrid updated with 4 rows");
                //DiagnosticLogger.WriteLine($"Log file: {DiagnosticLogger.GetLogFilePath()}");
                DiagnosticLogger.WriteLine("UpdateWiringStatus() complete");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.WriteException("UpdateWiringStatus", ex);
                labelWiringStatus.Text = "Error checking status";
                labelWiringStatus.ForeColor = Color.Red;
            }
        }

        private void OnUserChange(object sender, EventArgs e)
        {
            // DON'T auto-save here - just notify that configuration changed
            // The ItemManager will call UpdateItem() when user clicks Save
            ConfigurationChangedByUser?.Invoke(this, e);
        }

        private void OnMapSettingChanged(object sender, EventArgs e)
        {
            // Update map preview when map settings change
            OnUserChange(sender, e);
            
            if (_item != null)
            {
                var settings = CollectCurrentSettings();
                UpdateSitePreview(settings);
            }
        }

        private RemoteServerSettings CollectCurrentSettings()
        {
            var settings = new RemoteServerSettings
            {
                Host = textBoxServerAddress.Text?.Trim() ?? string.Empty,
                Port = (int)numericUpDownPort.Value,
                UseSsl = checkBoxUseHttps.Checked,
                ApiKey = textBoxApiKey.Text?.Trim() ?? string.Empty,
                DefaultUsername = textBoxUsername.Text?.Trim() ?? string.Empty,
                DefaultPassword = textBoxPassword.Text ?? string.Empty,
                DefaultLatitude = ParseDoubleOrDefault(textBoxLatitude.Text, 45.5098),  // Oregon Zoo
                DefaultLongitude = ParseDoubleOrDefault(textBoxLongitude.Text, -122.7161),  // Oregon Zoo
                DefaultZoomLevel = ParseDoubleOrDefault(textBoxZoom.Text, 14),
                SiteRadiusMeters = 0, // Not editable in this UI
                PollingIntervalSeconds = (double)numericUpDownPollingInterval.Value,
                TailLength = 200,
                MapProvider = (MapProvider)(comboBoxMapProvider.SelectedIndex >= 0 ? comboBoxMapProvider.SelectedIndex : 0),
                MapboxAccessToken = textBoxMapboxToken.Text?.Trim() ?? string.Empty,
                EnableMapCaching = checkBoxEnableMapCaching.Checked,
                SelectedRegionIds = GetSelectedRegionIds(),
                AssociatedCameraIds = GetSelectedCameraIds()
            };
            return settings;
        }

        private void ButtonTestConnection_Click(object sender, EventArgs e)
        {
            // TODO: Implement connection test
            MessageBox.Show("Connection test - To be implemented", "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void ButtonCreateEvents_Click(object sender, EventArgs e)
        {
            var originalCursor = this.Cursor;
            
            try
            {
                this.Cursor = Cursors.WaitCursor;
                buttonCreateEvents.Enabled = false;
                buttonCreateEvents.Text = "Creating...";
                Application.DoEvents();

                var siteName = textBoxName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(siteName))
                {
                    MessageBox.Show("Please enter a site name first.", "Site Name Required", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"This will create User-Defined Events & Alarms for site: {siteName}\n\n" +
                    $"Will create (via MIP SDK):\n" +
                    $"  • C2_Alert_{siteName} (Event + Alarm)\n" +
                    $"  • C2_Alarm_{siteName} (Event + Alarm)\n\n" +
                    $"Associated cameras: {checkedListBoxCameras.CheckedItems.Count}\n\n" +
                    "Do you want to proceed?",
                    "Create Events & Alarms",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                DiagnosticLogger.WriteSection("CREATE EVENTS + ALARMS (MIP SDK)");
                DiagnosticLogger.WriteLine($"Site: {siteName}");
                DiagnosticLogger.WriteLine($"Using VideoOS.Platform ConfigurationItems API");

                int created = 0;
                int existed = 0;
                var errors = new System.Collections.Generic.List<string>();

                // Get camera paths
                var cameraPaths = new System.Collections.Generic.List<string>();
                if (checkedListBoxCameras.CheckedItems.Count > 0)
                {
                    foreach (CameraItem cam in checkedListBoxCameras.CheckedItems)
                    {
                        try
                        {
                            var cameraItem = Configuration.Instance.GetItem(cam.Id, Kind.Camera);
                            if (cameraItem != null)
                            {
                                var cameraConfig = new Camera(cameraItem.FQID);
                                cameraPaths.Add(cameraConfig.Path);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error getting path for {cam.Name}: {ex.Message}");
                        }
                    }
                }

                // Create C2_Alert (Event + Alarm) via MIP SDK
                try
                {
                    DiagnosticLogger.WriteLine($"Creating C2_Alert_{siteName} via MIP SDK...");
                    
                    var alertResult = await System.Threading.Tasks.Task.Run(() =>
                    {
                        return C2AlarmWiringVerified.EnsureUdeAndAlarmDefinitionVerified(
                            udeName: $"C2_Alert_{siteName}",
                            alarmDefinitionName: $"C2_Alert_{siteName}",
                            alarmPriority: "Medium",
                            relatedCameraPaths: cameraPaths.ToArray());
                    });
                    
                    if (alertResult != null)
                    {
                        created++;
                        DiagnosticLogger.WriteLine($"✓ Created C2_Alert_{siteName}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"C2.Alert: {ex.Message}");
                    DiagnosticLogger.WriteException("C2.Alert SDK creation", ex);
                }

                // Create C2_Alarm (Event + Alarm) via MIP SDK
                try
                {
                    DiagnosticLogger.WriteLine($"Creating C2_Alarm_{siteName} via MIP SDK...");
                    
                    var alarmResult = await System.Threading.Tasks.Task.Run(() =>
                    {
                        return C2AlarmWiringVerified.EnsureUdeAndAlarmDefinitionVerified(
                            udeName: $"C2_Alarm_{siteName}",
                            alarmDefinitionName: $"C2_Alarm_{siteName}",
                            alarmPriority: "High",
                            relatedCameraPaths: cameraPaths.ToArray());
                    });
                    
                    if (alarmResult != null)
                    {
                        created++;
                        DiagnosticLogger.WriteLine($"✓ Created C2_Alarm_{siteName}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"C2.Alarm: {ex.Message}");
                    DiagnosticLogger.WriteException("C2.Alarm SDK creation", ex);
                }

                // Wait for Management Server to process
                await System.Threading.Tasks.Task.Delay(1000);
                
                // Update status
                UpdateWiringStatus();

                // Show results
                var totalProcessed = created + existed;
                var message = $"Event & Alarm Creation Complete (MIP SDK)\n\n" +
                             $"Site: {siteName}\n" +
                             $"Created: {created} pairs (Event + Alarm)\n" +
                             $"Already existed: {existed} pairs\n" +
                             $"Total: {totalProcessed}\n\n";

                if (errors.Count > 0)
                {
                    message += "⚠️ Errors:\n" + string.Join("\n", errors) + "\n\n";
                }

                if (totalProcessed > 0)
                {
                    message += "✓ Events and Alarms are ready!\n\n" +
                              "The alarm definitions are wired to trigger on their events.\n" +
                              "Created via Milestone MIP SDK (VideoOS.Platform ConfigurationItems).";
                }

                MessageBox.Show(message, "Creation Complete", 
                    MessageBoxButtons.OK, 
                    errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagnosticLogger.WriteException("ButtonCreateEvents_Click (SDK)", ex);
                MessageBox.Show($"Error: {ex.Message}\n\nSee diagnostic log for details.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buttonCreateEvents.Text = "Create Events + Alarms";
                buttonCreateEvents.Enabled = true;
                this.Cursor = originalCursor;
            }
        }
        // OLD METHOD REMOVED: EnsureUdeAndAlarmDefinition()
        //
        // Reason: Never called. Superseded by C2AlarmWiringVerified.EnsureUdeAndAlarmDefinitionVerified()
        // in Admin/C2AlarmWiringVerified.cs which has proper error handling, polling, and value probing.

        private void WaitForTaskOrThrow(ServerTask task, string errorPrefix)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            // Wait for task completion (with timeout)
            int maxWaitMs = 30000; // 30 seconds
            int elapsedMs = 0;
            int sleepMs = 50;

            while ((task.State == StateEnum.InProgress || task.State == StateEnum.Idle) && elapsedMs < maxWaitMs)
            {
                System.Threading.Thread.Sleep(sleepMs);
                elapsedMs += sleepMs;
            }

            if (elapsedMs >= maxWaitMs)
            {
                throw new TimeoutException($"{errorPrefix}. Task timed out after {maxWaitMs}ms");
            }

            if (task.State != StateEnum.Success && task.State != StateEnum.Completed)
            {
                var msg = string.IsNullOrWhiteSpace(task.ErrorText) ? task.State.ToString() : task.ErrorText;
                throw new InvalidOperationException($"{errorPrefix}. Task state={task.State}. Error={msg}");
            }
        }

        private void ButtonRefreshCameras_Click(object sender, EventArgs e)
        {
            LoadCameraList();
        }

        private void CheckedListBoxCameras_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Update count after check state changes
            this.BeginInvoke(new Action(() => UpdateCameraCount()));
        }

        private void CheckedListBoxRegions_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Trigger save on region selection change
            this.BeginInvoke(new Action(() => OnUserChange(sender, e)));
        }

        private void ButtonRefreshRegions_Click(object sender, EventArgs e)
        {
            LoadRegionsAsync();
        }

        private void ButtonCheckServer_Click(object sender, EventArgs e)
        {
            DiagnosticLogger.WriteSection("SERVER CONNECTION CHECK");
            
            try
            {
                var config = VideoOS.Platform.Configuration.Instance;
                var serverFQID = config?.ServerFQID;
                
                var info = new System.Text.StringBuilder();
                info.AppendLine("Management Server Connection Info:");
                info.AppendLine();
                info.AppendLine($"Machine: {Environment.MachineName}");
                info.AppendLine($"User: {Environment.UserName}");
                info.AppendLine();
                
                if (config != null && serverFQID != null)
                {
                    info.AppendLine($"Server Type: {serverFQID.ServerId?.ServerType ?? "Unknown"}");
                    
                    var serverId = serverFQID.ServerId?.Id;
                    info.AppendLine($"Server ID: {(serverId.HasValue ? serverId.Value.ToString() : "Unknown")}");
                    
                    info.AppendLine($"Connected: Yes");
                    
                    DiagnosticLogger.WriteLine($"Server Type: {serverFQID.ServerId?.ServerType}");
                    DiagnosticLogger.WriteLine($"Server ID: {(serverId.HasValue ? serverId.Value.ToString() : "Unknown")}");
                }
                else
                {
                    info.AppendLine("ERROR: Not connected to Management Server!");
                    DiagnosticLogger.WriteLine("ERROR: Configuration.Instance is null!");
                }
                
                info.AppendLine();
                info.AppendLine("Checking for User-Defined Events:");
                
                var udeFolder = new UserDefinedEventFolder();
                udeFolder.ClearChildrenCache();
                udeFolder.FillChildren(new[] { nameof(UserDefinedEvent) });
                
                int totalUDEs = udeFolder.UserDefinedEvents?.Count ?? 0;
                info.AppendLine($"Total UDEs found: {totalUDEs}");
                
                DiagnosticLogger.WriteLine($"Total UDEs found: {totalUDEs}");
                
                if (totalUDEs > 0)
                {
                    info.AppendLine();
                    info.AppendLine("User-Defined Events:");
                    foreach (var ude in udeFolder.UserDefinedEvents)
                    {
                        info.AppendLine($"  - {ude.Name}");
                        DiagnosticLogger.WriteLine($"  UDE: {ude.Name}");
                    }
                    
                    // Check for NRK events specifically
                    var nrkEvents = udeFolder.UserDefinedEvents
                        .Where(u => u.Name.Contains("NRK"))
                        .ToList();
                    
                    if (nrkEvents.Any())
                    {
                        info.AppendLine();
                        info.AppendLine($"NRK-related events found: {nrkEvents.Count}");
                        foreach (var nrkEvent in nrkEvents)
                        {
                            info.AppendLine($"  ? {nrkEvent.Name}");
                        }
                    }
                    else
                    {
                        info.AppendLine();
                        info.AppendLine("?? NO NRK events found!");
                    }
                }
                else
                {
                    info.AppendLine();
                    info.AppendLine("?? NO User-Defined Events found on this server!");
                    info.AppendLine();
                    info.AppendLine("This means either:");
                    info.AppendLine("1. No events have been created yet");
                    info.AppendLine("2. You're looking at a DIFFERENT Management Server");
                    info.AppendLine();
                    info.AppendLine("Check: Are you connected to the right server?");
                }
                
                info.AppendLine();
                //info.AppendLine($"Log file: {DiagnosticLogger.GetLogFilePath()}");
                
                MessageBox.Show(info.ToString(), "Server Connection Check", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagnosticLogger.WriteException("ButtonCheckServer_Click", ex);
                MessageBox.Show($"Error checking server:\n\n{ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonOfflineMapData_Click(object sender, EventArgs e)
        {
            var form = new Form();
            form.Text = "Offline Map Data for Remote Smart Clients";
            form.Size = new Size(800, 600);
            form.StartPosition = FormStartPosition.CenterParent;
            form.FormBorderStyle = FormBorderStyle.Sizable;

            var textBox = new TextBox();
            textBox.Multiline = true;
            textBox.ReadOnly = true;
            textBox.ScrollBars = ScrollBars.Vertical;
            textBox.Dock = DockStyle.Fill;
            textBox.Font = new Font("Consolas", 9F);
            textBox.Text = @"OFFLINE MAP DATA DEPLOYMENT FOR REMOTE SMART CLIENTS

==============================================================
OVERVIEW
==============================================================
Smart Clients running on remote machines need local map tile 
caches for optimal performance and offline capability.

Since Milestone XProtect does NOT automatically push WebView2 
cache data to Smart Clients, you must deploy map data manually 
to each remote machine.

==============================================================
DEPLOYMENT OPTIONS
==============================================================

OPTION 1: MANUAL DEPLOYMENT (Small Scale)
----------------------------------------------------------
For small deployments (1-5 Smart Client machines):

1. Run Smart Client on ONE machine with internet access
2. Let it cache map tiles naturally by:
   - Opening the CoreCommandMIP view
   - Panning and zooming around the site area
   - Enabling 3D maps and letting it load
   - Waiting 5-10 minutes for full cache
   
3. Locate the cache folder:
   %LocalAppData%\CoreCommandMIP\WebView2Cache\
   
4. Zip the entire WebView2Cache folder
   
5. Copy the zip to remote Smart Client machines
   
6. Extract to same location on remote machines:
   C:\Users\[Username]\AppData\Local\CoreCommandMIP\WebView2Cache\


OPTION 2: PRE-PACKAGE TILES (Medium Scale)
----------------------------------------------------------
For medium deployments (5-50 machines):

1. Download TileMill or MapTiler:
   https://www.maptiler.com/desktop/

2. Create an offline tile package:
   - Set center point (your site lat/lon)
   - Set zoom levels (8-18 recommended)
   - Export as MBTiles format
   
3. Convert MBTiles to WebView2 cache format:
   - Use the provided PowerShell script
   - Script location: [To Be Created]
   
4. Deploy via:
   - Group Policy
   - SCCM/Intune
   - Manual copy


OPTION 3: NETWORK SHARE (Any Scale)
----------------------------------------------------------
For any size deployment:

1. Create a network share accessible by all Smart Clients:
   \\fileserver\xprotect\map-cache\
   
2. Populate with cached tiles (Option 1 or 2)
   
3. Modify plugin to check network share first:
   - Edit CoreCommandMIPViewItemWpfUserControl.xaml.cs
   - Update PrepareWebViewHost() method
   - Add network path fallback
   
4. Smart Clients will use network cache
   
PROS: Centralized updates
CONS: Requires network access, slower than local


OPTION 4: MAPBOX OFFLINE (Mapbox Only)
----------------------------------------------------------
If using Mapbox with commercial license:

1. Get Mapbox Maps SDK for offline:
   https://docs.mapbox.com/mapbox-gl-js/guides/install/
   
2. Download offline region:
   - Use Mapbox Tiling Service
   - Specify bounding box around site
   - Download as .mbtiles
   
3. Host tiles locally:
   - Run tile server (TileServer-GL)
   - Update Mapbox URL in plugin config
   - Point to http://localhost:8080/


==============================================================
RECOMMENDED APPROACH
==============================================================

FOR ONLINE-ONLY DEPLOYMENTS:
? Do nothing - let Smart Clients cache naturally
? Ensure good internet bandwidth
? Accept slower initial load

FOR OFFLINE/REMOTE DEPLOYMENTS:
1. Use Option 1 (Manual) for initial setup
2. Schedule quarterly updates via Option 2 or 3
3. Document the deployed cache version
4. Keep source tiles for re-deployment

FOR HIGH-SECURITY/AIR-GAPPED:
1. Use Option 2 (Pre-package) exclusively
2. Deploy via physical media or secure network
3. Plan for map data aging (tiles go stale)
4. Re-deploy updated tiles annually


==============================================================
CACHE SIZE ESTIMATES
==============================================================

Leaflet (OpenStreetMap):
- Zoom 8-12:   ~50 MB
- Zoom 8-16:   ~500 MB
- Zoom 8-18:   ~2 GB

Mapbox 2D:
- Zoom 8-12:   ~100 MB
- Zoom 8-16:   ~800 MB
- Zoom 8-18:   ~3 GB

Mapbox 3D (Buildings + Terrain):
- Zoom 8-12:   ~200 MB
- Zoom 8-16:   ~2 GB
- Zoom 8-18:   ~8 GB

Plan storage accordingly!


==============================================================
AUTOMATION SCRIPT (To Be Created)
==============================================================

A PowerShell script will be provided to automate:
1. Tile download for specific region
2. Conversion to WebView2 cache format
3. Packaging for deployment
4. Deployment via SCCM/Intune

Script name: Deploy-MapCache.ps1
Status: TODO - Request if needed


==============================================================
MILESTONE LIMITATIONS
==============================================================

? Milestone does NOT provide:
- Automatic plugin data distribution
- Central cache management
- WebView2 cache synchronization

? Milestone DOES provide:
- Plugin DLL distribution to Smart Clients
- Centralized configuration storage
- Event Server plugin hosting

You must handle map cache deployment separately!


==============================================================
QUESTIONS?
==============================================================

Contact your XProtect integrator for:
- Deployment automation
- Network share setup
- Offline tile generation
- Cache update procedures

For this plugin specifically:
- Check documentation folder
- Review Deploy-MapCache.ps1 script
- Test on pilot machine first
";

            var buttonClose = new Button();
            buttonClose.Text = "Close";
            buttonClose.DialogResult = DialogResult.OK;
            buttonClose.Dock = DockStyle.Bottom;
            buttonClose.Height = 35;
            
            form.Controls.Add(textBox);
            form.Controls.Add(buttonClose);
            form.AcceptButton = buttonClose;
            
            form.ShowDialog(this);
        }

        private void LinkLabelGetMapboxToken_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://account.mapbox.com/access-tokens/");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCameraList()
        {
            checkedListBoxCameras.Items.Clear();
            
            try
            {
                // Get all cameras from configuration
                var cameras = Configuration.Instance.GetItems(ItemHierarchy.SystemDefined);
                foreach (var item in cameras)
                {
                    if (item.FQID.Kind == Kind.Camera)
                    {
                        var cameraItem = new CameraItem { Name = item.Name, Id = item.FQID.ObjectId };
                        checkedListBoxCameras.Items.Add(cameraItem);
                    }
                }

                UpdateCameraCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load cameras: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateCameraCount()
        {
            int count = checkedListBoxCameras.CheckedItems.Count;
            labelCameraCount.Text = $"{count} camera{(count != 1 ? "s" : "")} selected";
        }

        private async void LoadRegionsAsync()
        {
            checkedListBoxRegions.Items.Clear();

            if (_item == null)
            {
                System.Diagnostics.Debug.WriteLine("LoadRegionsAsync: Configuration not loaded.");
                return;
            }

            var settings = RemoteServerSettings.FromItem(_item);

            if (!settings.IsConfigured())
            {
                System.Diagnostics.Debug.WriteLine("LoadRegionsAsync: Server not configured.");
                return;
            }

            try
            {
                buttonRefreshRegions.Enabled = false;
                buttonRefreshRegions.Text = "Loading...";

                var provider = new Client.RemoteServerDataProvider();
                var baseUrl = settings.GetBaseUrl();

                System.Diagnostics.Debug.WriteLine($"Loading regions from: {baseUrl}");

                var regionList = await provider.FetchRegionListAsync(baseUrl, settings.DefaultUsername, settings.DefaultPassword, System.Threading.CancellationToken.None);

                System.Diagnostics.Debug.WriteLine($"Received {regionList?.Count ?? 0} regions");

                if (regionList == null || regionList.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("LoadRegionsAsync: No regions found on server.");
                    return;
                }

                // Parse saved selection
                var selectedIds = ParseSelectedRegionIds(settings.SelectedRegionIds);

                foreach (var region in regionList)
                {
                    // Check if this region is selected (by GUID or numeric ID)
                    var isChecked = selectedIds.Contains(region.GuidId ?? string.Empty) ||
                                   selectedIds.Contains(region.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));

                    checkedListBoxRegions.Items.Add(region, isChecked);
                    System.Diagnostics.Debug.WriteLine($"Added region: {region}, Checked: {isChecked}");
                }

                System.Diagnostics.Debug.WriteLine($"LoadRegionsAsync: Loaded {regionList.Count} region(s) successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadRegionsAsync: Failed to load regions - {ex.Message}");
            }
            finally
            {
                buttonRefreshRegions.Enabled = true;
                buttonRefreshRegions.Text = "Refresh Regions";
            }
        }

        private System.Collections.Generic.HashSet<string> ParseSelectedRegionIds(string csv)
        {
            var set = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(csv)) return set;

            var parts = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    set.Add(trimmed);
                }
            }
            return set;
        }

        private string GetSelectedRegionIds()
        {
            var selectedIds = new System.Collections.Generic.List<string>();
            foreach (var item in checkedListBoxRegions.CheckedItems)
            {
                if (item is Client.RegionListItem regionItem)
                {
                    // Use GUID if available, otherwise use numeric ID
                    var identifier = !string.IsNullOrEmpty(regionItem.GuidId)
                        ? regionItem.GuidId
                        : regionItem.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    selectedIds.Add(identifier);
                }
            }
            return string.Join(",", selectedIds);
        }

        public void Init(Item item)
        {
            _item = item;
            LoadFromItem();
            LoadCameraList();
            
            // If this is a new item with default name, suggest unique name
            if (_item != null && _item.Name == "Enter a name")
            {
                // Generate unique name based on Instance ID
                var settings = RemoteServerSettings.FromItem(_item);
                var shortId = settings.PluginInstanceId.ToString().Substring(0, 8);
                textBoxName.Text = $"C2 Site {shortId}";
            }
        }

        private void LoadFromItem()
        {
            if (_item == null) return;

            var settings = RemoteServerSettings.FromItem(_item);

            // Tab 1: Base Configuration
            textBoxName.Text = _item.Name;
            textBoxServerAddress.Text = settings.Host;
            numericUpDownPort.Value = settings.Port;
            checkBoxUseHttps.Checked = settings.UseSsl;
            textBoxUsername.Text = settings.DefaultUsername;
            textBoxPassword.Text = settings.DefaultPassword;
            textBoxApiKey.Text = settings.ApiKey;

            // Display status
            labelInstanceId.Text = settings.PluginInstanceId.ToString();
            UpdateHealthStatus(settings.HealthStatus, settings.LastHealthCheck);

            // Tab 2: Map & Regions
            comboBoxMapProvider.SelectedIndex = (int)settings.MapProvider;
            textBoxMapboxToken.Text = settings.MapboxAccessToken ?? string.Empty;
            checkBoxEnableMapCaching.Checked = settings.EnableMapCaching;
            checkBoxEnable3DMap.Checked = settings.Enable3DMap;
            checkBoxEnable3DBuildings.Checked = settings.Enable3DBuildings;
            checkBoxEnable3DTerrain.Checked = settings.Enable3DTerrain;
            textBoxLatitude.Text = settings.DefaultLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            textBoxLongitude.Text = settings.DefaultLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            textBoxZoom.Text = settings.DefaultZoomLevel.ToString(System.Globalization.CultureInfo.InvariantCulture);
            numericUpDownPollingInterval.Value = (decimal)Math.Max(0.5, Math.Min(60, settings.PollingIntervalSeconds));

            // Load regions asynchronously
            LoadRegionsAsync();

            // Update map preview
            UpdateSitePreview(settings);

            // Tab 3: Camera selection
            LoadSelectedCameras(settings.AssociatedCameraIds);
            
            // Tab 3: Update wiring status
            UpdateWiringStatus();
        }

        private void SaveToItem()
        {
            if (_item == null) return;

            var settings = RemoteServerSettings.FromItem(_item);

            // Tab 1: Base Configuration
            _item.Name = textBoxName.Text;
            settings.Host = textBoxServerAddress.Text;
            settings.Port = (int)numericUpDownPort.Value;
            settings.UseSsl = checkBoxUseHttps.Checked;
            settings.DefaultUsername = textBoxUsername.Text;
            settings.DefaultPassword = textBoxPassword.Text;
            settings.ApiKey = textBoxApiKey.Text;

            // Tab 2: Map & Regions
            settings.MapProvider = (MapProvider)(comboBoxMapProvider.SelectedIndex >= 0 ? comboBoxMapProvider.SelectedIndex : 0);
            settings.MapboxAccessToken = textBoxMapboxToken.Text;
            settings.EnableMapCaching = checkBoxEnableMapCaching.Checked;
            settings.Enable3DMap = checkBoxEnable3DMap.Checked;
            settings.Enable3DBuildings = checkBoxEnable3DBuildings.Checked;
            settings.Enable3DTerrain = checkBoxEnable3DTerrain.Checked;
            settings.DefaultLatitude = ParseDoubleOrDefault(textBoxLatitude.Text, 45.5098);  // Oregon Zoo
            settings.DefaultLongitude = ParseDoubleOrDefault(textBoxLongitude.Text, -122.7161);  // Oregon Zoo
            settings.DefaultZoomLevel = ParseDoubleOrDefault(textBoxZoom.Text, 14);
            settings.PollingIntervalSeconds = (double)numericUpDownPollingInterval.Value;
            settings.SelectedRegionIds = GetSelectedRegionIds();

            // Tab 3: Cameras
            settings.AssociatedCameraIds = GetSelectedCameraIds();

            settings.ApplyToItem(_item);
        }

        private static double ParseDoubleOrDefault(string value, double defaultValue)
        {
            return double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultValue;
        }

        private void LoadSelectedCameras(string cameraIdsString)
        {
            if (string.IsNullOrEmpty(cameraIdsString)) return;

            var ids = cameraIdsString.Split(',');
            foreach (string idStr in ids)
            {
                if (Guid.TryParse(idStr.Trim(), out Guid id))
                {
                    for (int i = 0; i < checkedListBoxCameras.Items.Count; i++)
                    {
                        if (checkedListBoxCameras.Items[i] is CameraItem cam && cam.Id == id)
                        {
                            checkedListBoxCameras.SetItemChecked(i, true);
                            break;
                        }
                    }
                }
            }
        }

        private string GetSelectedCameraIds()
        {
            var ids = new System.Collections.Generic.List<string>();
            foreach (CameraItem cam in checkedListBoxCameras.CheckedItems)
            {
                ids.Add(cam.Id.ToString());
            }
            return string.Join(",", ids);
        }

        private void UpdateHealthStatus(HealthStatus status, DateTime? lastCheck)
        {
            switch (status)
            {
                case HealthStatus.Healthy:
                    labelHealthStatus.Text = "? Healthy";
                    labelHealthStatus.ForeColor = Color.Green;
                    break;
                case HealthStatus.Degraded:
                    labelHealthStatus.Text = "? Degraded";
                    labelHealthStatus.ForeColor = Color.Orange;
                    break;
                case HealthStatus.Unhealthy:
                    labelHealthStatus.Text = "? Unhealthy";
                    labelHealthStatus.ForeColor = Color.Red;
                    break;
                case HealthStatus.Disconnected:
                    labelHealthStatus.Text = "? Disconnected";
                    labelHealthStatus.ForeColor = Color.DarkRed;
                    break;
                default:
                    labelHealthStatus.Text = "? Unknown";
                    labelHealthStatus.ForeColor = Color.Gray;
                    break;
            }

            labelLastHealthCheck.Text = lastCheck.HasValue ? lastCheck.Value.ToLocalTime().ToString("g") : "Never";
        }

        private async void UpdateSitePreview(RemoteServerSettings settings)
        {
            if (webViewSitePreview == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateSitePreview: WebView2 control is null");
                return;
            }

            if (settings == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateSitePreview: Settings are null");
                ShowMapPlaceholder();
                return;
            }

            // Check if WebView2 is initialized
            if (webViewSitePreview.CoreWebView2 == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateSitePreview: CoreWebView2 not initialized yet, will retry when ready");
                
                // Store settings for later update when WebView2 is ready
                // The HandleCreated event will call UpdateSitePreview again
                return;
            }

            try
            {
                var lat = settings.DefaultLatitude;
                var lon = settings.DefaultLongitude;
                var zoom = settings.DefaultZoomLevel;
                
                System.Diagnostics.Debug.WriteLine($"UpdateSitePreview: Lat={lat}, Lon={lon}, Zoom={zoom}");
                System.Diagnostics.Debug.WriteLine($"  From text boxes: Lat='{textBoxLatitude.Text}', Lon='{textBoxLongitude.Text}', Zoom='{textBoxZoom.Text}'");
                
                var siteName = textBoxName?.Text ?? "Site";
                
                var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <title>Site Preview</title>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
    <style>
        html, body, #map {{ height: 100%; margin: 0; padding: 0; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <script>
        console.log('Creating map at lat={lat}, lon={lon}, zoom={zoom}');
        var map = L.map('map').setView([{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}], {zoom.ToString(System.Globalization.CultureInfo.InvariantCulture)});
        
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            maxZoom: 19,
            attribution: '© OpenStreetMap'
        }}).addTo(map);
        
        var marker = L.marker([{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}]).addTo(map);
        marker.bindPopup('{siteName.Replace("'", "\\'")}').openPopup();
        
        {(settings.SiteRadiusMeters > 0 ? $@"L.circle([{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}], {{
            radius: {settings.SiteRadiusMeters.ToString(System.Globalization.CultureInfo.InvariantCulture)},
            color: '#1e88e5',
            fillColor: '#1e88e5',
            fillOpacity: 0.1
        }}).addTo(map);" : "")}
        
        console.log('Map created successfully');
    </script>
</body>
</html>";

                webViewSitePreview.NavigateToString(html);
                System.Diagnostics.Debug.WriteLine("UpdateSitePreview: Map HTML loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating site preview: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                DiagnosticLogger.WriteException("UpdateSitePreview", ex);
            }
        }

        private void ShowMapPlaceholder()
        {
            if (webViewSitePreview == null || webViewSitePreview.CoreWebView2 == null)
                return;

            try
            {
                var html = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <title>Map Preview</title>
    <style>
        html, body { 
            height: 100%; 
            margin: 0; 
            padding: 0; 
            display: flex;
            align-items: center;
            justify-content: center;
            background: #f0f0f0;
            font-family: 'Segoe UI', sans-serif;
        }
        .placeholder {
            text-align: center;
            color: #666;
        }
        .placeholder h2 {
            margin: 0 0 10px 0;
            color: #333;
        }
        .placeholder p {
            margin: 5px 0;
        }
    </style>
</head>
<body>
    <div class='placeholder'>
        <h2>Map Preview</h2>
        <p>Enter coordinates and zoom level</p>
        <p>to see site location preview</p>
    </div>
</body>
</html>";
                
                webViewSitePreview.NavigateToString(html);
                System.Diagnostics.Debug.WriteLine("Placeholder map shown");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing placeholder: {ex.Message}");
            }
        }

        private void ButtonQueryAllEvents_Click(object sender, EventArgs e)
        {
            DiagnosticLogger.WriteSection("QUERY ALL EVENTS (UserDefinedEventFolder.UserDefinedEvents)");
            
            try
            {
                ServerId msServerId = Configuration.Instance.ServerFQID.ServerId;
                var ms = new ManagementServer(msServerId);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var folder = ms.UserDefinedEventFolder.UserDefinedEvents.ToArray();
                var alarmFolder = ms.AlarmDefinitionFolder.AlarmDefinitions.ToArray();
                

                var userDef1 = new ManagementServer(msServerId).UserDefinedEventFolder;
                userDef1.ClearChildrenCache();
                userDef1.FillChildren(new[] { "UserDefinedEvents" });
                foreach (var udv in userDef1.UserDefinedEvents)
                { 
                    DiagnosticLogger.WriteLine($"Using FillInChildrenUDE from userDef1: {udv.Name}");
                }

                //var folder = new UserDefinedEventFolder();
                DiagnosticLogger.WriteLine($"Management Server:  {ms.Name}");
                DiagnosticLogger.WriteLine($"User Events Folder:  {folder.Count()}");
                DiagnosticLogger.WriteLine($"User Events Folder Path:  {folder.Length}");
                DiagnosticLogger.WriteLine($"Alarm Folder:  {alarmFolder.Count()}");
                DiagnosticLogger.WriteLine($"Alarm   Folder Path:  {alarmFolder.Length}");



                //folder.ClearChildrenCache();
                //folder.FillChildren(new[] { "UserDefinedEvent" });



                //var userEvents = folder.UserDefinedEvents?.ToList() ?? new System.Collections.Generic.List<UserDefinedEvent>();
                sw.Stop();
                
                DiagnosticLogger.WriteLine($"Query completed in {sw.ElapsedMilliseconds}ms");
                DiagnosticLogger.WriteLine($"Total UserDefinedEvents: {folder.Count()}");
                DiagnosticLogger.WriteLine($"Total AlarmDefinitions: {alarmFolder.Count()}");


                var info = new System.Text.StringBuilder();
                info.AppendLine($"UserDefinedEventFolder.UserDefinedEvents Query:");
                info.AppendLine();
                info.AppendLine($"Time: {sw.ElapsedMilliseconds}ms");
                info.AppendLine($"Total Events: {folder.Count()}");
                info.AppendLine();
                
                if (folder.Count() > 0)
                {
                    info.AppendLine("User-Defined Events:");
                    DiagnosticLogger.WriteLine("All User-Defined Events:");
                    var Eventinfo = new System.Text.StringBuilder();
                    DumpItemCollections(folder, Eventinfo);
                    DiagnosticLogger.WriteLine(Eventinfo.ToString());
                   

                    var Alarminfo = new System.Text.StringBuilder();
                    DumpItemCollections(alarmFolder, Alarminfo);
                    DiagnosticLogger.WriteLine(Alarminfo.ToString());
                    foreach (var evt in folder)
                    {
                        info.AppendLine($"  - {evt.Name}");
                        DiagnosticLogger.WriteLine($"  - Event Name: {evt.Name}");
                        DiagnosticLogger.WriteLine($"    Event Path: {evt.Path}");
                       
        
                    }
                    
                    // Check for NRK events
                    var nrkEvents = folder.Where(ev => ev.Name.Contains("NRK")).ToList();
                    if (nrkEvents.Any())
                    {
                        info.AppendLine();
                        info.AppendLine($"? NRK Events Found: {nrkEvents.Count}");
                        foreach (var nrkEvt in nrkEvents)
                        {
                            info.AppendLine($"    • {nrkEvt.Name}");
                        }
                    }
                    else
                    {
                        info.AppendLine();
                        info.AppendLine("?? No NRK events found");
                    }
                }
                else
                {
                    info.AppendLine("?? NO USER-DEFINED EVENTS FOUND!");
                    info.AppendLine();
                    info.AppendLine("FillChildren() returned 0 events.");
                    info.AppendLine("Create events manually in");
                    info.AppendLine("Management Client to test.");
                }
                
                info.AppendLine();
                //info.AppendLine($"Log: {DiagnosticLogger.GetLogFilePath()}");
                
                MessageBox.Show(info.ToString(), "Query All Events", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagnosticLogger.WriteException("ButtonQueryAllEvents_Click", ex);
                MessageBox.Show($"Error querying events:\n\n{ex.Message}\n\nSee log for stack trace", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void DumpItemCollections(object obj, System.Text.StringBuilder info)
        {
            if (obj == null)
                return;

            var type = obj.GetType();
            info.AppendLine($"Inspecting: {type.FullName}");

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Skip indexers
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                object value;
                try
                {
                    value = prop.GetValue(obj);
                }
                catch
                {
                    continue;
                }

                if (value is IEnumerable enumerable && !(value is string))
                {
                    info.AppendLine($"  Property: {prop.Name}");

                    foreach (var item in enumerable)
                    {
                        if (item == null)
                            continue;

                        if (item is Item mipItem)
                        {
                            info.AppendLine($"    - {mipItem.Name}");
                            //DiagnosticLogger.WriteLine($"    Path: {mipItem..Path}");
                        }
                        else
                        {
                            info.AppendLine($"    - {item}");
                        }
                    }
                }
            }
        }

        private class CameraItem
        {
            public string Name { get; set; }
            public Guid Id { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
