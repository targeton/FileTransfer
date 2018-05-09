using System;
using System.Windows;

namespace FileTransfer.Views
{
    /// <summary>
    /// AddMonitorView.xaml 的交互逻辑
    /// </summary>
    public partial class AddMonitorView : Window
    {
        public AddMonitorView()
        {
            InitializeComponent();
        }

        public AddMonitorView(Window parent = null)
        {
            InitializeComponent();
            this.Owner = parent;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //SimpleIoc.Default.Unregister<AddMonitorViewModel>();
        }
    }
}
