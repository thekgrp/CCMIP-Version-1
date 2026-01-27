using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;
using CoreCommandMIP;

namespace CoreCommandMIP.Client
{
    /// <summary>
    /// The ViewItemWpfUserControl is the WPF version of the ViewItemUserControl. It is instantiated for every position it is created on the current visible view. When a user select another View or ViewLayout, this class will be disposed.  No permanent settings can be saved in this class.
    /// The Init() method is called when the class is initiated and handle has been created for the UserControl. Please perform resource initialization in this method.
    /// <br>
    /// If Message communication is performed, register the MessageReceivers during the Init() method and UnRegister the receivers during the Close() method.
    /// <br>
    /// The Close() method can be used to Dispose resources in a controlled manor.
    /// <br>
    /// Mouse events not used by this control, should be passed on to the Smart Client by issuing the following methods:<br>
    /// FireClickEvent() for single click<br>
    ///	FireDoubleClickEvent() for double click<br>
    /// The single click will be interpreted by the Smart Client as a selection of the item, and the double click will be interpreted to expand the current viewitem to fill the entire View.
    /// </summary>
    public partial class CoreCommandMIPViewItemWpfUserControl : ViewItemWpfUserControl
    {
    #region Component private class variables

        private CoreCommandMIPViewItemManager _viewItemManager;
        private object _themeChangedReceiver;
        private SmartMapLocation _lastLocation;
        private Task _mapDocumentTask;
        private TaskCompletionSource<bool> _mapReadySource;
		private CancellationTokenSource _autoPollCancellation;
		private Task _autoPollTask;
		private IReadOnlyList<SmartMapLocation> _latestTracks = Array.Empty<SmartMapLocation>();
		private long? _selectedTrackId;
		private object _trackSelectionReceiver;
		private MetadataStore _metadataStore;
		private long? _lastTrackListCounter;
		private SmartMapLocation _pendingTrackSelection;
		private Guid? _pendingTrackSelectionSiteId;
		private bool _shouldApplyZoomOnNextUpdate;
		private double _userZoomLevel;
		private List<RegionDefinition> _siteRegions = new List<RegionDefinition>();
		private TrackAlarmManager _alarmManager;

        #endregion

        #region Component constructors + dispose

        /// <summary>
		/// Constructs a CoreCommandMIPViewItemUserControl instance
        /// </summary>
		public CoreCommandMIPViewItemWpfUserControl(CoreCommandMIPViewItemManager viewItemManager)
        {
            _viewItemManager = viewItemManager;

            InitializeComponent();

		if (_mapView != null)
		{
			PrepareWebViewHost();
			_mapView.NavigationCompleted += MapViewOnNavigationCompleted;
		}

            SetHeaderColors();
        }

        private static Color GetWindowsMediaColor(System.Drawing.Color inColor)
        {
            return Color.FromArgb(inColor.A, inColor.R, inColor.G, inColor.B);
        }

        private void SetHeaderColors()
        {
            _headerGrid.Background = new SolidColorBrush(GetWindowsMediaColor(ClientControl.Instance.Theme.BackgroundColor));
        }

		private void RefreshMetadataStore()
		{
			if (_viewItemManager == null || _viewItemManager.SomeId == Guid.Empty)
			{
				_metadataStore = null;
				return;
			}

		_metadataStore = new MetadataStore(_viewItemManager.SomeId);
	}

	private void InitializeAlarmManager()
	{
		try
		{
			var siteName = _viewItemManager.SomeName ?? "Unknown Site";
			_alarmManager = new TrackAlarmManager(CoreCommandMIPDefinition.CoreCommandMIPPluginId, siteName);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to initialize alarm manager: {ex.Message}");
			_alarmManager = null;
		}
	}

        private void SetUpApplicationEventListeners()
        {
            //set up ViewItem event listeners
            _viewItemManager.PropertyChangedEvent += new EventHandler(ViewItemManagerPropertyChangedEvent);
            _viewItemManager.ContextUpdated += ViewItemManagerContextUpdated;

            _themeChangedReceiver = EnvironmentManager.Instance.RegisterReceiver(new MessageReceiver(ThemeChangedIndicationHandler),
                                             new MessageIdFilter(MessageId.SmartClient.ThemeChangedIndication));

			_trackSelectionReceiver = EnvironmentManager.Instance.RegisterReceiver(new MessageReceiver(TrackSelectionIndicationHandler),
				new MessageIdFilter(CoreCommandMIPDefinition.TrackSelectedMessageId));

        }

