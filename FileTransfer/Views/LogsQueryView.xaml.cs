using System;
using System.Windows;

namespace FileTransfer.Views
{
    /// <summary>
    /// LogsQueryView.xaml 的交互逻辑
    /// </summary>
    public partial class LogsQueryView : Window
    {
        public LogsQueryView()
        {
            InitializeComponent();
        }

        public LogsQueryView(Window parent = null)
        {
            InitializeComponent();
            this.Owner = parent;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //SimpleIoc.Default.Unregister<LogsQueryViewModel>();
        }

    }
}
