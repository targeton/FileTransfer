using System;
using System.Windows;

namespace FileTransfer.Views
{
    /// <summary>
    /// SubscribeView.xaml 的交互逻辑
    /// </summary>
    public partial class SubscribeView : Window
    {
        public SubscribeView()
        {
            InitializeComponent();
        }

        public SubscribeView(Window parent = null)
        {
            InitializeComponent();
            this.Owner = parent;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //SimpleIoc.Default.Unregister<SubscribeViewModel>();
        }
    }
}
