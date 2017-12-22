using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            SimpleIoc.Default.Unregister<AddMonitorViewModel>();
        }
    }
}
