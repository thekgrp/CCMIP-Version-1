using VideoOS.Platform.Client;

namespace CoreCommandMIP.Client
{
    public class CoreCommandMIPWorkSpaceViewItemManager : CoreCommandMIPViewItemManager
    {
        public CoreCommandMIPWorkSpaceViewItemManager()
        {
        }

        public override ViewItemWpfUserControl GenerateViewItemWpfUserControl()
        {
            return new CoreCommandMIPWorkSpaceViewItemWpfUserControl(this);
        }
    }
}
