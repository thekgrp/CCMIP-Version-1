using CoreCommandMIP.Admin;
using CoreCommandMIP.Background;
using CoreCommandMIP.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using VideoOS.Platform;
using VideoOS.Platform.Admin;
using VideoOS.Platform.Background;
using VideoOS.Platform.Client;
using VideoOS.Platform.UI.Controls;

namespace CoreCommandMIP
{
    /// <summary>
    /// The PluginDefinition is the ‘entry’ point to any plugin.  
    /// This is the starting point for any plugin development and the class MUST be available for a plugin to be loaded.  
    /// Several PluginDefinitions are allowed to be available within one DLL.
    /// Here the references to all other plugin known objects and classes are defined.
    /// The class is an abstract class where all implemented methods and properties need to be declared with override.
    /// The class is constructed when the environment is loading the DLL.
    /// </summary>
    public class CoreCommandMIPDefinition : PluginDefinition
    {
        private static readonly Image _treeNodeImage;
        private static readonly VideoOSIconSourceBase _pluginIcon;
        private static readonly Image _topTreeNodeImage;

        internal static readonly Uri DummyImagePackUri;

        internal static Guid CoreCommandMIPPluginId = new Guid("bedbf651-31ef-4c8f-b713-88985196e63f");
        internal static Guid CoreCommandMIPKind = new Guid("fc599ba8-359d-4814-a255-26546565059c");
        internal static Guid CoreCommandMIPSidePanel = new Guid("e1bedfdb-114c-4f40-a102-278346a6f384");
        internal static Guid CoreCommandMIPViewItemPlugin = new Guid("06fa8a8a-c39a-4a15-a514-ec2ccf255940");
        internal static Guid CoreCommandMIPSettingsPanel = new Guid("ecc5bd32-c03e-43a8-a7e4-3eee9d969ef7");
        internal static Guid CoreCommandMIPBackgroundPlugin = new Guid("9112adcc-20da-48b9-8312-80bcefe6d556");
        internal static Guid CoreCommandMIPWorkSpacePluginId = new Guid("9ebce0b7-efee-44a5-9061-48cf4b82d28b");
        internal static Guid CoreCommandMIPWorkSpaceViewItemPluginId = new Guid("f5bffd0a-15d0-4ea0-b11e-191d00d3cce6");
        internal static Guid CoreCommandMIPTrackListViewItemPluginId = new Guid("d0ec9fd7-d2c0-4f37-9a4a-6b57a0b457d8");
        internal static Guid CoreCommandMIPTabPluginId = new Guid("cabdd2e8-fde4-43f6-94b5-2d3dbb3c06fd");
        internal static Guid CoreCommandMIPViewLayoutId = new Guid("452ef9ab-41a8-4c12-a6f5-af2b83d437a6");
        // IMPORTANT! Due to shortcoming in Visual Studio template the below cannot be automatically replaced with proper unique GUIDs, so you will have to do it yourself
        internal static Guid CoreCommandMIPWorkSpaceToolbarPluginId = new Guid("f0d08cee-c5ed-4c9a-9c94-2f2af5120a4e");
        internal static Guid CoreCommandMIPViewItemToolbarPluginId = new Guid("c2c6a43c-907e-4f12-94d1-ac2642d80bc7");
        internal static Guid CoreCommandMIPToolsOptionDialogPluginId = new Guid("5586a634-0df1-441d-91cc-c0d5857a1fbc");
        internal static Guid CoreCommandMIPClientActionId = new Guid("a8700ff5-7383-4324-acf5-14cb5df7d18c");
        internal static Guid CoreCommandMIPClientActionGroupId = new Guid("4c64b19f-6e62-4f07-baf3-9a8a8b7c37d7");

        #region Private fields

        private UserControl _treeNodeInofUserControl;

        //
        // Note that all the plugin are constructed during application start, and the constructors
        // should only contain code that references their own dll, e.g. resource load.

