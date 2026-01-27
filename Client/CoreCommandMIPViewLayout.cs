using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VideoOS.Platform.Client;
using VideoOS.Platform.UI.Controls;

namespace CoreCommandMIP.Client
{
    public class CoreCommandMIPViewLayout : ViewLayout
    {
        public override VideoOSIconBitmapSource IconSource
        {
            get => new VideoOSIconBitmapSource() { BitmapSource = new BitmapImage(CoreCommandMIPDefinition.DummyImagePackUri) };
            set { }
        }

        public override Rectangle[] Rectangles
        {
            get { return new Rectangle[] { new Rectangle(000, 000, 999, 499), new Rectangle(000, 499, 499, 499), new Rectangle(499, 499, 499, 499) }; }
            set { }
        }

        public override Guid Id
        {
            get { return CoreCommandMIPDefinition.CoreCommandMIPViewLayoutId; }
            set { }
        }

        public override string DisplayName
        {
            get { return "CoreCommandMIP"; }
            set { }
        }
    }
}
