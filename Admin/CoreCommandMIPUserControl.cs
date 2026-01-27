using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreCommandMIP;
using CoreCommandMIP.Client;
using VideoOS.Platform;
using VideoOS.Platform.Admin;
using VideoOS.Platform.UI;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CoreCommandMIP.Admin
{
    /// <summary>
    /// This UserControl only contains a configuration of the Name for the Item.
    /// The methods and properties are used by the ItemManager, and can be changed as you see fit.
    /// </summary>
    public partial class CoreCommandMIPUserControl : UserControl
    {
        internal event EventHandler ConfigurationChangedByUser;

        private RemoteServerSettings _remoteSettings = new RemoteServerSettings();
        private bool _previewReady;
        private CoreWebView2Environment _previewEnvironment;
        private const string PreviewDocumentTemplate = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8'/>
<title>Site Preview</title>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
<style>html,body,#map{height:100%;margin:0;padding:0;background-color:#1b1b1b;color:#fafafa;font-family:'Segoe UI',sans-serif;} .leaflet-container{font-family:'Segoe UI',sans-serif;}</style>
</head>
<body>
<div id='map'></div>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<script>
var map = L.map('map',{zoomControl:true}).setView([__LAT__,__LON__],__ZOOM__);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{maxZoom:19,attribution:'&copy; OpenStreetMap contributors'}).addTo(map);
var marker = L.marker([__LAT__,__LON__]).addTo(map);
marker.bindPopup('Configured site').openPopup();
var radius = __RADIUS__;
if(radius>0){
    L.circle([__LAT__,__LON__],{radius:radius,color:'#1e88e5',fillColor:'#1e88e5',fillOpacity:0.1}).addTo(map);
}
</script>
</body>
</html>";


        public CoreCommandMIPUserControl()
        {
            InitializeComponent();
            InitializePreviewAsync();
        }

        internal String DisplayName
        {
            get { return textBoxName.Text; }
            set { textBoxName.Text = value; }
        }

        /// <summary>
        /// Ensure that all user entries will call this method to enable the Save button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void OnUserChange(object sender, EventArgs e)
        {
            if (ConfigurationChangedByUser != null)
                ConfigurationChangedByUser(this, new EventArgs());
        }

        internal void FillContent(Item item)
        {
            if (item == null)
            {
                _remoteSettings = new RemoteServerSettings();
                ClearContent();
                return;
            }

            _remoteSettings = RemoteServerSettings.FromItem(item);
            textBoxName.Text = item.Name;
            textBoxServerAddress.Text = _remoteSettings.Host;
            numericUpDownPort.Value = Math.Max(numericUpDownPort.Minimum, Math.Min(numericUpDownPort.Maximum, _remoteSettings.Port));
            checkBoxUseHttps.Checked = _remoteSettings.UseSsl;
            textBoxUsername.Text = _remoteSettings.DefaultUsername;
            textBoxPassword.Text = _remoteSettings.DefaultPassword;
            textBoxApiKey.Text = _remoteSettings.ApiKey;
            textBoxLatitude.Text = _remoteSettings.DefaultLatitude.ToString(CultureInfo.InvariantCulture);
            textBoxLongitude.Text = _remoteSettings.DefaultLongitude.ToString(CultureInfo.InvariantCulture);
            textBoxZoom.Text = _remoteSettings.DefaultZoomLevel.ToString(CultureInfo.InvariantCulture);
            
            // Map provider settings
            comboBoxMapProvider.SelectedIndex = (int)_remoteSettings.MapProvider;
            textBoxMapboxToken.Text = _remoteSettings.MapboxAccessToken ?? string.Empty;
            checkBoxEnableMapCaching.Checked = _remoteSettings.EnableMapCaching;
            
            // Polling interval
            numericUpDownPollingInterval.Value = (decimal)Math.Max(0, Math.Min(60, _remoteSettings.PollingIntervalSeconds));
            
            // Load regions list asynchronously
            LoadRegionsAsync();
            
            UpdateSitePreview(_remoteSettings);
        }

        internal void UpdateItem(Item item)
        {
            item.Name = DisplayName;
            _remoteSettings = CollectCurrentSettings();
            _remoteSettings.ApplyToItem(item);
        }

        internal void ClearContent()
        {
            textBoxName.Text = "";
            textBoxServerAddress.Text = string.Empty;
            numericUpDownPort.Value = 443;
            checkBoxUseHttps.Checked = true;
            textBoxUsername.Text = string.Empty;
            textBoxPassword.Text = string.Empty;
            textBoxApiKey.Text = string.Empty;
            textBoxLatitude.Text = "0";
            textBoxLongitude.Text = "0";
            textBoxZoom.Text = "8";
            comboBoxMapProvider.SelectedIndex = 0;
            textBoxMapboxToken.Text = string.Empty;
            checkBoxEnableMapCaching.Checked = true;
            numericUpDownPollingInterval.Value = 1;
            UpdateSitePreview(null);
        }

        internal bool TryValidate(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(textBoxServerAddress.Text))
            {
                errorMessage = "Remote server address is required.";
                textBoxServerAddress.Focus();
                return false;
            }

            if (!double.TryParse(textBoxLatitude.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                errorMessage = "Latitude must be a valid number.";
                textBoxLatitude.Focus();
                return false;
            }

            if (!double.TryParse(textBoxLongitude.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                errorMessage = "Longitude must be a valid number.";
                textBoxLongitude.Focus();
                return false;
            }

            if (!double.TryParse(textBoxZoom.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                errorMessage = "Zoom must be a valid number.";
                textBoxZoom.Focus();
                return false;
            }

            return true;
        }

        private bool TryValidateConnectionInputs(out string errorMessage)
        {
            if (!TryValidate(out errorMessage))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(textBoxUsername.Text))
            {
                errorMessage = "Username is required to test the connection.";
                textBoxUsername.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(textBoxPassword.Text))
            {
                errorMessage = "Password is required to test the connection.";
                textBoxPassword.Focus();
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        internal RemoteServerSettings GetRemoteSettings()
        {
            return _remoteSettings;
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
                SiteRadiusMeters = _remoteSettings?.SiteRadiusMeters ?? 0,
                PollingIntervalSeconds = (double)numericUpDownPollingInterval.Value,
                TailLength = _remoteSettings?.TailLength ?? 200,
                MapProvider = (MapProvider)(comboBoxMapProvider.SelectedIndex >= 0 ? comboBoxMapProvider.SelectedIndex : 0),
                MapboxAccessToken = textBoxMapboxToken.Text?.Trim() ?? string.Empty,
                EnableMapCaching = checkBoxEnableMapCaching.Checked,
                SelectedRegionIds = GetSelectedRegionIds()
            };

            return settings;
        }

        private static double ParseDoubleOrDefault(string value, double defaultValue)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultValue;
        }

        private async void buttonTestConnection_Click(object sender, EventArgs e)
        {
            if (!TryValidateConnectionInputs(out var error))
            {
                MessageBox.Show(error, "Remote server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var settings = CollectCurrentSettings();
            _remoteSettings = settings;

            try
            {
                using (new TestButtonScope(this))
                {
                    var provider = new RemoteServerDataProvider();
                    var siteInfo = await provider.FetchSiteConfigurationAsync(settings.GetBaseUrl(), settings.DefaultUsername, settings.DefaultPassword, CancellationToken.None).ConfigureAwait(true);
                    if (siteInfo == null)
                    {
                        MessageBox.Show("The remote server did not return site information.", "Remote server", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    settings.ApplySiteConfiguration(siteInfo.Latitude, siteInfo.Longitude, siteInfo.SiteRadius, siteInfo.Name, siteInfo.KeepAliveDurationSeconds, siteInfo.TailLength);
                    textBoxLatitude.Text = settings.DefaultLatitude.ToString(CultureInfo.InvariantCulture);
                    textBoxLongitude.Text = settings.DefaultLongitude.ToString(CultureInfo.InvariantCulture);
                    textBoxZoom.Text = settings.DefaultZoomLevel.ToString(CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(siteInfo.Name))
                    {
                        textBoxName.Text = siteInfo.Name;
                    }
                    _remoteSettings = settings;
                    UpdateSitePreview(settings);

                    MessageBox.Show("Connection successful and site information retrieved.", "Remote server", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Remote server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void InitializePreviewAsync()
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoreCommandMIP", "AdminPreview");
                Directory.CreateDirectory(folder);
                _previewEnvironment = await CoreWebView2Environment.CreateAsync(userDataFolder: folder).ConfigureAwait(true);
                await webViewSitePreview.EnsureCoreWebView2Async(_previewEnvironment).ConfigureAwait(true);
                _previewReady = true;
                UpdateSitePreview(_remoteSettings);
            }
            catch (Exception)
            {
                _previewReady = false;
            }
        }

        private void UpdateSitePreview(RemoteServerSettings settings)
        {
            if (!_previewReady || webViewSitePreview?.CoreWebView2 == null)
            {
                return;
            }

            var html = BuildPreviewDocument(settings);
            try
            {
                webViewSitePreview.NavigateToString(html);
            }
            catch
            {
                // Swallow navigation errors in admin preview
            }
        }

        private string BuildPreviewDocument(RemoteServerSettings settings)
        {
            var latitude = (settings?.DefaultLatitude ?? 0d).ToString("F6", CultureInfo.InvariantCulture);
            var longitude = (settings?.DefaultLongitude ?? 0d).ToString("F6", CultureInfo.InvariantCulture);
            var zoom = (settings?.DefaultZoomLevel ?? 8d).ToString("F1", CultureInfo.InvariantCulture);
            var radius = Math.Max(0, settings?.SiteRadiusMeters ?? 0d).ToString("F2", CultureInfo.InvariantCulture);

            return PreviewDocumentTemplate
                .Replace("__LAT__", latitude)
                .Replace("__LON__", longitude)
                .Replace("__ZOOM__", zoom)
                .Replace("__RADIUS__", radius);
        }

        private void OnCoordinateChanged(object sender, EventArgs e)
        {
            OnUserChange(sender, e);
            UpdateSitePreview(CollectCurrentSettings());
        }

        private sealed class TestButtonScope : IDisposable
        {
            private readonly CoreCommandMIPUserControl _owner;
            private readonly string _previousText;

            internal TestButtonScope(CoreCommandMIPUserControl owner)
            {
                _owner = owner;
                _previousText = owner.buttonTestConnection.Text;
                owner.buttonTestConnection.Enabled = false;
                owner.buttonTestConnection.Text = "Testing...";
                owner.Cursor = Cursors.WaitCursor;
            }

            public void Dispose()
            {
                _owner.buttonTestConnection.Enabled = true;
                _owner.buttonTestConnection.Text = _previousText;
                _owner.Cursor = Cursors.Default;
            }
        }

        private void linkLabelGetMapboxToken_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://account.mapbox.com/access-tokens/");
            }
            catch
            {
                MessageBox.Show(
                    "Please visit https://account.mapbox.com/access-tokens/ to get your free Mapbox access token.\n\n" +
                    "Free tier includes 50,000 map loads per month.",
                    "Get Mapbox Token",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private async void LoadRegionsAsync()
        {
            checkedListBoxRegions.Items.Clear();
            
            if (_remoteSettings == null)
            {
                MessageBox.Show("Settings not loaded yet.", "Region Load", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            if (!_remoteSettings.IsConfigured())
            {
                MessageBox.Show("Server not configured. Please enter server address, username, and password first.", "Region Load", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                buttonRefreshRegions.Enabled = false;
                buttonRefreshRegions.Text = "Loading...";
                
                var provider = new Client.RemoteServerDataProvider();
                var baseUrl = _remoteSettings.GetBaseUrl();
                
                System.Diagnostics.Debug.WriteLine($"Loading regions from: {baseUrl}");
                
                var regionList = await provider.FetchRegionListAsync(baseUrl, _remoteSettings.DefaultUsername, _remoteSettings.DefaultPassword, CancellationToken.None);
                
                System.Diagnostics.Debug.WriteLine($"Received {regionList?.Count ?? 0} regions");
                
                // Show raw response for debugging (temporary)
                var debugInfo = $"Region List Debug:\n\n";
                debugInfo += $"Base URL: {baseUrl}/rest/regions/list\n";
                debugInfo += $"Regions Count: {regionList?.Count ?? 0}\n\n";
                
                if (regionList != null)
                {
                    foreach (var r in regionList)
                    {
                        var typeLabel = r.Exclusion ? "Exclusion" : "Alarm";
                        debugInfo += $"ID: {r.Id}, Name: {r.Name}, Type: {typeLabel}, Active: {r.Active}\n";
                    }
                }
                
                MessageBox.Show(debugInfo, "Region Load Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                if (regionList == null || regionList.Count == 0)
                {
                    MessageBox.Show("No regions found on server.\n\nCheck Debug Output window for server response details.", "Region Load", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Parse saved selection
                var selectedIds = ParseSelectedRegionIds(_remoteSettings.SelectedRegionIds);
                
                foreach (var region in regionList)
                {
                    // Check if this region is selected (by GUID or numeric ID)
                    var isChecked = selectedIds.Contains(region.GuidId ?? string.Empty) || 
                                   selectedIds.Contains(region.Id.ToString(CultureInfo.InvariantCulture));
                    
                    checkedListBoxRegions.Items.Add(region, isChecked);
                    System.Diagnostics.Debug.WriteLine($"Added region: {region}, Checked: {isChecked}, GUID: {region.GuidId}, Exclusion: {region.Exclusion}");
                }
                
                MessageBox.Show($"Loaded {regionList.Count} region(s) successfully.", "Region Load", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load regions:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Region Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Region load exception: {ex}");
            }
            finally
            {
                buttonRefreshRegions.Enabled = true;
                buttonRefreshRegions.Text = "Refresh";
            }
        }

        private void buttonRefreshRegions_Click(object sender, EventArgs e)
        {
            LoadRegionsAsync();
        }

        private void checkedListBoxRegions_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Trigger save on check change
            BeginInvoke(new Action(() => OnUserChange(sender, e)));
        }

        private string GetSelectedRegionIds()
        {
            var selectedIds = new List<string>();
            foreach (var item in checkedListBoxRegions.CheckedItems)
            {
                if (item is Client.RegionListItem regionItem)
                {
                    // Use GUID if available, otherwise use numeric ID
                    var identifier = !string.IsNullOrEmpty(regionItem.GuidId) 
                        ? regionItem.GuidId 
                        : regionItem.Id.ToString(CultureInfo.InvariantCulture);
                    selectedIds.Add(identifier);
                }
            }
            return string.Join(",", selectedIds);
        }

        private static HashSet<string> ParseSelectedRegionIds(string selectedRegionIds)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(selectedRegionIds))
            {
                return result;
            }

            var parts = selectedRegionIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.Add(trimmed);
                }
            }
            return result;
        }

    }
}


