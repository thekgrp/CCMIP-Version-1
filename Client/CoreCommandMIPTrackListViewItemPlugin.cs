using System;
using VideoOS.Platform.Client;
using VideoOS.Platform.UI.Controls;

namespace CoreCommandMIP.Client
{
    public class CoreCommandMIPTrackListViewItemPlugin : ViewItemPlugin
    {
        public override Guid Id => CoreCommandMIPDefinition.CoreCommandMIPTrackListViewItemPluginId;

        public override VideoOSIconSourceBase IconSource
        {
            get => CoreCommandMIPDefinition.PluginIcon;
            protected set => base.IconSource = value;
        }

        public override string Name => "Track List View Item";

        public override ViewItemManager GenerateViewItemManager()
        {
            return new CoreCommandMIPTrackListViewItemManager();
        }

        public override void Init()
        {
        }

        public override void Close()
        {
        }
    }
}
