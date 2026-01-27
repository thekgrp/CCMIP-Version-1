using VideoOS.Platform.Admin;

namespace CoreCommandMIP.Admin
{
    public partial class CoreCommandMIPToolsOptionDialogUserControl : ToolsOptionsDialogUserControl
    {
        public CoreCommandMIPToolsOptionDialogUserControl()
        {
            InitializeComponent();
        }

        public override void Init()
        {
        }

        public override void Close()
        {
        }

        public string MyPropValue
        {
            set { textBoxPropValue.Text = value ?? ""; }
            get { return textBoxPropValue.Text; }
        }
    }
}
