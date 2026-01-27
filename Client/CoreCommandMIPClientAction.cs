using System;
using System.Windows;
using System.Windows.Media.Imaging;
using VideoOS.Platform.Client;
using VideoOS.Platform.UI.Controls;

namespace CoreCommandMIP.Client
{
    public class CoreCommandMIPClientAction : ClientAction
    {
        public override Guid Id
        {
            get => CoreCommandMIPDefinition.CoreCommandMIPClientActionId;
        }

        public override string Name
        {
            get => "CoreCommandMIP Client Action"; //Note that the action name should be localized (unless it contains a name of an Item or similar).
        }

        public override VideoOSIconSourceBase Icon
        {
            get => CoreCommandMIPDefinition.PluginIcon;
        }

        public override void Init()
        {
        }

        public override void Close()
        {
        }

        public override void Activated()
        {
            MessageBox.Show("CoreCommandMIP Client Action activated.");
        }
    }
}