        private List<BackgroundPlugin> _backgroundPlugins = new List<BackgroundPlugin>();
        private Collection<SettingsPanelPlugin> _settingsPanelPlugins = new Collection<SettingsPanelPlugin>();
        private List<ViewItemPlugin> _viewItemPlugins = new List<ViewItemPlugin>();
        private List<ItemNode> _itemNodes = new List<ItemNode>();
        private List<SidePanelPlugin> _sidePanelPlugins = new List<SidePanelPlugin>();
        internal const string TrackListUpdatedMessageId = "CoreCommandMIP.TrackListUpdated";
        internal const string TrackSelectedMessageId = "CoreCommandMIP.TrackSelected";
        internal const string TrackAlarmMessageId = "CoreCommandMIP.TrackAlarm";

        private List<String> _messageIdStrings = new List<string>
        {
            TrackListUpdatedMessageId,
            TrackSelectedMessageId,
            TrackAlarmMessageId
        };
        private List<SecurityAction> _securityActions = new List<SecurityAction>();
        private List<WorkSpacePlugin> _workSpacePlugins = new List<WorkSpacePlugin>();
        private List<TabPlugin> _tabPlugins = new List<TabPlugin>();
        private List<ViewItemToolbarPlugin> _viewItemToolbarPlugins = new List<ViewItemToolbarPlugin>();
        private List<WorkSpaceToolbarPlugin> _workSpaceToolbarPlugins = new List<WorkSpaceToolbarPlugin>();
        private List<ViewLayout> _viewLayouts = new List<ViewLayout> { new CoreCommandMIPViewLayout() };
        private List<ToolsOptionsDialogPlugin> _toolsOptionsDialogPlugins = new List<ToolsOptionsDialogPlugin>();
        private List<ClientActionGroup> _clientActionGroups = new List<ClientActionGroup>();

        #endregion

        #region Initialization

        /// <summary>
        /// Load resources 
        /// </summary>
        static CoreCommandMIPDefinition()
        {
            try
            {
                _topTreeNodeImage = Properties.Resources.Server;
                
                // Pack URIs only work in WPF applications (Smart Client/Management Client)
                // On Event Server (Windows Service), skip WPF resource loading
                if (System.Windows.Application.Current != null)
                {
                    DummyImagePackUri = new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Resources/DummyItem.png");
                    _pluginIcon = new VideoOSIconUriSource() { Uri = DummyImagePackUri };
                    _treeNodeImage = ResourceToImage(DummyImagePackUri);
                }
                else
                {
                    // Running as Windows Service (Event Server) - use null/default values
                    DummyImagePackUri = null;
                    _pluginIcon = null;
                    _treeNodeImage = Properties.Resources.Server; // Fallback to embedded resource
                }
            }
            catch (Exception ex)
            {
                // If anything fails, log it but don't crash the plugin
                System.Diagnostics.Debug.WriteLine($"CoreCommandMIPDefinition static constructor failed: {ex.Message}");
                _topTreeNodeImage = null;
                DummyImagePackUri = null;
                _pluginIcon = null;
                _treeNodeImage = null;
            }
        }

        /// <summary>
        /// WPF requires resources to be stored with Build Action=Resource, which unfortunately cannot easily be read for WinForms controls, so we use this small
        /// utility method
        /// </summary>
        /// <param name="imageUri">Pack URI pointing to the image <seealso cref="https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf"/></param>
        /// <returns></returns>
        private static Image ResourceToImage(Uri imageUri)
        {
            var bitmapImage = new BitmapImage(imageUri);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Flush();
                return new Bitmap(stream);
            }
        }

        /// <summary>
        /// Get the icon for the plugin in WPF format
        /// </summary>
        internal static VideoOSIconSourceBase PluginIcon => _pluginIcon;

        /// <summary>
        /// Get the icon for the plugin in WinForms format
        /// </summary>
        internal static Image TreeNodeImage => _treeNodeImage;

        #endregion

