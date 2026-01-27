using System;
using System.Collections.Generic;
using VideoOS.Platform;
using VideoOS.Platform.Client;

namespace CoreCommandMIP.Client
{
    internal class CoreCommandMIPWorkSpaceToolbarPluginInstance : WorkSpaceToolbarPluginInstance
    {
        private Item _window;

        public CoreCommandMIPWorkSpaceToolbarPluginInstance()
        {
        }

        public override void Init(Item window)
        {
            _window = window;

            Title = "CoreCommandMIP";
        }

        public override void Activate()
        {
            // Here you should put whatever action that should be executed when the toolbar button is pressed
        }

        public override void Close()
        {
        }

    }

    internal class CoreCommandMIPWorkSpaceToolbarPlugin : WorkSpaceToolbarPlugin
    {
        public CoreCommandMIPWorkSpaceToolbarPlugin()
        {
        }

        public override Guid Id
        {
            get { return CoreCommandMIPDefinition.CoreCommandMIPWorkSpaceToolbarPluginId; }
        }

        public override string Name
        {
            get { return "CoreCommandMIP"; }
        }

        public override void Init()
        {
            // TODO: remove below check when CoreCommandMIPDefinition.CoreCommandMIPWorkSpaceToolbarPluginId has been replaced with proper GUID
            if (Id == new Guid("22222222-2222-2222-2222-222222222222"))
            {
                System.Windows.MessageBox.Show("Default GUID has not been replaced for CoreCommandMIPWorkSpaceToolbarPluginId!");
            }

            WorkSpaceToolbarPlaceDefinition.WorkSpaceIds = new List<Guid>() { ClientControl.LiveBuildInWorkSpaceId, ClientControl.PlaybackBuildInWorkSpaceId, CoreCommandMIPDefinition.CoreCommandMIPWorkSpacePluginId };
            WorkSpaceToolbarPlaceDefinition.WorkSpaceStates = new List<WorkSpaceState>() { WorkSpaceState.Normal };
        }

        public override void Close()
        {
        }

        public override WorkSpaceToolbarPluginInstance GenerateWorkSpaceToolbarPluginInstance()
        {
            return new CoreCommandMIPWorkSpaceToolbarPluginInstance();
        }
    }
}
