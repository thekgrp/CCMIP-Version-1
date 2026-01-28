using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
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
            textBoxLatitude.Text = "0";
            textBoxLongitude.Text = "0";
            textBoxZoom.Text = "8";
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
            int controlWidth = 320;
            int rowHeight = 35;

            // Map Settings Group
            var grpMap = new GroupBox();
            grpMap.Text = "Map Settings";
            grpMap.Location = new Point(10, y);
            grpMap.Size = new Size(600, 280);
            tab.Controls.Add(grpMap);

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

            // Enable 3D Buildings (indented)
            checkBoxEnable3DBuildings = new CheckBox { Text = "Show 3D Buildings", Location = new Point(controlX + 20, gy), Size = new Size(200, 20), Checked = true };
            checkBoxEnable3DBuildings.CheckedChanged += OnUserChange;
            grpMap.Controls.Add(checkBoxEnable3DBuildings);
            gy += rowHeight;

            // Enable 3D Terrain (indented)
            checkBoxEnable3DTerrain = new CheckBox { Text = "Show 3D Terrain", Location = new Point(controlX + 20, gy), Size = new Size(200, 20), Checked = true };
            checkBoxEnable3DTerrain.CheckedChanged += OnUserChange;
            grpMap.Controls.Add(checkBoxEnable3DTerrain);
            gy += rowHeight;

            // Default Latitude
            var lblLat = new Label { Text = "Default Latitude:", Location = new Point(labelX, gy), Size = new Size(140, 20) };
            textBoxLatitude = new TextBox { Location = new Point(controlX, gy), Size = new Size(120, 20), Text = "0" };
            textBoxLatitude.TextChanged += OnMapSettingChanged;
            grpMap.Controls.Add(lblLat);
            grpMap.Controls.Add(textBoxLatitude);
            gy += rowHeight;

            // Default Longitude
            var lblLon = new Label { Text = "Default Longitude:", Location = new Point(labelX, gy), Size = new Size(140, 20) };
            textBoxLongitude = new TextBox { Location = new Point(controlX, gy), Size = new Size(120, 20), Text = "0" };
            textBoxLongitude.TextChanged += OnMapSettingChanged;
            grpMap.Controls.Add(lblLon);
            grpMap.Controls.Add(textBoxLongitude);
            gy += rowHeight;

            // Default Zoom
            var lblZoom = new Label { Text = "Default Zoom:", Location = new Point(labelX, gy), Size = new Size(140, 20) };
            textBoxZoom = new TextBox { Location = new Point(controlX, gy), Size = new Size(120, 20), Text = "8" };
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

            y += 260;

            // Map Preview Group (NEW - was missing!)
            var grpPreview = new GroupBox();
            grpPreview.Text = "Map Preview";
            grpPreview.Location = new Point(10, y);
            grpPreview.Size = new Size(600, 280);
            tab.Controls.Add(grpPreview);

            var lblPreviewInfo = new Label { 
                Text = "Preview of configured map location:", 
                Location = new Point(15, 25), 
                Size = new Size(400, 20) 
            };
            grpPreview.Controls.Add(lblPreviewInfo);

            webViewSitePreview = new Microsoft.Web.WebView2.WinForms.WebView2();
            webViewSitePreview.Location = new Point(15, 50);
            webViewSitePreview.Size = new Size(570, 220);
            
            // Initialize WebView2 when control is ready
            webViewSitePreview.HandleCreated += async (s, ev) =>
            {
                try
                {
                    await webViewSitePreview.EnsureCoreWebView2Async(null);
                    System.Diagnostics.Debug.WriteLine("WebView2 initialized on HandleCreated");
                    
                    // Load initial preview if settings exist
                    if (_item != null)
                    {
                        var settings = RemoteServerSettings.FromItem(_item);
                        UpdateSitePreview(settings);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error initializing WebView2: {ex.Message}");
                }
            };
            
            grpPreview.Controls.Add(webViewSitePreview);
        }

        private void CreateAlarmWiringTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 20;

            // Event Types Group
            var grpEvents = new GroupBox();
            grpEvents.Text = "Event Types";
            grpEvents.Location = new Point(10, y);
            grpEvents.Size = new Size(360, 250);  // Increased width and height
            tab.Controls.Add(grpEvents);

            var lblEvents = new Label { 
                Text = "Available C2 Event Types:", 
                Location = new Point(15, 30), 
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            grpEvents.Controls.Add(lblEvents);

            listBoxEventTypes = new ListBox();
            listBoxEventTypes.Location = new Point(15, 55);
            listBoxEventTypes.Size = new Size(330, 180);  // Increased size
            listBoxEventTypes.Items.AddRange(new object[] {
                "C2.Alert (Medium severity)",
                "C2.Alarm (High severity)",
                "C2.AlarmCleared (Info)",
                "C2.TrackEnterRegion (Info)",
                "C2.TrackLost (Info)"
            });
            grpEvents.Controls.Add(listBoxEventTypes);

            // Alarm Definitions Group
            var grpAlarms = new GroupBox();
            grpAlarms.Text = "Recommended Alarm Definitions";
            grpAlarms.Location = new Point(380, y);  // More space from left group
            grpAlarms.Size = new Size(400, 250);  // Increased height
            tab.Controls.Add(grpAlarms);

            var lblAlarms = new Label {
                Text = "These alarms will be created in Milestone:",
                Location = new Point(15, 30),
                Size = new Size(370, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            grpAlarms.Controls.Add(lblAlarms);

            dataGridViewAlarms = new DataGridView();
            dataGridViewAlarms.Location = new Point(15, 55);
            dataGridViewAlarms.Size = new Size(370, 120);  // Adjusted size
            dataGridViewAlarms.AllowUserToAddRows = false;
            dataGridViewAlarms.ReadOnly = true;
            dataGridViewAlarms.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dataGridViewAlarms.RowHeadersVisible = false;
            dataGridViewAlarms.ScrollBars = ScrollBars.Vertical;
            dataGridViewAlarms.AllowUserToResizeColumns = true;
            
            dataGridViewAlarms.Columns.Add("Name", "Alarm Name");
            dataGridViewAlarms.Columns.Add("Event", "Event");
            dataGridViewAlarms.Columns.Add("Severity", "Severity");
            dataGridViewAlarms.Columns.Add("Status", "Status");
            
            // Set explicit column widths
            dataGridViewAlarms.Columns[0].Width = 130;  // Name
            dataGridViewAlarms.Columns[1].Width = 80;   // Event
            dataGridViewAlarms.Columns[2].Width = 70;   // Severity
            dataGridViewAlarms.Columns[3].Width = 90;   // Status

            // Add recommended alarms (note: site-specific names will be created)
            var siteName = string.IsNullOrWhiteSpace(textBoxName?.Text) ? "[Site Name]" : textBoxName.Text;
            dataGridViewAlarms.Rows.Add($"C2 Alert - {siteName}", "C2.Alert", "Medium", "Not Created");
            dataGridViewAlarms.Rows.Add($"C2 Alarm - {siteName}", "C2.Alarm", "High", "Not Created");
            
            grpAlarms.Controls.Add(dataGridViewAlarms);

            // Status label
            labelWiringStatus = new Label {
                Text = "No events or alarms created yet",
                Location = new Point(15, 185),
                Size = new Size(740, 40),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
            grpAlarms.Controls.Add(labelWiringStatus);

            // Step 1: Create Events button
            buttonCreateEvents = new Button { 
                Text = "Step 1: Create Events", 
                Location = new Point(15, 230), 
                Size = new Size(180, 35),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            buttonCreateEvents.Click += ButtonCreateEvents_Click;
            grpAlarms.Controls.Add(buttonCreateEvents);

            // Step 2: Create Alarms button (initially disabled)
            buttonCreateAlarms = new Button { 
                Text = "Step 2: Create Alarms", 
                Location = new Point(205, 230), 
                Size = new Size(180, 35),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false
            };
            buttonCreateAlarms.Click += ButtonCreateAlarms_Click;
            grpAlarms.Controls.Add(buttonCreateAlarms);

            // Refresh button
            var buttonRefreshStatus = new Button {
                Text = "Refresh Status",
                Location = new Point(395, 230),
                Size = new Size(120, 35)
            };
            buttonRefreshStatus.Click += (s, ev) => UpdateWiringStatus();
            grpAlarms.Controls.Add(buttonRefreshStatus);

            y += 330;  // Increased height for new buttons

            // Existing Definitions Group (NEW)
            var grpExisting = new GroupBox();
            grpExisting.Text = "Existing Event & Alarm Definitions";
            grpExisting.Location = new Point(10, y);
            grpExisting.Size = new Size(770, 200);
            tab.Controls.Add(grpExisting);

            var lblExisting = new Label {
                Text = "C2 events and alarms currently in Milestone:",
                Location = new Point(15, 30),
                Size = new Size(740, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            grpExisting.Controls.Add(lblExisting);

            var listBoxExisting = new ListBox();
            listBoxExisting.Name = "listBoxExistingDefinitions";
            listBoxExisting.Location = new Point(15, 55);
            listBoxExisting.Size = new Size(620, 130);
            grpExisting.Controls.Add(listBoxExisting);

            var btnRefreshList = new Button {
                Text = "Refresh",
                Location = new Point(645, 55),
                Size = new Size(110, 30)
            };
            btnRefreshList.Click += (s, ev) => LoadExistingEventDefinitions();
            grpExisting.Controls.Add(btnRefreshList);

            var btnDeleteSelected = new Button {
                Text = "Info",
                Location = new Point(645, 95),
                Size = new Size(110, 30)
            };
            btnDeleteSelected.Click += ButtonShowInfo_Click;
            grpExisting.Controls.Add(btnDeleteSelected);

            var btnOpenInMC = new Button {
                Text = "Open in MC",
                Location = new Point(645, 135),
                Size = new Size(110, 30)
            };
            btnOpenInMC.Click += ButtonOpenInMC_Click;
            grpExisting.Controls.Add(btnOpenInMC);

            y += 210;

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

        private async void ButtonCreateAlarms_Click(object sender, EventArgs e)
        {
            var originalCursor = this.Cursor;
            
            try
            {
                this.Cursor = Cursors.WaitCursor;
                buttonCreateAlarms.Enabled = false;
                buttonCreateAlarms.Text = "Creating Alarms...";
                Application.DoEvents();

                var siteName = textBoxName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(siteName))
                {
                    MessageBox.Show("Please enter a site name first.", "Site Name Required", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"This will create Alarm Definitions for site: {siteName}\n\n" +
                    $"Alarms to create:\n" +
                    $"  • C2 Alert - {siteName} (Priority: Medium)\n" +
                    $"  • C2 Alarm - {siteName} (Priority: High)\n\n" +
                    $"Associated cameras: {checkedListBoxCameras.CheckedItems.Count}\n\n" +
                    "Do you want to proceed?",
                    "Create Alarm Definitions",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                this.Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                int createdCount = 0;
                int skippedCount = 0;
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

                // Create C2 Alert Alarm
                try
                {
                    var success = CreateAlarmDefinition(
                        siteName,
                        $"C2.Alert - {siteName}",
                        $"C2 Alert - {siteName}",
                        $"Medium severity alerts from C2 tracking system at {siteName}",
                        "Medium",
                        cameraPaths.ToArray(),
                        out bool created);

                    if (created) createdCount++;
                    else if (success) skippedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"C2 Alert: {ex.Message}");
                }

                // Create C2 Alarm Alarm
                try
                {
                    var success = CreateAlarmDefinition(
                        siteName,
                        $"C2.Alarm - {siteName}",
                        $"C2 Alarm - {siteName}",
                        $"High severity alarms from C2 tracking system at {siteName}",
                        "High",
                        cameraPaths.ToArray(),
                        out bool created);

                    if (created) createdCount++;
                    else if (success) skippedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"C2 Alarm: {ex.Message}");
                }

                // Wait and refresh
                await System.Threading.Tasks.Task.Delay(1000);
                UpdateWiringStatus();

                // Show results
                var message = $"Alarm Creation Complete for Site: {siteName}\n\n" +
                             $"Created: {createdCount} alarms\n" +
                             $"Already existed: {skippedCount} alarms\n\n";

                if (createdCount > 0)
                {
                    message += "? Alarms created successfully!\n\n" +
                              "The alarms are now active and will trigger when C2 sends track alarms.";
                }

                if (errors.Count > 0)
                {
                    message += "\n\nErrors:\n" + string.Join("\n", errors);
                    MessageBox.Show(message, "Alarm Creation Results", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(message, "Alarms Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating alarms:\n\n{ex.Message}\n\nSee Debug Output for details", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Create alarms exception: {ex}");
            }
            finally
            {
                this.Cursor = originalCursor;
                buttonCreateAlarms.Enabled = true;
                buttonCreateAlarms.Text = "Step 2: Create Alarms";
            }
        }

        private bool CreateAlarmDefinition(
            string siteName,
            string udeName,
            string alarmName,
            string alarmDescription,
            string priority,
            string[] cameraPaths,
            out bool created)
        {
            created = false;

            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Creating alarm: {alarmName} ===");

                // Get UDE
                var udeFolder = new UserDefinedEventFolder();
                udeFolder.FillChildren(new[] { nameof(UserDefinedEvent) });

                var ude = udeFolder.UserDefinedEvents
                    .FirstOrDefault(e => string.Equals(e.Name, udeName, StringComparison.OrdinalIgnoreCase));

                if (ude == null)
                {
                    throw new InvalidOperationException($"Event '{udeName}' not found. Please create events first (Step 1).");
                }

                if (string.IsNullOrEmpty(ude.Path))
                {
                    throw new InvalidOperationException($"Event '{udeName}' has no path.");
                }

                System.Diagnostics.Debug.WriteLine($"Found UDE: {udeName}, Path: {ude.Path}");

                // Check if alarm already exists
                var alarmFolder = new AlarmDefinitionFolder();
                alarmFolder.FillChildren(new[] { nameof(AlarmDefinition) });

                var existingAlarm = alarmFolder.AlarmDefinitions
                    .FirstOrDefault(a => string.Equals(a.Name, alarmName, StringComparison.OrdinalIgnoreCase));

                if (existingAlarm != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Alarm already exists: {alarmName}");
                    return true; // Success, but not created
                }

                // Create alarm
                var relatedCameraList = (cameraPaths != null && cameraPaths.Length > 0)
                    ? string.Join(",", cameraPaths)
                    : string.Empty;

                System.Diagnostics.Debug.WriteLine($"Creating alarm with {cameraPaths?.Length ?? 0} cameras");

                var task = alarmFolder.AddAlarmDefinition(
                    name: alarmName,
                    description: alarmDescription,
                    eventTypeGroup: "External Events",
                    eventType: "External Event",
                    sourceList: ude.Path,
                    enableRule: "Always",
                    timeProfile: string.Empty,
                    enableEventList: string.Empty,
                    disableEventList: string.Empty,
                    managementTimeoutTime: string.Empty,
                    managementTimeoutEventList: string.Empty,
                    relatedCameraList: relatedCameraList,
                    mapType: string.Empty,
                    relatedMap: string.Empty,
                    owner: string.Empty,
                    priority: priority,
                    category: "C2 Alarms",
                    triggerEventlist: string.Empty);

                WaitForTaskOrThrow(task, $"Failed creating alarm '{alarmName}'");
                created = true;
                System.Diagnostics.Debug.WriteLine($"? Created alarm: {alarmName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating alarm: {ex.Message}");
                throw;
            }
        }

        private void UpdateWiringStatus()
        {
            try
            {
                var siteName = textBoxName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(siteName))
                {
                    labelWiringStatus.Text = "Enter a site name to begin";
                    labelWiringStatus.ForeColor = Color.Gray;
                    buttonCreateEvents.Enabled = false;
                    buttonCreateAlarms.Enabled = false;
                    return;
                }

                buttonCreateEvents.Enabled = true;

                // Check events
                var udeFolder = new UserDefinedEventFolder();
                udeFolder.FillChildren(new[] { nameof(UserDefinedEvent) });

                var alertEvent = udeFolder.UserDefinedEvents
                    .FirstOrDefault(e => string.Equals(e.Name, $"C2.Alert - {siteName}", StringComparison.OrdinalIgnoreCase));
                var alarmEvent = udeFolder.UserDefinedEvents
                    .FirstOrDefault(e => string.Equals(e.Name, $"C2.Alarm - {siteName}", StringComparison.OrdinalIgnoreCase));

                bool eventsExist = alertEvent != null && alarmEvent != null;

                // Check alarms
                var alarmFolder = new AlarmDefinitionFolder();
                alarmFolder.FillChildren(new[] { nameof(AlarmDefinition) });

                var alertAlarm = alarmFolder.AlarmDefinitions
                    .FirstOrDefault(a => string.Equals(a.Name, $"C2 Alert - {siteName}", StringComparison.OrdinalIgnoreCase));
                var alarmAlarm = alarmFolder.AlarmDefinitions
                    .FirstOrDefault(a => string.Equals(a.Name, $"C2 Alarm - {siteName}", StringComparison.OrdinalIgnoreCase));

                bool alarmsExist = alertAlarm != null && alarmAlarm != null;

                // Update status
                if (eventsExist && alarmsExist)
                {
                    labelWiringStatus.Text = $"? All events and alarms exist for site '{siteName}'";
                    labelWiringStatus.ForeColor = Color.DarkGreen;
                    buttonCreateEvents.Enabled = true; // Can recreate if needed
                    buttonCreateAlarms.Enabled = true; // Can recreate if needed
                }
                else if (eventsExist)
                {
                    labelWiringStatus.Text = $"? Events exist. Ready to create alarms for site '{siteName}'";
                    labelWiringStatus.ForeColor = Color.DarkBlue;
                    buttonCreateEvents.Enabled = true;
                    buttonCreateAlarms.Enabled = true; // Enable Step 2
                }
                else
                {
                    labelWiringStatus.Text = $"No events created yet. Click 'Step 1: Create Events' to begin.";
                    labelWiringStatus.ForeColor = Color.OrangeRed;
                    buttonCreateEvents.Enabled = true;
                    buttonCreateAlarms.Enabled = false; // Disable until events exist
                }

                // Update DataGrid
                dataGridViewAlarms.Rows.Clear();
                dataGridViewAlarms.Rows.Add($"C2 Alert - {siteName}", "C2.Alert", "Medium", 
                    alertEvent != null ? "? Created" : "Not Created");
                dataGridViewAlarms.Rows.Add($"C2 Alarm - {siteName}", "C2.Alarm", "High", 
                    alarmEvent != null ? "? Created" : "Not Created");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating wiring status: {ex.Message}");
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
                DefaultLatitude = ParseDoubleOrDefault(textBoxLatitude.Text, 0),
                DefaultLongitude = ParseDoubleOrDefault(textBoxLongitude.Text, 0),
                DefaultZoomLevel = ParseDoubleOrDefault(textBoxZoom.Text, 8),
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
                buttonCreateEvents.Text = "Creating Events...";
                Application.DoEvents();

                var siteName = textBoxName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(siteName))
                {
                    MessageBox.Show("Please enter a site name first.", "Site Name Required", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"This will create User-Defined Events for site: {siteName}\n\n" +
                    $"Events to create:\n" +
                    $"  • C2.Alert - {siteName} (Medium severity)\n" +
                    $"  • C2.Alarm - {siteName} (High severity)\n\n" +
                    "Do you want to proceed?",
                    "Create User-Defined Events",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                this.Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                int createdCount = 0;
                int skippedCount = 0;
                var errors = new System.Collections.Generic.List<string>();

                // Create C2.Alert Event
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Creating C2.Alert event for {siteName}...");
                    
                    var udeFolder = new UserDefinedEventFolder();
                    udeFolder.FillChildren(new[] { nameof(UserDefinedEvent) });

                    var udeName = $"C2.Alert - {siteName}";
                    var existingUde = udeFolder.UserDefinedEvents
                        .FirstOrDefault(ude => string.Equals(ude.Name, udeName, StringComparison.OrdinalIgnoreCase));

                    if (existingUde == null)
                    {
                        var task = udeFolder.AddUserDefinedEvent(udeName);
                        WaitForTaskOrThrow(task, $"Failed creating UDE '{udeName}'");
                        createdCount++;
                        System.Diagnostics.Debug.WriteLine($"? Created UDE: {udeName}");
                    }
                    else
                    {
                        skippedCount++;
                        System.Diagnostics.Debug.WriteLine($"UDE already exists: {udeName}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"C2.Alert: {ex.Message}");
                }

                // Create C2.Alarm Event
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Creating C2.Alarm event for {siteName}...");
                    
                    var udeFolder = new UserDefinedEventFolder();
                    udeFolder.FillChildren(new[] { nameof(UserDefinedEvent) });

                    var udeName = $"C2.Alarm - {siteName}";
                    var existingUde = udeFolder.UserDefinedEvents
                        .FirstOrDefault(ude => string.Equals(ude.Name, udeName, StringComparison.OrdinalIgnoreCase));

                    if (existingUde == null)
                    {
                        var task = udeFolder.AddUserDefinedEvent(udeName);
                        WaitForTaskOrThrow(task, $"Failed creating UDE '{udeName}'");
                        createdCount++;
                        System.Diagnostics.Debug.WriteLine($"? Created UDE: {udeName}");
                    }
                    else
                    {
                        skippedCount++;
                        System.Diagnostics.Debug.WriteLine($"UDE already exists: {udeName}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"C2.Alarm: {ex.Message}");
                }

                // Wait for server to propagate
                await System.Threading.Tasks.Task.Delay(1000);
                
                // Update status
                UpdateWiringStatus();

                // Show results
                var message = $"Event Creation Complete for Site: {siteName}\n\n" +
                             $"Created: {createdCount} events\n" +
                             $"Already existed: {skippedCount} events\n\n";

                if (createdCount > 0)
                {
                    message += "? Events created successfully!\n\n" +
                              "Now click 'Step 2: Create Alarms' to create the alarm definitions.";
                }

                if (errors.Count > 0)
                {
                    message += "\n\nErrors:\n" + string.Join("\n", errors);
                    MessageBox.Show(message, "Event Creation Results", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(message, "Events Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating events:\n\n{ex.Message}\n\nSee Debug Output for details", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Create events exception: {ex}");
            }
            finally
            {
                this.Cursor = originalCursor;
                buttonCreateEvents.Enabled = true;
                buttonCreateEvents.Text = "Step 1: Create Events";
            }
        }

        private void EnsureUdeAndAlarmDefinition(
            string udeName,
            string udeDescription,
            string alarmDefinitionName,
            string alarmDescription,
            string alarmPriority,
            string[] relatedCameraPaths,
            out bool udeCreated,
            out bool alarmCreated)
        {
            udeCreated = false;
            alarmCreated = false;

            try
            {
                System.Diagnostics.Debug.WriteLine($"=== EnsureUdeAndAlarmDefinition START ===");
                System.Diagnostics.Debug.WriteLine($"UDE Name: {udeName}");
                System.Diagnostics.Debug.WriteLine($"Alarm Name: {alarmDefinitionName}");

                // Access the UDE and Alarm folders using Management Client APIs
                System.Diagnostics.Debug.WriteLine("Creating UserDefinedEventFolder...");
                var udeFolder = new UserDefinedEventFolder();
                
                System.Diagnostics.Debug.WriteLine("Creating AlarmDefinitionFolder...");
                var alarmFolder = new AlarmDefinitionFolder();

                // Load children
                System.Diagnostics.Debug.WriteLine("Loading UDE children...");
                udeFolder.FillChildren(new[] { nameof(UserDefinedEvent) });
                System.Diagnostics.Debug.WriteLine($"Found {udeFolder.UserDefinedEvents?.Count ?? 0} existing UDEs");
                
                System.Diagnostics.Debug.WriteLine("Loading Alarm children...");
                alarmFolder.FillChildren(new[] { nameof(AlarmDefinition) });
                System.Diagnostics.Debug.WriteLine($"Found {alarmFolder.AlarmDefinitions?.Count ?? 0} existing Alarms");

                // 1) Ensure User-Defined Event exists
                var ude = udeFolder.UserDefinedEvents
                    .FirstOrDefault(e => string.Equals(e.Name, udeName, StringComparison.OrdinalIgnoreCase));

                if (ude == null)
                {
                    System.Diagnostics.Debug.WriteLine($"UDE not found, creating: {udeName}");
                    var task = udeFolder.AddUserDefinedEvent(udeName);
                    System.Diagnostics.Debug.WriteLine($"Task created, state: {task.State}");
                    
                    WaitForTaskOrThrow(task, $"Failed creating UDE '{udeName}'");

                    udeCreated = true;
                    System.Diagnostics.Debug.WriteLine($"? UDE created successfully");

                    // Try to get the UDE path, but don't fail if we can't find it immediately
                    System.Diagnostics.Debug.WriteLine("Waiting 500ms for server to update...");
                    System.Threading.Thread.Sleep(500);
                    
                    System.Diagnostics.Debug.WriteLine("Refreshing UDE list...");
                    udeFolder.ClearChildrenCache();
                    udeFolder.FillChildren(new[] { nameof(UserDefinedEvent) });
                    System.Diagnostics.Debug.WriteLine($"After refresh: {udeFolder.UserDefinedEvents?.Count ?? 0} UDEs");

                    ude = udeFolder.UserDefinedEvents
                        .FirstOrDefault(e => string.Equals(e.Name, udeName, StringComparison.OrdinalIgnoreCase));

                    if (ude == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR: UDE '{udeName}' created but still not queryable after refresh!");
                        throw new InvalidOperationException($"UDE '{udeName}' was created but cannot be found yet. The Management Server may need more time. Please wait 10 seconds and click 'Apply Recommended Wiring' again.");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"? UDE found after refresh");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"UDE already exists: {udeName}");
                }

                // Verify UDE has a valid path
                System.Diagnostics.Debug.WriteLine($"UDE Path: '{ude.Path}'");
                if (string.IsNullOrEmpty(ude.Path))
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: UDE path is null or empty!");
                    throw new InvalidOperationException($"UDE '{udeName}' has no path. Cannot create alarm definition.");
                }

                // 2) Ensure Alarm Definition exists
                var existingAlarm = alarmFolder.AlarmDefinitions
                    .FirstOrDefault(a => string.Equals(a.Name, alarmDefinitionName, StringComparison.OrdinalIgnoreCase));

                if (existingAlarm != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Alarm Definition already exists: {alarmDefinitionName}");
                    return;
                }

                // Create the alarm definition wired to the UDE
                System.Diagnostics.Debug.WriteLine($"Creating Alarm Definition: {alarmDefinitionName}");
                
                var eventTypeGroup = "External Events";
                var eventType = "External Event";
                var sourceList = ude.Path; // Link to UDE

                var relatedCameraList = (relatedCameraPaths != null && relatedCameraPaths.Length > 0)
                    ? string.Join(",", relatedCameraPaths)
                    : string.Empty;

                System.Diagnostics.Debug.WriteLine($"Alarm parameters:");
                System.Diagnostics.Debug.WriteLine($"  Name: {alarmDefinitionName}");
                System.Diagnostics.Debug.WriteLine($"  Description: {alarmDescription}");
                System.Diagnostics.Debug.WriteLine($"  EventTypeGroup: {eventTypeGroup}");
                System.Diagnostics.Debug.WriteLine($"  EventType: {eventType}");
                System.Diagnostics.Debug.WriteLine($"  SourceList: {sourceList}");
                System.Diagnostics.Debug.WriteLine($"  Priority: {alarmPriority}");
                System.Diagnostics.Debug.WriteLine($"  Camera paths: {relatedCameraPaths?.Length ?? 0}");
                if (relatedCameraPaths != null && relatedCameraPaths.Length > 0)
                {
                    foreach (var path in relatedCameraPaths)
                    {
                        System.Diagnostics.Debug.WriteLine($"    - {path}");
                    }
                }

                try
                {
                    System.Diagnostics.Debug.WriteLine("Calling AddAlarmDefinition...");
                    var addAlarmTask = alarmFolder.AddAlarmDefinition(
                        name: alarmDefinitionName,
                        description: alarmDescription,
                        eventTypeGroup: eventTypeGroup,
                        eventType: eventType,
                        sourceList: sourceList,
                        enableRule: "Always",
                        timeProfile: string.Empty,
                        enableEventList: string.Empty,
                        disableEventList: string.Empty,
                        managementTimeoutTime: string.Empty,
                        managementTimeoutEventList: string.Empty,
                        relatedCameraList: relatedCameraList,
                        mapType: string.Empty,
                        relatedMap: string.Empty,
                        owner: string.Empty,
                        priority: alarmPriority,
                        category: "C2 Alarms",
                        triggerEventlist: string.Empty);

                    System.Diagnostics.Debug.WriteLine($"Task created, state: {addAlarmTask.State}");
                    WaitForTaskOrThrow(addAlarmTask, $"Failed creating Alarm Definition '{alarmDefinitionName}'");
                    
                    alarmCreated = true;
                    System.Diagnostics.Debug.WriteLine($"? Alarm Definition created successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR creating alarm definition:");
                    System.Diagnostics.Debug.WriteLine($"  Message: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"  Type: {ex.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"  Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Inner exception: {ex.InnerException.Message}");
                    }
                    throw new InvalidOperationException($"Failed to create alarm '{alarmDefinitionName}': {ex.Message}", ex);
                }
                
                System.Diagnostics.Debug.WriteLine($"=== EnsureUdeAndAlarmDefinition END ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== EnsureUdeAndAlarmDefinition FAILED ===");
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Type: {ex.GetType().Name}");
                throw;
            }
        }

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
            
            // Tab 3: Load existing UDEs and Alarms and update wiring status
            LoadExistingEventDefinitions();
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
            settings.DefaultLatitude = ParseDoubleOrDefault(textBoxLatitude.Text, 0);
            settings.DefaultLongitude = ParseDoubleOrDefault(textBoxLongitude.Text, 0);
            settings.DefaultZoomLevel = ParseDoubleOrDefault(textBoxZoom.Text, 8);
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

        private void LoadExistingEventDefinitions()
        {
            try
            {
                // Find the listbox
                var listBox = FindControlByName<ListBox>(tabControl.TabPages[2], "listBoxExistingDefinitions");
                if (listBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadExistingEventDefinitions: ListBox not found!");
                    return;
                }

                listBox.Items.Clear();
                System.Diagnostics.Debug.WriteLine("=== Loading Existing Event Definitions ===");

                // Load User-Defined Events
                var udeFolder = new UserDefinedEventFolder();
                udeFolder.FillChildren(new[] { nameof(UserDefinedEvent) });

                System.Diagnostics.Debug.WriteLine($"Total UDEs found: {udeFolder.UserDefinedEvents?.Count ?? 0}");
                
                int udeCount = 0;
                if (udeFolder.UserDefinedEvents != null)
                {
                    foreach (var ude in udeFolder.UserDefinedEvents)
                    {
                        System.Diagnostics.Debug.WriteLine($"  UDE: {ude.Name}");
                        
                        if (ude.Name != null && (ude.Name.StartsWith("C2.") || ude.Name.Contains("C2 ") || ude.Name.Contains("C2-")))
                        {
                            var displayItem = new DefinitionListItem
                            {
                                Name = ude.Name,
                                Type = "[Event]",
                                Path = ude.Path,
                                ConfigItem = ude
                            };
                            listBox.Items.Add(displayItem);
                            udeCount++;
                            System.Diagnostics.Debug.WriteLine($"    ? Added C2 UDE: {ude.Name}");
                        }
                    }
                }

                // Load Alarm Definitions
                var alarmFolder = new AlarmDefinitionFolder();
                alarmFolder.FillChildren(new[] { nameof(AlarmDefinition) });

                System.Diagnostics.Debug.WriteLine($"Total Alarms found: {alarmFolder.AlarmDefinitions?.Count ?? 0}");
                
                int alarmCount = 0;
                if (alarmFolder.AlarmDefinitions != null)
                {
                    foreach (var alarm in alarmFolder.AlarmDefinitions)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Alarm: {alarm.Name}");
                        
                        if (alarm.Name != null && (alarm.Name.StartsWith("C2") || alarm.Name.Contains("C2 ") || alarm.Name.Contains("C2-")))
                        {
                            var displayItem = new DefinitionListItem
                            {
                                Name = alarm.Name,
                                Type = "[Alarm]",
                                Path = alarm.Path,
                                ConfigItem = alarm
                            };
                            listBox.Items.Add(displayItem);
                            alarmCount++;
                            System.Diagnostics.Debug.WriteLine($"    ? Added C2 Alarm: {alarm.Name}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"=== Loaded {udeCount} C2 events and {alarmCount} C2 alarms ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading existing definitions: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error loading definitions: {ex.Message}\n\nCheck Debug Output for details.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private T FindControlByName<T>(Control parent, string name) where T : Control
        {
            foreach (Control control in parent.Controls)
            {
                if (control is T && control.Name == name)
                    return (T)control;
                
                if (control.HasChildren)
                {
                    var found = FindControlByName<T>(control, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private void ButtonShowInfo_Click(object sender, EventArgs e)
        {
            try
            {
                var listBox = FindControlByName<ListBox>(tabControl.TabPages[2], "listBoxExistingDefinitions");
                if (listBox == null || listBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select an item to view details.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var selectedItem = listBox.SelectedItem as DefinitionListItem;
                if (selectedItem == null) return;

                var info = new System.Text.StringBuilder();
                info.AppendLine($"Name: {selectedItem.Name}");
                info.AppendLine($"Type: {selectedItem.Type}");
                info.AppendLine($"Path: {selectedItem.Path}");
                info.AppendLine();

                if (selectedItem.ConfigItem is UserDefinedEvent ude)
                {
                    info.AppendLine("User-Defined Event Details:");
                    info.AppendLine($"  Display Name: {ude.DisplayName}");
                }
                else if (selectedItem.ConfigItem is AlarmDefinition alarm)
                {
                    info.AppendLine("Alarm Definition Details:");
                    info.AppendLine($"  Display Name: {alarm.DisplayName}");
                    info.AppendLine($"  Priority: {alarm.Priority}");
                    info.AppendLine($"  Category: {alarm.Category}");
                    info.AppendLine($"  Event Type: {alarm.EventType}");
                    info.AppendLine($"  Source: {alarm.SourceList}");
                }

                MessageBox.Show(info.ToString(), "Item Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing info: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonOpenInMC_Click(object sender, EventArgs e)
        {
            var listBox = FindControlByName<ListBox>(tabControl.TabPages[2], "listBoxExistingDefinitions");
            if (listBox == null || listBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an item to open.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MessageBox.Show(
                "To modify this definition:\n\n" +
                "1. Open Management Client\n" +
                "2. Navigate to Rules and Events\n" +
                "3. Find the definition by name\n" +
                "4. Double-click to edit\n\n" +
                "Note: Direct navigation from plugin is not supported by Milestone API.",
                "Open in Management Client",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private class DefinitionListItem
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Path { get; set; }
            public object ConfigItem { get; set; } // Can be UserDefinedEvent or AlarmDefinition

            public override string ToString()
            {
                return $"{Type} {Name}";
            }
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
                return;
            }

            // Check if tab is visible
            if (!tabControl.TabPages[1].Visible)
            {
                System.Diagnostics.Debug.WriteLine("UpdateSitePreview: Tab 2 not visible yet, skipping");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdateSitePreview: Lat={settings.DefaultLatitude}, Lon={settings.DefaultLongitude}, Zoom={settings.DefaultZoomLevel}");
                
                // Ensure WebView2 is initialized
                await webViewSitePreview.EnsureCoreWebView2Async(null);
                System.Diagnostics.Debug.WriteLine("UpdateSitePreview: WebView2 initialized");

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
        console.log('Creating map at lat={settings.DefaultLatitude}, lon={settings.DefaultLongitude}, zoom={settings.DefaultZoomLevel}');
        var map = L.map('map').setView([{settings.DefaultLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {settings.DefaultLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], {settings.DefaultZoomLevel.ToString(System.Globalization.CultureInfo.InvariantCulture)});
        
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            maxZoom: 19,
            attribution: '© OpenStreetMap'
        }}).addTo(map);
        
        var marker = L.marker([{settings.DefaultLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {settings.DefaultLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}]).addTo(map);
        marker.bindPopup('{siteName.Replace("'", "\\'")}').openPopup();
        
        {(settings.SiteRadiusMeters > 0 ? $@"L.circle([{settings.DefaultLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {settings.DefaultLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], {{
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
                System.Diagnostics.Debug.WriteLine("UpdateSitePreview: Map HTML loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating site preview: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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
