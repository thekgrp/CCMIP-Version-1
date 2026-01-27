using VideoOS.Platform.Client;

namespace CoreCommandMIP.Client
{
    public class CoreCommandMIPTrackListViewItemManager : CoreCommandMIPViewItemManager
    {
        public override ViewItemWpfUserControl GenerateViewItemWpfUserControl()
        {
            return new CoreCommandMIPTrackListViewItemWpfUserControl(this);
        }
    }
}
