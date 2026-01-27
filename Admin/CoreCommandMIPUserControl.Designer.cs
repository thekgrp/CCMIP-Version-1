namespace CoreCommandMIP.Admin
{
    partial class CoreCommandMIPUserControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.labelServer = new System.Windows.Forms.Label();
            this.textBoxServerAddress = new System.Windows.Forms.TextBox();
            this.labelPort = new System.Windows.Forms.Label();
            this.numericUpDownPort = new System.Windows.Forms.NumericUpDown();
            this.checkBoxUseHttps = new System.Windows.Forms.CheckBox();
            this.labelUsername = new System.Windows.Forms.Label();
            this.textBoxUsername = new System.Windows.Forms.TextBox();
            this.labelPassword = new System.Windows.Forms.Label();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.labelApiKey = new System.Windows.Forms.Label();
            this.textBoxApiKey = new System.Windows.Forms.TextBox();
            this.labelLatitude = new System.Windows.Forms.Label();
            this.textBoxLatitude = new System.Windows.Forms.TextBox();
            this.labelLongitude = new System.Windows.Forms.Label();
            this.textBoxLongitude = new System.Windows.Forms.TextBox();
            this.labelZoom = new System.Windows.Forms.Label();
            this.textBoxZoom = new System.Windows.Forms.TextBox();
            this.labelMapProvider = new System.Windows.Forms.Label();
            this.comboBoxMapProvider = new System.Windows.Forms.ComboBox();
            this.labelMapboxToken = new System.Windows.Forms.Label();
            this.textBoxMapboxToken = new System.Windows.Forms.TextBox();
            this.linkLabelGetMapboxToken = new System.Windows.Forms.LinkLabel();
            this.checkBoxEnableMapCaching = new System.Windows.Forms.CheckBox();
            this.labelPollingInterval = new System.Windows.Forms.Label();
            this.numericUpDownPollingInterval = new System.Windows.Forms.NumericUpDown();
            this.buttonTestConnection = new System.Windows.Forms.Button();
            this.labelRegions = new System.Windows.Forms.Label();
            this.checkedListBoxRegions = new System.Windows.Forms.CheckedListBox();
            this.buttonRefreshRegions = new System.Windows.Forms.Button();
            this.labelSitePreview = new System.Windows.Forms.Label();
            this.webViewSitePreview = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPollingInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.webViewSitePreview)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            // 
            // textBoxName
            // 
            this.textBoxName.Location = new System.Drawing.Point(84, 17);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(279, 20);
            this.textBoxName.TabIndex = 1;
            this.textBoxName.TextChanged += new System.EventHandler(this.OnUserChange);
            // 
            // labelServer
            // 
            this.labelServer.AutoSize = true;
            this.labelServer.Location = new System.Drawing.Point(13, 55);
            this.labelServer.Name = "labelServer";
            this.labelServer.Size = new System.Drawing.Size(78, 13);
            this.labelServer.TabIndex = 2;
            this.labelServer.Text = "Server address:";
            // 
            // textBoxServerAddress
            // 
            this.textBoxServerAddress.Location = new System.Drawing.Point(97, 52);
            this.textBoxServerAddress.Name = "textBoxServerAddress";
            this.textBoxServerAddress.Size = new System.Drawing.Size(266, 20);
            this.textBoxServerAddress.TabIndex = 3;
            this.textBoxServerAddress.TextChanged += new System.EventHandler(this.OnUserChange);
            // 
            // labelPort
            // 
            this.labelPort.AutoSize = true;
            this.labelPort.Location = new System.Drawing.Point(13, 87);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(29, 13);
            this.labelPort.TabIndex = 4;
            this.labelPort.Text = "Port:";
            // 
            // numericUpDownPort
            // 
            this.numericUpDownPort.Location = new System.Drawing.Point(97, 85);
            this.numericUpDownPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numericUpDownPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownPort.Name = "numericUpDownPort";
            this.numericUpDownPort.Size = new System.Drawing.Size(120, 20);
            this.numericUpDownPort.TabIndex = 5;
            this.numericUpDownPort.Value = new decimal(new int[] {
            443,
            0,
            0,
            0});
            this.numericUpDownPort.ValueChanged += new System.EventHandler(this.OnUserChange);
            // 
            // checkBoxUseHttps
            // 
            this.checkBoxUseHttps.AutoSize = true;
            this.checkBoxUseHttps.Checked = true;
            this.checkBoxUseHttps.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxUseHttps.Location = new System.Drawing.Point(239, 86);
            this.checkBoxUseHttps.Name = "checkBoxUseHttps";
            this.checkBoxUseHttps.Size = new System.Drawing.Size(86, 17);
            this.checkBoxUseHttps.TabIndex = 6;
            this.checkBoxUseHttps.Text = "Use HTTPS";
            this.checkBoxUseHttps.UseVisualStyleBackColor = true;
            this.checkBoxUseHttps.CheckedChanged += new System.EventHandler(this.OnUserChange);
            // 
            // labelApiKey
            // 
            this.labelUsername.AutoSize = true;
            this.labelUsername.Location = new System.Drawing.Point(13, 120);
            this.labelUsername.Name = "labelUsername";
            this.labelUsername.Size = new System.Drawing.Size(61, 13);
            this.labelUsername.TabIndex = 7;
            this.labelUsername.Text = "Username:";
            // 
            // textBoxUsername
            // 
            this.textBoxUsername.Location = new System.Drawing.Point(97, 117);
            this.textBoxUsername.Name = "textBoxUsername";
            this.textBoxUsername.Size = new System.Drawing.Size(266, 20);
            this.textBoxUsername.TabIndex = 8;
            this.textBoxUsername.TextChanged += new System.EventHandler(this.OnUserChange);
            // 
            // labelPassword
            // 
            this.labelPassword.AutoSize = true;
            this.labelPassword.Location = new System.Drawing.Point(13, 152);
            this.labelPassword.Name = "labelPassword";
            this.labelPassword.Size = new System.Drawing.Size(59, 13);
            this.labelPassword.TabIndex = 9;
            this.labelPassword.Text = "Password:";
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.Location = new System.Drawing.Point(97, 149);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.Size = new System.Drawing.Size(266, 20);
            this.textBoxPassword.TabIndex = 10;
            this.textBoxPassword.UseSystemPasswordChar = true;
            this.textBoxPassword.TextChanged += new System.EventHandler(this.OnUserChange);
            // 
            // labelApiKey
            // 
            this.labelApiKey.AutoSize = true;
            this.labelApiKey.Location = new System.Drawing.Point(13, 184);
            this.labelApiKey.Name = "labelApiKey";
            this.labelApiKey.Size = new System.Drawing.Size(84, 13);
            this.labelApiKey.TabIndex = 11;
            this.labelApiKey.Text = "API key / token:";
            // 
            // textBoxApiKey
            // 
            this.textBoxApiKey.Location = new System.Drawing.Point(97, 181);
            this.textBoxApiKey.Name = "textBoxApiKey";
            this.textBoxApiKey.Size = new System.Drawing.Size(266, 20);
            this.textBoxApiKey.TabIndex = 12;
            this.textBoxApiKey.TextChanged += new System.EventHandler(this.OnUserChange);
            // 
            // labelLatitude
            // 
            this.labelLatitude.AutoSize = true;
            this.labelLatitude.Location = new System.Drawing.Point(13, 219);
            this.labelLatitude.Name = "labelLatitude";
            this.labelLatitude.Size = new System.Drawing.Size(48, 13);
            this.labelLatitude.TabIndex = 13;
            this.labelLatitude.Text = "Latitude:";
            // 
            // textBoxLatitude
            // 
            this.textBoxLatitude.Location = new System.Drawing.Point(97, 216);
            this.textBoxLatitude.Name = "textBoxLatitude";
            this.textBoxLatitude.Size = new System.Drawing.Size(120, 20);
            this.textBoxLatitude.TabIndex = 14;
            this.textBoxLatitude.TextChanged += new System.EventHandler(this.OnCoordinateChanged);
            // 
            // labelLongitude
            // 
            this.labelLongitude.AutoSize = true;
            this.labelLongitude.Location = new System.Drawing.Point(13, 251);
            this.labelLongitude.Name = "labelLongitude";
            this.labelLongitude.Size = new System.Drawing.Size(57, 13);
            this.labelLongitude.TabIndex = 15;
            this.labelLongitude.Text = "Longitude:";
            // 
            // textBoxLongitude
            // 
            this.textBoxLongitude.Location = new System.Drawing.Point(97, 248);
            this.textBoxLongitude.Name = "textBoxLongitude";
            this.textBoxLongitude.Size = new System.Drawing.Size(120, 20);
            this.textBoxLongitude.TabIndex = 16;
            this.textBoxLongitude.TextChanged += new System.EventHandler(this.OnCoordinateChanged);
            // 
            // labelZoom
            // 
            this.labelZoom.AutoSize = true;
            this.labelZoom.Location = new System.Drawing.Point(13, 283);
            this.labelZoom.Name = "labelZoom";
            this.labelZoom.Size = new System.Drawing.Size(67, 13);
            this.labelZoom.TabIndex = 17;
            this.labelZoom.Text = "Zoom Level:";
            // 
            // textBoxZoom
            // 
            this.textBoxZoom.Location = new System.Drawing.Point(97, 280);
            this.textBoxZoom.Name = "textBoxZoom";
            this.textBoxZoom.Size = new System.Drawing.Size(120, 20);
            this.textBoxZoom.TabIndex = 18;
            this.textBoxZoom.TextChanged += new System.EventHandler(this.OnCoordinateChanged);
            // 
            // labelMapProvider
            // 
            this.labelMapProvider.AutoSize = true;
            this.labelMapProvider.Location = new System.Drawing.Point(13, 315);
            this.labelMapProvider.Name = "labelMapProvider";
            this.labelMapProvider.Size = new System.Drawing.Size(74, 13);
            this.labelMapProvider.TabIndex = 19;
            this.labelMapProvider.Text = "Map Provider:";
            // 
            // comboBoxMapProvider
            // 
            this.comboBoxMapProvider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMapProvider.FormattingEnabled = true;
            this.comboBoxMapProvider.Items.AddRange(new object[] {
            "Leaflet (OpenStreetMap)",
            "Mapbox (Satellite)"});
            this.comboBoxMapProvider.Location = new System.Drawing.Point(97, 312);
            this.comboBoxMapProvider.Name = "comboBoxMapProvider";
            this.comboBoxMapProvider.Size = new System.Drawing.Size(266, 21);
            this.comboBoxMapProvider.TabIndex = 20;
            this.comboBoxMapProvider.SelectedIndexChanged += new System.EventHandler(this.OnUserChange);
            // 
            // labelMapboxToken
            // 
            this.labelMapboxToken.AutoSize = true;
            this.labelMapboxToken.Location = new System.Drawing.Point(13, 347);
            this.labelMapboxToken.Name = "labelMapboxToken";
            this.labelMapboxToken.Size = new System.Drawing.Size(81, 13);
            this.labelMapboxToken.TabIndex = 21;
            this.labelMapboxToken.Text = "Mapbox Token:";
            // 
            // textBoxMapboxToken
            // 
            this.textBoxMapboxToken.Location = new System.Drawing.Point(97, 344);
            this.textBoxMapboxToken.Name = "textBoxMapboxToken";
            this.textBoxMapboxToken.Size = new System.Drawing.Size(266, 20);
            this.textBoxMapboxToken.TabIndex = 22;
            this.textBoxMapboxToken.TextChanged += new System.EventHandler(this.OnUserChange);
            // 
            // linkLabelGetMapboxToken
            // 
            this.linkLabelGetMapboxToken.AutoSize = true;
            this.linkLabelGetMapboxToken.Location = new System.Drawing.Point(97, 367);
            this.linkLabelGetMapboxToken.Name = "linkLabelGetMapboxToken";
            this.linkLabelGetMapboxToken.Size = new System.Drawing.Size(154, 13);
            this.linkLabelGetMapboxToken.TabIndex = 23;
            this.linkLabelGetMapboxToken.TabStop = true;
            this.linkLabelGetMapboxToken.Text = "Get free Mapbox token (50k/mo)";
            this.linkLabelGetMapboxToken.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelGetMapboxToken_LinkClicked);
            // 
            // checkBoxEnableMapCaching
            // 
            this.checkBoxEnableMapCaching.AutoSize = true;
            this.checkBoxEnableMapCaching.Checked = true;
            this.checkBoxEnableMapCaching.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxEnableMapCaching.Location = new System.Drawing.Point(97, 390);
            this.checkBoxEnableMapCaching.Name = "checkBoxEnableMapCaching";
            this.checkBoxEnableMapCaching.Size = new System.Drawing.Size(178, 17);
            this.checkBoxEnableMapCaching.TabIndex = 24;
            this.checkBoxEnableMapCaching.Text = "Enable offline map tile caching";
            this.checkBoxEnableMapCaching.UseVisualStyleBackColor = true;
            this.checkBoxEnableMapCaching.CheckedChanged += new System.EventHandler(this.OnUserChange);
            // 
            // labelPollingInterval
            // 
            this.labelPollingInterval.AutoSize = true;
            this.labelPollingInterval.Location = new System.Drawing.Point(13, 420);
            this.labelPollingInterval.Name = "labelPollingInterval";
            this.labelPollingInterval.Size = new System.Drawing.Size(134, 13);
            this.labelPollingInterval.TabIndex = 26;
            this.labelPollingInterval.Text = "Polling Interval (seconds):";
            // 
            // numericUpDownPollingInterval
            // 
            this.numericUpDownPollingInterval.DecimalPlaces = 1;
            this.numericUpDownPollingInterval.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numericUpDownPollingInterval.Location = new System.Drawing.Point(153, 418);
            this.numericUpDownPollingInterval.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numericUpDownPollingInterval.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numericUpDownPollingInterval.Name = "numericUpDownPollingInterval";
            this.numericUpDownPollingInterval.Size = new System.Drawing.Size(80, 20);
            this.numericUpDownPollingInterval.TabIndex = 27;
            this.numericUpDownPollingInterval.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownPollingInterval.ValueChanged += new System.EventHandler(this.OnUserChange);
            // 
            // buttonTestConnection
            // 
            this.buttonTestConnection.Location = new System.Drawing.Point(97, 450);
            this.buttonTestConnection.Name = "buttonTestConnection";
            this.buttonTestConnection.Size = new System.Drawing.Size(150, 23);
            this.buttonTestConnection.TabIndex = 28;
            this.buttonTestConnection.Text = "Test connection";
            this.buttonTestConnection.UseVisualStyleBackColor = true;
            this.buttonTestConnection.Click += new System.EventHandler(this.buttonTestConnection_Click);
            // 
            // labelRegions
            // 
            this.labelRegions.AutoSize = true;
            this.labelRegions.Location = new System.Drawing.Point(13, 485);
            this.labelRegions.Name = "labelRegions";
            this.labelRegions.Size = new System.Drawing.Size(270, 13);
            this.labelRegions.TabIndex = 29;
            this.labelRegions.Text = "Regions to Load (leave empty to load all):";
            // 
            // checkedListBoxRegions
            // 
            this.checkedListBoxRegions.CheckOnClick = true;
            this.checkedListBoxRegions.FormattingEnabled = true;
            this.checkedListBoxRegions.Location = new System.Drawing.Point(16, 505);
            this.checkedListBoxRegions.Name = "checkedListBoxRegions";
            this.checkedListBoxRegions.Size = new System.Drawing.Size(347, 94);
            this.checkedListBoxRegions.TabIndex = 30;
            this.checkedListBoxRegions.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBoxRegions_ItemCheck);
            // 
            // buttonRefreshRegions
            // 
            this.buttonRefreshRegions.Location = new System.Drawing.Point(289, 480);
            this.buttonRefreshRegions.Name = "buttonRefreshRegions";
            this.buttonRefreshRegions.Size = new System.Drawing.Size(74, 23);
            this.buttonRefreshRegions.TabIndex = 31;
            this.buttonRefreshRegions.Text = "Refresh";
            this.buttonRefreshRegions.UseVisualStyleBackColor = true;
            this.buttonRefreshRegions.Click += new System.EventHandler(this.buttonRefreshRegions_Click);
            // 
            // labelSitePreview
            // 
            this.labelSitePreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSitePreview.AutoSize = true;
            this.labelSitePreview.Location = new System.Drawing.Point(399, 17);
            this.labelSitePreview.Name = "labelSitePreview";
            this.labelSitePreview.Size = new System.Drawing.Size(110, 13);
            this.labelSitePreview.TabIndex = 20;
            this.labelSitePreview.Text = "Site preview (Leaflet):";
            // 
            // webViewSitePreview
            // 
            this.webViewSitePreview.AllowExternalDrop = false;
            this.webViewSitePreview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.webViewSitePreview.CreationProperties = null;
            this.webViewSitePreview.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webViewSitePreview.Location = new System.Drawing.Point(402, 40);
            this.webViewSitePreview.MinimumSize = new System.Drawing.Size(280, 200);
            this.webViewSitePreview.Name = "webViewSitePreview";
            this.webViewSitePreview.Size = new System.Drawing.Size(300, 301);
            this.webViewSitePreview.TabIndex = 21;
            this.webViewSitePreview.ZoomFactor = 1D;
            // 
            // CoreCommandMIPUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Controls.Add(this.webViewSitePreview);
            this.Controls.Add(this.labelSitePreview);
            this.Controls.Add(this.buttonRefreshRegions);
            this.Controls.Add(this.checkedListBoxRegions);
            this.Controls.Add(this.labelRegions);
            this.Controls.Add(this.buttonTestConnection);
            this.Controls.Add(this.numericUpDownPollingInterval);
            this.Controls.Add(this.labelPollingInterval);
            this.Controls.Add(this.checkBoxEnableMapCaching);
            this.Controls.Add(this.linkLabelGetMapboxToken);
            this.Controls.Add(this.textBoxMapboxToken);
            this.Controls.Add(this.labelMapboxToken);
            this.Controls.Add(this.comboBoxMapProvider);
            this.Controls.Add(this.labelMapProvider);
            this.Controls.Add(this.textBoxZoom);
            this.Controls.Add(this.labelZoom);
            this.Controls.Add(this.textBoxLongitude);
            this.Controls.Add(this.labelLongitude);
            this.Controls.Add(this.textBoxLatitude);
            this.Controls.Add(this.labelLatitude);
            this.Controls.Add(this.textBoxApiKey);
            this.Controls.Add(this.labelApiKey);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.textBoxUsername);
            this.Controls.Add(this.labelUsername);
            this.Controls.Add(this.checkBoxUseHttps);
            this.Controls.Add(this.numericUpDownPort);
            this.Controls.Add(this.labelPort);
            this.Controls.Add(this.textBoxServerAddress);
            this.Controls.Add(this.labelServer);
            this.Controls.Add(this.textBoxName);
            this.Controls.Add(this.label1);
            this.Name = "CoreCommandMIPUserControl";
            this.Size = new System.Drawing.Size(718, 534);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPollingInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.webViewSitePreview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion




        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.Label labelServer;
        private System.Windows.Forms.TextBox textBoxServerAddress;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.NumericUpDown numericUpDownPort;
        private System.Windows.Forms.CheckBox checkBoxUseHttps;
        private System.Windows.Forms.Label labelUsername;
        private System.Windows.Forms.TextBox textBoxUsername;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Label labelApiKey;
        private System.Windows.Forms.TextBox textBoxApiKey;
        private System.Windows.Forms.Label labelLatitude;
        private System.Windows.Forms.TextBox textBoxLatitude;
        private System.Windows.Forms.Label labelLongitude;
        private System.Windows.Forms.TextBox textBoxLongitude;
        private System.Windows.Forms.Label labelZoom;
        private System.Windows.Forms.TextBox textBoxZoom;
        private System.Windows.Forms.Label labelMapProvider;
        private System.Windows.Forms.ComboBox comboBoxMapProvider;
        private System.Windows.Forms.Label labelMapboxToken;
        private System.Windows.Forms.TextBox textBoxMapboxToken;
        private System.Windows.Forms.LinkLabel linkLabelGetMapboxToken;
        private System.Windows.Forms.CheckBox checkBoxEnableMapCaching;
        private System.Windows.Forms.Label labelPollingInterval;
        private System.Windows.Forms.NumericUpDown numericUpDownPollingInterval;
        private System.Windows.Forms.Button buttonTestConnection;
        private System.Windows.Forms.Label labelRegions;
        private System.Windows.Forms.CheckedListBox checkedListBoxRegions;
        private System.Windows.Forms.Button buttonRefreshRegions;
        private System.Windows.Forms.Label labelSitePreview;
        private Microsoft.Web.WebView2.WinForms.WebView2 webViewSitePreview;
    }
}
