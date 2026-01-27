using System;
using VideoOS.Platform.Client;

namespace CoreCommandMIP.Client
{
    public partial class CoreCommandMIPWorkSpaceViewItemWpfUserControl : ViewItemWpfUserControl
    {
        private readonly CoreCommandMIPViewItemWpfUserControl _innerControl;

        public CoreCommandMIPWorkSpaceViewItemWpfUserControl()
        {
            InitializeComponent();
        }

        public CoreCommandMIPWorkSpaceViewItemWpfUserControl(CoreCommandMIPWorkSpaceViewItemManager manager) : this()
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            _innerControl = new CoreCommandMIPViewItemWpfUserControl(manager);
            _contentHost.Content = _innerControl;
        }

        public override void Init()
        {
            _innerControl?.Init();
        }

        public override void Close()
        {
            _innerControl?.Close();
        }

        public override bool ShowToolbar => false;

        private void ViewItemWpfUserControl_ClickEvent(object sender, EventArgs e)
        {
            FireClickEvent();
        }

        private void ViewItemWpfUserControl_DoubleClickEvent(object sender, EventArgs e)
        {
            FireDoubleClickEvent();
        }
    }
}
