using FileTransfer.ViewModels;
using FileTransfer.Views;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileTransfer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 变量
        private SubscribeView _subscribeView;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Register<string>(this, ReceiveMessage);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Messenger.Default.Unregister<string>(this, ReceiveMessage);
            SimpleIoc.Default.Unregister<MainViewModel>();
            SimpleIoc.Default.Unregister<SubscribeViewModel>();
        }

        private void ReceiveMessage(string message)
        {
            switch (message)
            {
                case "ShowSubscribeView":
                    if (_subscribeView == null || (new WindowInteropHelper(_subscribeView)).Handle == IntPtr.Zero)
                        _subscribeView = new SubscribeView(this);
                    _subscribeView.ShowDialog();
                    break;
                case "CloseSubscribeView":
                    if (_subscribeView != null)
                        _subscribeView.Close();
                    break;
                default:
                    break;
            }
        }
    }
}
