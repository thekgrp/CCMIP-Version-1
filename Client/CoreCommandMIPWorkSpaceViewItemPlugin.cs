using System;
using VideoOS.Platform.Client;
using VideoOS.Platform.UI.Controls;

namespace CoreCommandMIP.Client
{
    public class CoreCommandMIPWorkSpaceViewItemPlugin : ViewItemPlugin
    {
        public CoreCommandMIPWorkSpaceViewItemPlugin()
        {
        }

        public override Guid Id
        {
            get { return CoreCommandMIPDefinition.CoreCommandMIPWorkSpaceViewItemPluginId; }
        }

        public override VideoOSIconSourceBase IconSource { get => CoreCommandMIPDefinition.PluginIcon; protected set => base.IconSource = value; }

        public override string Name
        {
            get { return "WorkSpace Plugin View Item"; }
        }

        public override bool HideSetupItem
        {
            get
            {
                return false;
            }
        }

        public override ViewItemManager GenerateViewItemManager()
        {
            return new CoreCommandMIPWorkSpaceViewItemManager();
        }

        public override void Init()
        {
        }

        public override void Close()
        {
        }


    }
}