        private void RemoveApplicationEventListeners()
        {
            //remove ViewItem event listeners
            _viewItemManager.PropertyChangedEvent -= new EventHandler(ViewItemManagerPropertyChangedEvent);
            _viewItemManager.ContextUpdated -= ViewItemManagerContextUpdated;

            EnvironmentManager.Instance.UnRegisterReceiver(_themeChangedReceiver);
            _themeChangedReceiver = null;
			if (_trackSelectionReceiver != null)
			{
				EnvironmentManager.Instance.UnRegisterReceiver(_trackSelectionReceiver);
				_trackSelectionReceiver = null;
			}
        }

        /// <summary>
        /// Method that is called immediately after the view item is displayed.
        /// </summary>
		public override void Init()
		{
			SetUpApplicationEventListeners();
			_nameTextBlock.Text = _viewItemManager.SomeName;
			PopulateSiteSelector();
			RefreshMetadataStore();
			InitializeAlarmManager();
			UpdateRemoteSummary();
			ClearTrackVisuals();
			StartAutoPolling();
			ApplyPendingTrackSelectionIfNeeded();
		}

        /// <summary>
        /// Method that is called when the view item is closed. The view item should free all resources when the method is called.
        /// Is called when userControl is not displayed anymore. Either because of 
        /// user clicking on another View or Item has been removed from View.
        /// </summary>
        public override void Close()
        {
            RemoveApplicationEventListeners();
			StopAutoPolling();
			if (_mapView != null)
			{
				_mapView.NavigationCompleted -= MapViewOnNavigationCompleted;
			}
        }

        #endregion

        #region Print method

        /// <summary>
        /// Method that is called when print is activated while the content holder is selected.
        /// </summary>
        public override void Print()
        {
            Print("Name of this item", "Some extra information");
        }

        #endregion

        #region Component events

