using System.Windows;
using System.Windows.Controls;

namespace CoreCommandMIP.Client
{
    public partial class CoreCommandMIPSettingsPanelControl : UserControl
    {
        private readonly CoreCommandMIPSettingsPanelPlugin _plugin;
        private const string _propertyId = "aSettingId";
        public CoreCommandMIPSettingsPanelControl(CoreCommandMIPSettingsPanelPlugin plugin)
        {
            _plugin = plugin;

            InitializeComponent();

            _aSettingTextBox.Text = _plugin.GetProperty(_propertyId);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _plugin.SetProperty(_propertyId, _aSettingTextBox.Text);
            string errorMessage;
            if (!_plugin.TrySaveChanges(out errorMessage))
            {
                MessageBox.Show(errorMessage);
            }
        }
    }
}
