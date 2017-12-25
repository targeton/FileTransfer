using FileTransfer.Sockets;
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
using System.Windows.Forms;

namespace FileTransfer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 变量
        private SubscribeView _subscribeView;
        private LogsQueryView _logsQueryView;
        private AddMonitorView _addMonitorView;
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
            //SimpleIoc.Default.Unregister<MainViewModel>();
            //SimpleIoc.Default.Unregister<SubscribeViewModel>();
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
                case "ShowLogsQueryView":
                    if (_logsQueryView == null || (new WindowInteropHelper(_logsQueryView)).Handle == IntPtr.Zero)
                        _logsQueryView = new LogsQueryView(this);
                    _logsQueryView.ShowDialog();
                    break;
                case "CloseLogsQueryView":
                    if (_logsQueryView != null)
                        _logsQueryView.Close();
                    break;
                case "ShowAddMonitorView":
                    if (_addMonitorView == null || (new WindowInteropHelper(_addMonitorView)).Handle == IntPtr.Zero)
                        _addMonitorView = new AddMonitorView(this);
                    _addMonitorView.ShowDialog();
                    break;
                case "CloseAddMonitorView":
                    if (_addMonitorView != null)
                        _addMonitorView.Close();
                    break;
                default:
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //如果正在接发数据，则阻止程序关闭
            if (SynchronousSocketManager.Instance.SendingFilesFlag || SynchronousSocketManager.Instance.ReceivingFlag)
            {
                System.Windows.Forms.MessageBox.Show("当前程序正在接发数据！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
            }
        }
    }
}