        private void ViewItemWpfUserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                FireClickEvent();
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                FireRightClickEvent(e);
            }
        }

        private void ViewItemWpfUserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                FireDoubleClickEvent();
            }
        }

        private async void TestRegionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _statusTextBlock.Text = "Testing region rendering...";
                
                // Test if JavaScript functions exist
                var testScript = @"
                    if (typeof window.clearRegions === 'function' && typeof window.addRegion === 'function') {
                        'OK';
                    } else {
                        'MISSING: clearRegions=' + (typeof window.clearRegions) + ', addRegion=' + (typeof window.addRegion);
                    }
                ";
                
                var result = await _mapView.ExecuteScriptAsync(testScript);
                System.Diagnostics.Debug.WriteLine($"JavaScript functions check: {result}");
                
                if (result != "\"OK\"")
                {
                    _statusTextBlock.Text = $"JavaScript error: {result}";
                    return;
                }
                
                // Add test region
                var testRegionScript = @"
                    window.clearRegions();
                    window.addRegion({
                        name: 'Test Region',
                        vertices: [
                            {lat: 38.7866, lng: -104.7886},
                            {lat: 38.7900, lng: -104.7886},
                            {lat: 38.7900, lng: -104.7850},
                            {lat: 38.7866, lng: -104.7850}
                        ],
                        color: '#00ff00',
                        fill: 0.3,
                        exclusion: false
                    });
                    'Region added';
                ";
                
                result = await _mapView.ExecuteScriptAsync(testRegionScript);
                _statusTextBlock.Text = $"Test region result: {result}";
                System.Diagnostics.Debug.WriteLine($"Test region result: {result}");
            }
            catch (Exception ex)
            {
                _statusTextBlock.Text = $"Test failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Test region exception: {ex}");
            }
        }

        private async void DebugConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open browser DevTools
                if (_mapView?.CoreWebView2 != null)
                {
                    _mapView.CoreWebView2.OpenDevToolsWindow();
                    _statusTextBlock.Text = "DevTools opened - check Console tab for JavaScript errors";
                }
            }
            catch (Exception ex)
            {
                _statusTextBlock.Text = $"Failed to open DevTools: {ex.Message}";
            }
        }

        /// <summary>
        /// Signals that the form is right clicked
        /// </summary>
        public event EventHandler RightClickEvent;

        /// <summary>
        /// Activates the RightClickEvent
        /// </summary>
        /// <param name="e">Event args</param>
        protected virtual void FireRightClickEvent(EventArgs e)
        {
            if (RightClickEvent != null)
            {
                RightClickEvent(this, e);
            }
        }

        void ViewItemManagerPropertyChangedEvent(object sender, EventArgs e)
        {
            _nameTextBlock.Text = _viewItemManager.SomeName;
			PopulateSiteSelector();
			RefreshMetadataStore();
			UpdateRemoteSummary();
			InvalidateMapDocument();
			ClearTrackVisuals();
			StartAutoPolling();
			ApplyPendingTrackSelectionIfNeeded();
        }

        private void ViewItemManagerContextUpdated(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _nameTextBlock.Text = _viewItemManager.SomeName;
				PopulateSiteSelector();
				RefreshMetadataStore();
                _lastLocation = null;
                InvalidateMapDocument();
				UpdateRemoteSummary();
				ClearTrackVisuals();
				StartAutoPolling();
				ApplyPendingTrackSelectionIfNeeded();
            });
        }

        private object ThemeChangedIndicationHandler(VideoOS.Platform.Messaging.Message message, FQID destination, FQID source)
        {
            SetHeaderColors();
            return null;
        }

		private object TrackSelectionIndicationHandler(VideoOS.Platform.Messaging.Message message, FQID destination, FQID source)
		{
			if (message?.Data is TrackSelectionMessage selection && selection.Track != null)
			{
				Dispatcher.Invoke(() =>
				{
					if (selection.ConfigurationId != _viewItemManager.SomeId)
					{
						_pendingTrackSelection = selection.Track;
						_pendingTrackSelectionSiteId = selection.ConfigurationId;
						_viewItemManager.SomeId = selection.ConfigurationId;
						return;
					}

					_pendingTrackSelection = null;
					_pendingTrackSelectionSiteId = null;
					_selectedTrackId = selection.Track.TrackId;
					_lastLocation = selection.Track;
					_shouldApplyZoomOnNextUpdate = true;
					UpdateLocationDetails(selection.Track);
				});
			}
			return null;
		}

        #endregion

        #region Remote server interaction

		private void UpdateRemoteSummary()
        {
            var baseUrl = _viewItemManager.RemoteSettings?.GetBaseUrl();
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                _remoteSummaryTextBlock.Text = string.Format(CultureInfo.InvariantCulture, "Configured endpoint: {0}", baseUrl);
            }
            else
            {
                _remoteSummaryTextBlock.Text = "Remote server not configured";
            }
        }

	private void UpdateLocationDetails(SmartMapLocation location)
	{
		if (location == null)
		{
			_statusTextBlock.Text = "No remote payload fetched yet.";
			_ = UpdateMapAsync();
			return;
		}

		var trackIdLabel = location.TrackId == 0 ? "N/A" : location.TrackId.ToString(CultureInfo.InvariantCulture);
		var classification = string.IsNullOrWhiteSpace(location.ClassificationLabel) ? "Unknown" : location.ClassificationLabel;
		_statusTextBlock.Text = location.StatusMessage ?? string.Format(CultureInfo.InvariantCulture, "Track {0} - {1}", trackIdLabel, classification);

		_ = UpdateMapAsync();
	}

	private void ClearTrackVisuals(string statusMessage = "Waiting for remote targets.", bool broadcast = true)
	{
		_latestTracks = Array.Empty<SmartMapLocation>();
		_selectedTrackId = null;
		_lastLocation = null;
		_shouldApplyZoomOnNextUpdate = true;
		_siteRegions.Clear();
_alarmManager?.ClearAll();		
		if (_mapView?.CoreWebView2 != null)
		{
			try
			{
				_mapView.ExecuteScriptAsync("window.clearRegions && window.clearRegions(); window.clearAllTracks && window.clearAllTracks();");
			}
			catch
			{
			}
		}

		// Reload regions after clearing
		Task.Run(async () =>
		{
			await Task.Delay(500).ConfigureAwait(false);
			await LoadAndRenderRegionsAsync().ConfigureAwait(false);
		});

		
		UpdateLocationDetails(null);
		if (!string.IsNullOrWhiteSpace(statusMessage))
		{
			_statusTextBlock.Text = statusMessage;
		}
		if (broadcast)
		{
			BroadcastTrackList(_latestTracks);
		}
	}

		private void PersistMetadata(SmartMapLocation location)
		{
			if (location == null || location.TrackId == 0 || _metadataStore == null)
			{
				return;
			}

			try
			{
				_metadataStore.TryPersist(location, _viewItemManager.RemoteSettings, _viewItemManager.SomeName);
			}
			catch (Exception ex)
			{
				_statusTextBlock.Text = $"Unable to store metadata: {ex.Message}";
			}
		}

		#endregion

	private void StartAutoPolling(bool forceRestart = true)
	{
		if (!TryGetStoredCredentials(out var baseUrl, out var username, out var password))
		{
			if (forceRestart || _autoPollTask == null)
			{
				StopAutoPolling();
				_statusTextBlock.Text = "Remote server credentials are required before polling.";
			}
			return;
		}

		if (!forceRestart && _autoPollTask != null)
		{
			return;
		}

		StopAutoPolling();
		
		// Immediately broadcast empty track list when switching sites
		_latestTracks = Array.Empty<SmartMapLocation>();
		BroadcastTrackList(_latestTracks);
		
		_statusTextBlock.Text = "Polling remote server for track updates...";

		_lastTrackListCounter = null;
		_autoPollCancellation = new CancellationTokenSource();
		var token = _autoPollCancellation.Token;
		var settingsSnapshot = _viewItemManager.RemoteSettings ?? new RemoteServerSettings();
		_autoPollTask = Task.Run(() => AutoPollLoopAsync(baseUrl, username, password, settingsSnapshot, token));
	}

		private void StopAutoPolling()
		{
			if (_autoPollCancellation != null)
			{
				_autoPollCancellation.Cancel();
				_autoPollCancellation.Dispose();
				_autoPollCancellation = null;
			}
			_autoPollTask = null;
			_lastTrackListCounter = null;
		}

		private bool TryGetStoredCredentials(out string baseUrl, out string username, out string password)
		{
			baseUrl = _viewItemManager.RemoteSettings?.GetBaseUrl()?.Trim();
			username = _viewItemManager.RemoteSettings?.DefaultUsername?.Trim();
			password = _viewItemManager.RemoteSettings?.DefaultPassword ?? string.Empty;
			if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
			{
				baseUrl = username = password = string.Empty;
				return false;
			}
			return true;
		}

	private async Task AutoPollLoopAsync(string baseUrl, string username, string password, RemoteServerSettings defaults, CancellationToken token)
	{
		var provider = new RemoteServerDataProvider();
		var snapshot = defaults ?? new RemoteServerSettings();
		var pollIntervalSeconds = snapshot.PollingIntervalSeconds;
		if (double.IsNaN(pollIntervalSeconds) || pollIntervalSeconds <= 0)
		{
			pollIntervalSeconds = 1d;
		}
		var pollDelay = TimeSpan.FromSeconds(Math.Max(1d, pollIntervalSeconds));
		var lastCounter = _lastTrackListCounter;
		var isFirstPoll = true;
		
		while (!token.IsCancellationRequested)
		{
			try
			{
				var result = await provider.FetchChangedTracksAsync(baseUrl, username, password, snapshot, lastCounter, token).ConfigureAwait(false);
				
				if (result.HasChanges || isFirstPoll)
				{
					if (result.ChangeCounter.HasValue)
					{
						lastCounter = result.ChangeCounter;
					}
					Dispatcher.Invoke(() => HandleTrackUpdate(result.Tracks));
					isFirstPoll = false;
				}
				else if (result.ChangeCounter.HasValue)
				{
					lastCounter = result.ChangeCounter;
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				Dispatcher.Invoke(() => _statusTextBlock.Text = $"Polling failed: {ex.Message}");
			}

			try
			{
				await Task.Delay(pollDelay, token).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}

		_lastTrackListCounter = lastCounter;
	}

	private void HandleTrackUpdate(IReadOnlyList<SmartMapLocation> tracks)
	{
		var meaningfulTracks = (tracks ?? Array.Empty<SmartMapLocation>()).Where(t => t.TrackId != 0).ToList();
		if (meaningfulTracks.Count == 0)
		{
			_latestTracks = Array.Empty<SmartMapLocation>();
			_lastLocation = null;
			UpdateLocationDetails(null);
			_statusTextBlock.Text = tracks?.FirstOrDefault()?.StatusMessage ?? "No active tracks.";
			BroadcastTrackList(_latestTracks);
			return;
		}

		_latestTracks = meaningfulTracks;
		
		// Process alarms for all tracks
		_alarmManager?.ProcessTracks(meaningfulTracks);
		
		SmartMapLocation nextTrack = null;
		if (_selectedTrackId.HasValue)
		{
			nextTrack = _latestTracks.FirstOrDefault(t => t.TrackId == _selectedTrackId.Value);
		}
		if (nextTrack == null)
		{
			nextTrack = _latestTracks.FirstOrDefault();
			_selectedTrackId = nextTrack?.TrackId;
		}

		if (nextTrack != null)
		{
			_lastLocation = nextTrack;
			UpdateLocationDetails(nextTrack);
			PersistMetadata(nextTrack);
		}
		_statusTextBlock.Text = nextTrack?.StatusMessage ?? "Waiting for remote targets.";
		BroadcastTrackList(_latestTracks);
	}

		private void BroadcastTrackList(IReadOnlyList<SmartMapLocation> tracks)
		{
			var message = new TrackListMessage(_viewItemManager.SomeId, tracks);
			EnvironmentManager.Instance.PostMessage(new VideoOS.Platform.Messaging.Message(CoreCommandMIPDefinition.TrackListUpdatedMessageId, message), null, null);
		}

		private void ApplyPendingTrackSelectionIfNeeded()
		{
			if (_pendingTrackSelection == null || !_pendingTrackSelectionSiteId.HasValue)
			{
				return;
			}

			if (_pendingTrackSelectionSiteId.Value != _viewItemManager.SomeId)
			{
				return;
			}

			_selectedTrackId = _pendingTrackSelection.TrackId == 0 ? (long?)null : _pendingTrackSelection.TrackId;
			_lastLocation = _pendingTrackSelection;
			_shouldApplyZoomOnNextUpdate = true;
			UpdateLocationDetails(_pendingTrackSelection);
			PersistMetadata(_pendingTrackSelection);
			_pendingTrackSelection = null;
			_pendingTrackSelectionSiteId = null;
		}

		private void PopulateSiteSelector()
		{
			if (_siteComboBox == null)
			{
				return;
			}

			var configItems = _viewItemManager.ConfigItems;
			if (configItems == null || configItems.Count == 0)
			{
				_siteComboBox.ItemsSource = null;
				_siteComboBox.IsEnabled = false;
				ClearTrackVisuals("Remote server not configured.");
				return;
			}

			_siteComboBox.IsEnabled = true;
			var options = configItems.Select(item => new SiteOption(item)).ToList();
			_siteComboBox.ItemsSource = options;
			var current = options.FirstOrDefault(opt => opt.Item.FQID.ObjectId == _viewItemManager.SomeId);
			_siteComboBox.SelectedItem = current ?? options.FirstOrDefault();
		}

		private void OnSiteSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!(_siteComboBox?.SelectedItem is SiteOption option))
			{
				return;
			}

			if (option.Item.FQID.ObjectId == _viewItemManager.SomeId)
			{
				return;
			}

			_viewItemManager.SomeId = option.Item.FQID.ObjectId;
			RefreshMetadataStore();
			InvalidateMapDocument();
			UpdateRemoteSummary();
			ClearTrackVisuals();
			StartAutoPolling();
			ApplyPendingTrackSelectionIfNeeded();
		}

		private string _webViewUserDataFolder;

		private void PrepareWebViewHost()
		{
			try
			{
				_webViewUserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoreCommandMIP", "WebView2");
				Directory.CreateDirectory(_webViewUserDataFolder);
			}
			catch (Exception ex)
			{
				_statusTextBlock.Text = $"Unable to prepare map control: {ex.Message}";
			}
		}

		#region Map rendering

		private void InvalidateMapDocument()
		{
			_mapDocumentTask = null;
			_mapReadySource = null;
		}

		private Task InitializeMapDocumentAsync()
		{
			if (_mapView == null)
			{
				return Task.CompletedTask;
			}

			if (_mapDocumentTask == null)
			{
				_mapDocumentTask = LoadMapDocumentAsync();
			}

			return _mapDocumentTask;
		}

	private async Task LoadMapDocumentAsync()
	{
		try
		{
			CoreWebView2Environment environment = null;
			if (!string.IsNullOrWhiteSpace(_webViewUserDataFolder))
			{
				environment = await CoreWebView2Environment.CreateAsync(userDataFolder: _webViewUserDataFolder).ConfigureAwait(true);
			}
			await _mapView.EnsureCoreWebView2Async(environment).ConfigureAwait(true);
			
			// Enable dev tools for debugging
			_mapView.CoreWebView2.Settings.AreDevToolsEnabled = true;
			
			// Subscribe to console messages
			_mapView.CoreWebView2.WebMessageReceived += (s, e) =>
			{
				System.Diagnostics.Debug.WriteLine($"WebView message: {e.WebMessageAsJson}");
			};
			
		_mapReadySource = new TaskCompletionSource<bool>();
		var html = BuildMapDocument(_viewItemManager.RemoteSettings);
		
		// TEMPORARY DEBUG - Save HTML to file
		try
		{
			var debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "map-debug.html");
			File.WriteAllText(debugPath, html);
			System.Diagnostics.Debug.WriteLine($"===== Saved map HTML to: {debugPath} =====");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to save debug HTML: {ex.Message}");
		}
		
		_mapView.NavigateToString(html);
		await _mapReadySource.Task.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			Dispatcher.Invoke(() => _statusTextBlock.Text = $"Unable to initialize map: {ex.Message}");
			InvalidateMapDocument();
		}
	}

	private string BuildMapDocument(RemoteServerSettings settings)
	{
		var latitude = (settings?.DefaultLatitude ?? 0d).ToString("F6", CultureInfo.InvariantCulture);
		var longitude = (settings?.DefaultLongitude ?? 0d).ToString("F6", CultureInfo.InvariantCulture);
		var serverUrl = settings?.GetBaseUrl() ?? string.Empty;
		double zoomLevel = _userZoomLevel > 0 ? _userZoomLevel : (settings?.DefaultZoomLevel ?? 0d);
		if ((double.IsNaN(zoomLevel) || zoomLevel <= 0) && settings != null && settings.SiteRadiusMeters > 0)
		{
			zoomLevel = RemoteServerSettings.CalculateZoomFromRadius(settings.SiteRadiusMeters, settings.DefaultLatitude);
		}
		if (double.IsNaN(zoomLevel) || zoomLevel <= 0)
		{
			zoomLevel = 8d;
		}
		_userZoomLevel = zoomLevel;
		var zoom = zoomLevel.ToString("F1", CultureInfo.InvariantCulture);
        var tail = Math.Max(1, settings?.TailLength ?? 200d).ToString("F0", CultureInfo.InvariantCulture);

		// Choose map provider
		string html;
		var hasMapboxToken = !string.IsNullOrWhiteSpace(settings?.MapboxAccessToken);
		var mapProvider = settings?.MapProvider ?? MapProvider.Leaflet;
		
		// AUTO-DETECT: If Mapbox token exists but provider not set, use Mapbox
		if (hasMapboxToken && mapProvider == MapProvider.Leaflet)
		{
			System.Diagnostics.Debug.WriteLine("Auto-detected Mapbox token, switching to Mapbox provider");
			mapProvider = MapProvider.Mapbox;
		}
		
		if (mapProvider == MapProvider.Mapbox && hasMapboxToken)
		{
			System.Diagnostics.Debug.WriteLine($"Using Mapbox with token: {settings.MapboxAccessToken.Substring(0, Math.Min(20, settings.MapboxAccessToken.Length))}...");
			html = MapboxTemplate.GetMapHtml(settings.MapboxAccessToken);
		}
		else
		{
			System.Diagnostics.Debug.WriteLine($"Using Leaflet (MapProvider={mapProvider}, HasToken={hasMapboxToken})");
			html = MapTemplate.GetMapHtml();
		}

		return html
			.Replace("__LAT__", latitude)
			.Replace("__LON__", longitude)
			.Replace("__ZOOM__", zoom)
			.Replace("__TAIL__", tail)
			.Replace("__SERVER_URL__", serverUrl);
	}

	private async Task UpdateMapAsync()
	{
		try
		{
			await InitializeMapDocumentAsync().ConfigureAwait(true);
			if (_mapView?.CoreWebView2 == null)
			{
				return;
			}

			if (_latestTracks == null || _latestTracks.Count == 0)
			{
				await _mapView.ExecuteScriptAsync("window.clearAllTracks && window.clearAllTracks();").ConfigureAwait(true);
				return;
			}

			if (_shouldApplyZoomOnNextUpdate)
			{
				await _mapView.ExecuteScriptAsync("window.setApplyZoom && window.setApplyZoom(true);").ConfigureAwait(true);
				_shouldApplyZoomOnNextUpdate = false;
			}

			var script = BuildTracksScript(_latestTracks);
			await _mapView.ExecuteScriptAsync(script).ConfigureAwait(true);

			await CaptureUserZoomAsync().ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			Dispatcher.Invoke(() => _statusTextBlock.Text = $"Unable to update map: {ex.Message}");
		}
	}

		private async Task CaptureUserZoomAsync()
		{
			try
			{
				if (_mapView?.CoreWebView2 == null)
				{
					return;
				}

				var zoomResult = await _mapView.ExecuteScriptAsync("window.getCurrentZoom && window.getCurrentZoom();").ConfigureAwait(true);
				if (!string.IsNullOrWhiteSpace(zoomResult) && double.TryParse(zoomResult, NumberStyles.Float, CultureInfo.InvariantCulture, out var zoom))
				{
					_userZoomLevel = zoom;
				}
			}
			catch
			{
			}
		}

	private string BuildTracksScript(IReadOnlyList<SmartMapLocation> locations)
	{
		if (locations == null || locations.Count == 0)
		{
			return "window.clearAllTracks && window.clearAllTracks();";
		}

		var tracks = new List<string>();
		var activeIds = new List<string>();
		var tail = Math.Max(1, _viewItemManager.RemoteSettings?.TailLength ?? 200d);

		foreach (var location in locations)
		{
			if (location.TrackId == 0)
			{
				continue;
			}

			activeIds.Add(location.TrackId.ToString(CultureInfo.InvariantCulture));

			var trackIdLabel = location.TrackId.ToString(CultureInfo.InvariantCulture);
			var label = string.Format(CultureInfo.InvariantCulture, "ID {0}", trackIdLabel);
			var classification = string.IsNullOrWhiteSpace(location.ClassificationLabel) ? "Unknown" : location.ClassificationLabel;
			var sourcesText = location.Sources != null && location.Sources.Count > 0 
				? "Sources: " + string.Join(", ", location.Sources) 
				: string.Empty;
			var details = string.Format(CultureInfo.InvariantCulture,
				"Type: {5}<br/>Lat: {0:F4}&deg;<br/>Lon: {1:F4}&deg;<br/>Alt: {2:F1} m<br/>Vel: {3:F1} m/s<br/>Conf: {4:P0}{6}{7}",
				location.Latitude,
				location.Longitude,
				location.Altitude,
				location.Velocity,
				Math.Max(0, location.ClassificationConfidence),
				classification,
				string.IsNullOrWhiteSpace(location.Description) ? string.Empty : "<br/>" + location.Description,
				string.IsNullOrWhiteSpace(sourcesText) ? string.Empty : "<br/>" + sourcesText);

			var color = string.IsNullOrWhiteSpace(location.IconColorHex) ? "#1e88e5" : location.IconColorHex;

			var track = string.Format(CultureInfo.InvariantCulture,
				"{{id:{0},lat:{1},lng:{2},label:\"{3}\",details:\"{4}\",color:\"{5}\",tail:{6},classification:\"{7}\"}}",
				location.TrackId,
				location.Latitude,
				location.Longitude,
				EscapeForJson(label),
				EscapeForJson(details),
				EscapeForJson(color),
				tail.ToString("F0", CultureInfo.InvariantCulture),
				EscapeForJson(classification));


			tracks.Add(track);
		}

		var tracksJson = "[" + string.Join(",", tracks) + "]";
		var activeIdsJson = "[" + string.Join(",", activeIds) + "]";
		return string.Format(CultureInfo.InvariantCulture,
			"window.updateTracks && window.updateTracks({0}); window.clearInactiveTracks && window.clearInactiveTracks({1});",
			tracksJson, activeIdsJson);
	}

		private static string EscapeForJson(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

		return value
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"")
			.Replace("\r", "\\r")
			.Replace("\n", "\\n");
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

	private void MapViewOnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
	{
		_mapReadySource?.TrySetResult(true);
		
		System.Diagnostics.Debug.WriteLine("=== Map navigation completed ===");
		Task.Run(async () =>
		{
			await Task.Delay(500).ConfigureAwait(false);
			System.Diagnostics.Debug.WriteLine("=== Starting region load ===");
			await LoadAndRenderRegionsAsync().ConfigureAwait(false);
		});
	}

	private async Task LoadAndRenderRegionsAsync()
	{
		if (!TryGetStoredCredentials(out var baseUrl, out var username, out var password))
		{
			Dispatcher.Invoke(() => _statusTextBlock.Text = "No credentials for region loading");
			return;
		}

		try
		{
			Dispatcher.Invoke(() => _statusTextBlock.Text = "Loading regions from server...");
			
			var provider = new RemoteServerDataProvider();
			var regionList = await provider.FetchRegionListAsync(baseUrl, username, password, CancellationToken.None).ConfigureAwait(false);
			
			Dispatcher.Invoke(() => _statusTextBlock.Text = $"Found {regionList?.Count ?? 0} regions");
			
			_siteRegions.Clear();

			if (regionList == null || regionList.Count == 0)
			{
				Dispatcher.Invoke(() => _statusTextBlock.Text = "No regions returned from server");
				return;
			}

		// Get selected region IDs from settings (empty = load all)
		var selectedRegionIds = ParseSelectedRegionIds(_viewItemManager?.RemoteSettings?.SelectedRegionIds);
		var loadAllRegions = selectedRegionIds.Count == 0;

		foreach (var regionItem in regionList)
		{
			if (!regionItem.Active)
			{
				System.Diagnostics.Debug.WriteLine($"Skipping inactive region: {regionItem.Name}");
				continue;
			}

			// Filter by selected regions (if any are selected)
			// Check both numeric ID and GUID-based selection
			var isSelected = loadAllRegions || 
			                 selectedRegionIds.Contains(regionItem.GuidId ?? string.Empty) ||
			                 selectedRegionIds.Contains(regionItem.Id.ToString(CultureInfo.InvariantCulture));
			
			if (!isSelected)
			{
				System.Diagnostics.Debug.WriteLine($"Skipping unselected region: {regionItem.Name} (ID: {regionItem.Id}) (GUID: {regionItem.GuidId})");
				continue;
			}

			try
			{
			var regionIdOrGuid = string.IsNullOrEmpty(regionItem.GuidId) ? regionItem.Id.ToString(CultureInfo.InvariantCulture) : regionItem.GuidId;
			System.Diagnostics.Debug.WriteLine($"Fetching details for region: {regionItem.Name} ({regionIdOrGuid})");
			var regionDef = await provider.FetchRegionDetailsAsync(baseUrl, username, password, regionIdOrGuid, CancellationToken.None).ConfigureAwait(false);
				if (regionDef != null && regionDef.Active && regionDef.Vertices != null && regionDef.Vertices.Count >= 3)
				{
					_siteRegions.Add(regionDef);
					System.Diagnostics.Debug.WriteLine($"Added region '{regionDef.Name}' with {regionDef.Vertices.Count} vertices to render list");
					Dispatcher.Invoke(() => _statusTextBlock.Text = $"Loaded region '{regionDef.Name}' with {regionDef.Vertices.Count} vertices");
				}
				else
				{
					var reason = regionDef == null ? "null" : 
								!regionDef.Active ? "inactive" : 
								regionDef.Vertices == null ? "no vertices" : 
								"too few vertices";
					System.Diagnostics.Debug.WriteLine($"Skipped region {regionItem.Id}: {reason}");
					Dispatcher.Invoke(() => _statusTextBlock.Text = $"Skipped region {regionItem.Id}: {reason}");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error loading region {regionItem.Id}: {ex}");
				Dispatcher.Invoke(() => _statusTextBlock.Text = $"Error loading region {regionItem.Id}: {ex.Message}");
			}
		}

			if (_siteRegions.Count > 0)
			{
				Dispatcher.Invoke(() => _statusTextBlock.Text = $"Rendering {_siteRegions.Count} regions...");
				Dispatcher.Invoke(() => RenderRegions());
			}
			else
			{
				Dispatcher.Invoke(() => _statusTextBlock.Text = "No valid regions to render");
			}
		}
		catch (Exception ex)
		{
			Dispatcher.Invoke(() => _statusTextBlock.Text = $"Region load failed: {ex.Message}");
		}
	}

	private async void RenderRegions()
	{
		try
		{
			if (_mapView?.CoreWebView2 == null)
			{
				_statusTextBlock.Text = "Map not ready for region rendering";
				return;
			}

			_statusTextBlock.Text = $"Clearing old regions...";
			await _mapView.ExecuteScriptAsync("window.clearRegions && window.clearRegions();").ConfigureAwait(true);

			if (_siteRegions.Count == 0)
			{
				_statusTextBlock.Text = "No regions in memory to render";
				return;
			}

			_statusTextBlock.Text = $"Rendering {_siteRegions.Count} region(s)...";
			var regionCount = 0;

			foreach (var region in _siteRegions)
			{
				if (region.Vertices == null || region.Vertices.Count < 3)
				{
					_statusTextBlock.Text = $"Skipping region '{region.Name}' - insufficient vertices";
					continue;
				}

				var vertices = string.Join(",", region.Vertices.ConvertAll(v => 
					string.Format(CultureInfo.InvariantCulture, "{{lat:{0},lng:{1}}}", v.Latitude, v.Longitude)));

				var color = string.IsNullOrWhiteSpace(region.Color) ? "#ff6b6b" : region.Color;
				var fill = region.Fill >= 0 && region.Fill <= 1 ? region.Fill : 0.2;

				var regionScript = string.Format(CultureInfo.InvariantCulture,
					"window.addRegion && window.addRegion({{name:\"{0}\",vertices:[{1}],color:\"{2}\",fill:{3},exclusion:{4}}});",
					EscapeForJson(region.Name ?? "Region"),
					vertices,
					color,
					fill.ToString("F2", CultureInfo.InvariantCulture),
					region.Exclusion ? "true" : "false");

				_statusTextBlock.Text = $"Rendering region '{region.Name}' ({region.Vertices.Count} vertices)...";
				
				var scriptPreview = regionScript.Length > 200 ? regionScript.Substring(0, 200) + "..." : regionScript;
				System.Diagnostics.Debug.WriteLine($"Region script: {scriptPreview}");
				
				var result = await _mapView.ExecuteScriptAsync(regionScript).ConfigureAwait(true);
				regionCount++;
			}

			_statusTextBlock.Text = $"Successfully rendered {regionCount} region(s)";
		}
		catch (Exception ex)
		{
			_statusTextBlock.Text = $"Region render error: {ex.Message}";
			System.Diagnostics.Debug.WriteLine($"Region render exception: {ex}");
		}
	}

	#endregion

        #region Component properties

        /// <summary>
        /// Gets boolean indicating whether the view item can be maximized or not. <br/>
        /// The content holder should implement the click and double click events even if it is not maximizable. 
        /// </summary>
        public override bool Maximizable
        {
            get { return true; }
        }

        /// <summary>
        /// Tell if ViewItem is selectable
        /// </summary>
        public override bool Selectable
        {
            get { return true; }
        }

        /// <summary>
        /// Make support for Theme colors to show if this ViewItem is selected or not.
        /// </summary>
        public override bool Selected
        {
            get
            {
                return base.Selected;
            }
            set
            {
                base.Selected = value;
                SetHeaderColors();
            }
        }

        #endregion
    }

	internal sealed class SiteOption
	{
		internal SiteOption(Item item)
		{
			Item = item;
		}

		internal Item Item { get; }

		public override string ToString()
		{
			return Item?.Name ?? "Unknown";
		}
	}
}