        /// <summary>
        /// This method is called when the environment is up and running.
        /// Registration of Messages via RegisterReceiver can be done at this point.
        /// </summary>
        public override void Init()
        {
            // Populate all relevant lists with your plugins etc.
            _itemNodes.Add(new ItemNode(CoreCommandMIPKind, Guid.Empty,
                                         "CoreCommandMIP", _treeNodeImage,
                                         "CoreCommandMIPs", _treeNodeImage,
                                         Category.Text, true,
                                         ItemsAllowed.Many,
                                         new CoreCommandMIPItemManager(CoreCommandMIPKind),
                                         null
                                         ));
            if (EnvironmentManager.Instance.EnvironmentType == EnvironmentType.SmartClient)
            {
                _workSpacePlugins.Add(new CoreCommandMIPWorkSpacePlugin());
                _sidePanelPlugins.Add(new CoreCommandMIPSidePanelPlugin());
                _viewItemPlugins.Add(new CoreCommandMIPViewItemPlugin());
                _viewItemPlugins.Add(new CoreCommandMIPWorkSpaceViewItemPlugin());
                _viewItemPlugins.Add(new CoreCommandMIPTrackListViewItemPlugin());
                _viewItemToolbarPlugins.Add(new CoreCommandMIPViewItemToolbarPlugin());
                _workSpaceToolbarPlugins.Add(new CoreCommandMIPWorkSpaceToolbarPlugin());
                _settingsPanelPlugins.Add(new CoreCommandMIPSettingsPanelPlugin());

                ClientActionGroup clientActionGroup = new ClientActionGroup(CoreCommandMIPClientActionGroupId, "CoreCommandMIP Client Action Group", CoreCommandMIPDefinition.PluginIcon); //Note that the group name should be localized.
                clientActionGroup.Actions.Add(new CoreCommandMIPClientAction());
                _clientActionGroups.Add(clientActionGroup);
            }
            if (EnvironmentManager.Instance.EnvironmentType == EnvironmentType.Administration)
            {
                _tabPlugins.Add(new CoreCommandMIPTabPlugin());
                _toolsOptionsDialogPlugins.Add(new CoreCommandMIPToolsOptionDialogPlugin());
            }

            _backgroundPlugins.Add(new CoreCommandMIPBackgroundPlugin());
        }

        /// <summary>
        /// The main application is about to be in an undetermined state, either logging off or exiting.
        /// You can release resources at this point, it should match what you acquired during Init, so additional call to Init() will work.
        /// </summary>
        public override void Close()
        {
            _itemNodes.Clear();
            _sidePanelPlugins.Clear();
            _viewItemPlugins.Clear();
            _settingsPanelPlugins.Clear();
            _backgroundPlugins.Clear();
            _workSpacePlugins.Clear();
            _tabPlugins.Clear();
            _viewItemToolbarPlugins.Clear();
            _workSpaceToolbarPlugins.Clear();
            _toolsOptionsDialogPlugins.Clear();
            _clientActionGroups.Clear();
        }

        /// <summary>
        /// Return any new messages that this plugin can use in SendMessage or PostMessage,
        /// or has a Receiver set up to listen for.
        /// The suggested format is: "YourCompany.Area.MessageId"
        /// </summary>
        public override List<string> PluginDefinedMessageIds
        {
            get
            {
                return _messageIdStrings;
            }
        }

        /// <summary>
        /// If authorization is to be used, add the SecurityActions the entire plugin 
        /// would like to be available.  E.g. Application level authorization.
        /// </summary>
        public override List<SecurityAction> SecurityActions
        {
            get
            {
                return _securityActions;
            }
            set
            {
            }
        }

        #region Identification Properties

        /// <summary>
        /// Gets the unique id identifying this plugin component
        /// </summary>
        public override Guid Id
        {
            get
            {
                return CoreCommandMIPPluginId;
            }
        }

