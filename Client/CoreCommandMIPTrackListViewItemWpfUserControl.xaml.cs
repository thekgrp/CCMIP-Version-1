using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using VideoOS.Platform;
using VideoOS.Platform.Client;
using VideoOS.Platform.Messaging;

namespace CoreCommandMIP.Client
{
    public partial class CoreCommandMIPTrackListViewItemWpfUserControl : ViewItemWpfUserControl
    {
        private readonly CoreCommandMIPTrackListViewItemManager _viewItemManager;
        private readonly ObservableCollection<SmartMapLocation> _tracks = new ObservableCollection<SmartMapLocation>();
        private object _trackListReceiver;

        public CoreCommandMIPTrackListViewItemWpfUserControl(CoreCommandMIPTrackListViewItemManager manager)
        {
            _viewItemManager = manager ?? throw new ArgumentNullException(nameof(manager));
            InitializeComponent();
            _trackList.ItemsSource = _tracks;
        }

        public override void Init()
        {
            _trackListReceiver = EnvironmentManager.Instance.RegisterReceiver(new MessageReceiver(TrackListReceived),
                new MessageIdFilter(CoreCommandMIPDefinition.TrackListUpdatedMessageId));
        }

        public override void Close()
        {
            if (_trackListReceiver != null)
            {
                EnvironmentManager.Instance.UnRegisterReceiver(_trackListReceiver);
                _trackListReceiver = null;
            }
        }

	private object TrackListReceived(Message message, FQID destination, FQID source)
	{
		if (message?.Data is TrackListMessage payload)
		{
			// Accept track list from any site - don't filter by ConfigurationId
			// This allows the track list to update when the map view changes sites
			Dispatcher.Invoke(() => 
			{
				UpdateTrackList(payload.Tracks);
				UpdateSiteName(payload.ConfigurationId);
				// Also update our manager's SomeId to stay in sync
				if (payload.ConfigurationId != _viewItemManager.SomeId)
				{
					_viewItemManager.SomeId = payload.ConfigurationId;
				}
			});
		}
		return null;
	}

	private void UpdateSiteName(Guid configurationId)
	{
		try
		{
			var configItems = _viewItemManager.ConfigItems;
			if (configItems != null && configItems.Count > 0)
			{
				var currentSite = configItems.FirstOrDefault(item => item.FQID.ObjectId == configurationId);
				if (currentSite != null)
				{
					_siteNameTextBlock.Text = $"Site: {currentSite.Name}";
					return;
				}
			}
			_siteNameTextBlock.Text = "Site: Unknown";
		}
		catch
		{
			_siteNameTextBlock.Text = "Site: Unknown";
		}
	}

        private void UpdateTrackList(IReadOnlyList<SmartMapLocation> tracks)
        {
            _tracks.Clear();
            if (tracks == null)
            {
                _summaryTextBlock.Text = "No tracks available.";
                return;
            }

            foreach (var track in tracks)
            {
                _tracks.Add(track);
            }

            _summaryTextBlock.Text = _tracks.Count == 0 ? "No tracks available." : $"Active targets: {_tracks.Count}";
        }

        private void OnTrackSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_trackList.SelectedItem is SmartMapLocation track)
            {
                var message = new TrackSelectionMessage(_viewItemManager.SomeId, track);
                EnvironmentManager.Instance.PostMessage(new Message(CoreCommandMIPDefinition.TrackSelectedMessageId, message), null, null);
            }
        }
    }
}