        /// <summary>
        /// This Guid can be defined on several different IPluginDefinitions with the same value,
        /// and will result in a combination of this top level ProductNode for several plugins.
        /// Set to Guid.Empty if no sharing is enabled.
        /// </summary>
        public override Guid SharedNodeId
        {
            get
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Define name of top level Tree node - e.g. A product name
        /// </summary>
        public override string Name
        {
            get { return "CoreCommandMIP"; }
        }

        /// <summary>
        /// Your company name
        /// </summary>
        public override string Manufacturer
        {
            get
            {
                return "Your Company name";
            }
        }

        /// <summary>
        /// Version of this plugin.
        /// </summary>
        public override string VersionString
        {
            get
            {
                return "1.0.0.0";
            }
        }

        /// <summary>
        /// Icon to be used on top level - e.g. a product or company logo
        /// </summary>
        public override System.Drawing.Image Icon
        {
            get { return _topTreeNodeImage; }
        }

        #endregion

        #region Administration properties

        /// <summary>
        /// A list of server side configuration items in the administrator
        /// </summary>
        public override List<ItemNode> ItemNodes
        {
            get { return _itemNodes; }
        }

        /// <summary>
        /// An extension plug-in running in the Administrator to add a tab for built-in devices and hardware.
        /// </summary>
        public override ICollection<TabPlugin> TabPlugins
        {
            get { return _tabPlugins; }
        }

        /// <summary>
        /// An extension plug-in running in the Administrator to add more tabs to the Tools-Options dialog.
        /// </summary>
        public override List<ToolsOptionsDialogPlugin> ToolsOptionsDialogPlugins
        {
            get { return _toolsOptionsDialogPlugins; }
        }

        /// <summary>
        /// A user control to display when the administrator clicks on the top TreeNode
        /// </summary>
        public override UserControl GenerateUserControl()
        {
            _treeNodeInofUserControl = new HelpPage();
            return _treeNodeInofUserControl;
        }

        /// <summary>
        /// This property can be set to true, to be able to display your own help UserControl on the entire panel.
        /// When this is false - a standard top and left side is added by the system.
        /// </summary>
        public override bool UserControlFillEntirePanel
        {
            get { return false; }
        }
        #endregion

        #region Client related methods and properties

        /// <summary>
        /// A list of Client side definitions for Smart Client
        /// </summary>
        public override List<ViewItemPlugin> ViewItemPlugins
        {
            get { return _viewItemPlugins; }
        }

        /// <summary>
        /// An extension plug-in running in the Smart Client to add more choices on the Settings panel.
        /// Supported from Smart Client 2017 R1. For older versions use OptionsDialogPlugins instead.
        /// </summary>
        public override Collection<SettingsPanelPlugin> SettingsPanelPlugins
        {
            get { return _settingsPanelPlugins; }
        }

        /// <summary> 
        /// An extension plugin to add to the side panel of the Smart Client.
        /// </summary>
        public override List<SidePanelPlugin> SidePanelPlugins
        {
            get { return _sidePanelPlugins; }
        }

        /// <summary>
        /// Return the workspace plugins
        /// </summary>
        public override List<WorkSpacePlugin> WorkSpacePlugins
        {
            get { return _workSpacePlugins; }
        }

        /// <summary> 
        /// An extension plug-in to add to the view item toolbar in the Smart Client.
        /// </summary>
        public override List<ViewItemToolbarPlugin> ViewItemToolbarPlugins
        {
            get { return _viewItemToolbarPlugins; }
        }

        /// <summary> 
        /// An extension plug-in to add to the work space toolbar in the Smart Client.
        /// </summary>
        public override List<WorkSpaceToolbarPlugin> WorkSpaceToolbarPlugins
        {
            get { return _workSpaceToolbarPlugins; }
        }

        /// <summary>
        /// An extension plug-in running in the Smart Client to provide extra view layouts.
        /// </summary>
        public override List<ViewLayout> ViewLayouts
        {
            get { return _viewLayouts; }
        }

        /// <summary>
        /// An extension plug-in running in the Smart Client to provide actions that can be activated by the operator.
        /// </summary>
        public override List<ClientActionGroup> ClientActionGroups
        {
            get { return _clientActionGroups; }
        }

        #endregion


        /// <summary>
        /// Create and returns the background task.
        /// </summary>
        public override List<BackgroundPlugin> BackgroundPlugins
        {
            get { return _backgroundPlugins; }
        }

    }
}
