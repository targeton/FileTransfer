using FileTransfer.Models;
using FileTransfer.Sockets;
using FileTransfer.Utils;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileTransfer.ViewModels
{
    public class SubscribeViewModel : ViewModelBase
    {
        #region 变量

        #endregion

        #region 属性
        private string _remoteIP;

        public string RemoteIP
        {
            get { return _remoteIP; }
            set
            {
                _remoteIP = value;
                RaisePropertyChanged("RemoteIP");
            }
        }

        private int _remotePort;

        public int RemotePort
        {
            get { return _remotePort; }
            set
            {
                _remotePort = value;
                RaisePropertyChanged("RemotePort");
            }
        }

        private ObservableCollection<RemoteMonitorModel> _remoteMonitorFloders;

        public ObservableCollection<RemoteMonitorModel> RemoteMonitorFloders
        {
            get { return _remoteMonitorFloders ?? (_remoteMonitorFloders = new ObservableCollection<RemoteMonitorModel>()); }
            set
            {
                _remoteMonitorFloders = value;
                RaisePropertyChanged("RemoteMonitorFloders");
            }
        }

        private string _acceptFilePath;

        public string AcceptFilePath
        {
            get { return _acceptFilePath; }
            set
            {
                _acceptFilePath = value;
                RaisePropertyChanged("AcceptFilePath");
            }
        }

        private string _notifyText;

        public string NotifyText
        {
            get { return _notifyText; }
            set
            {
                _notifyText = value;
                RaisePropertyChanged("NotifyText");
            }
        }

        #endregion

        #region 命令
        public RelayCommand RequestMonitorFlodersCommand { get; set; }
        public RelayCommand SetAcceptFilePathCommand { get; set; }
        public RelayCommand ConfirmCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
        #endregion

        #region 构造函数
        public SubscribeViewModel()
        {
            InitialCommands();
        }
        #endregion

        #region 方法
        private void InitialCommands()
        {
            RequestMonitorFlodersCommand = new RelayCommand(ExecuteRequestMonitorFlodersCommand, CanExecuteRequestMonitorFlodersCommand);
            SetAcceptFilePathCommand = new RelayCommand(ExecuteSetAcceptFilePathCommand);
            ConfirmCommand = new RelayCommand(ExecuteConfirmCommand, CanExecuteConfirmCommand);
            CancelCommand = new RelayCommand(ExecuteCancelCommand);
        }

        private bool CanExecuteRequestMonitorFlodersCommand()
        {
            return !string.IsNullOrEmpty(RemoteIP) && Regex.IsMatch(RemoteIP, @"^(((\d{1,2})|(1[0-9][0-9])|(2[0-4][0-9])|(25[0-5]))\.){3}((\d{1,2})|(1[0-9][0-9])|(2[0-4][0-9])|(25[0-5]))$") && (RemotePort >= 0 && RemotePort <= 65535);
        }

        private void ExecuteRequestMonitorFlodersCommand()
        {
            RemoteMonitorFloders = new ObservableCollection<RemoteMonitorModel>();
            byte[] address = UtilHelper.Instance.GetIPAddressBytes(RemoteIP);
            IPEndPoint ep = new IPEndPoint(new IPAddress(address), RemotePort);
            List<string> remoteMonitorFloders = SynchronousSocketManager.Instance.RequestRemoteMoniterFloders(ep);
            if (remoteMonitorFloders != null && remoteMonitorFloders.Count > 0)
            {
                remoteMonitorFloders = remoteMonitorFloders.Distinct().ToList();
                remoteMonitorFloders.ForEach(f => RemoteMonitorFloders.Add(new RemoteMonitorModel(f)));
            }
            else
                MessageBox.Show(@"从远端未能获取监控文件夹信息！可能未配置监控文件夹！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }



        private void ExecuteSetAcceptFilePathCommand()
        {
            var dlg = new FolderBrowserDialog();
            dlg.Description = @"请选择接收文件夹目录";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                AcceptFilePath = dlg.SelectedPath;
            }
        }

        private bool CanExecuteConfirmCommand()
        {
            //TODO:添加RemoteIP和RemotePort的正则表达式来判断输入值是否合法
            return RemoteMonitorFloders.Count > 0 && (RemoteMonitorFloders.Where(m => m.IsSelected == true).Count() == 1) && !string.IsNullOrEmpty(AcceptFilePath);
        }

        private void ExecuteConfirmCommand()
        {
            string remoteAddress = string.Format("{0}:{1}", RemoteIP, RemotePort);
            string monitorDirectory = RemoteMonitorFloders.Where(m => m.IsSelected == true).ElementAt(0).RemoteMonitorFloder;
            string acceptDirectiory = AcceptFilePath;
            SubscribeModel subscribe = new SubscribeModel() { MonitorIP = RemoteIP, MonitorDirectory = monitorDirectory, AcceptDirectory = acceptDirectiory };
            if (SimpleIoc.Default.GetInstance<MainViewModel>().SubscribeCollection.FirstOrDefault(s => s.MonitorIP == remoteAddress && s.MonitorDirectory == monitorDirectory && s.AcceptDirectory == acceptDirectiory) != null)
            {
                MessageBox.Show("接受配置中已有相同项！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SimpleIoc.Default.GetInstance<MainViewModel>().SubscribeCollection.Add(subscribe);
            byte[] ipBytes = UtilHelper.Instance.GetIPAddressBytes(RemoteIP);
            IPEndPoint remote = new IPEndPoint(new IPAddress(ipBytes), RemotePort);
            SynchronousSocketManager.Instance.SendSubscribeInfo(remote, monitorDirectory);
            Messenger.Default.Send<string>("CloseSubscribeView");
        }

        private void ExecuteCancelCommand()
        {

            Messenger.Default.Send<string>("CloseSubscribeView");
        }
        #endregion

    }

    public class RemoteMonitorModel : ObservableObject
    {
        #region 属性
        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                RaisePropertyChanged("IsSelected");
            }
        }

        private string _remoteMonitorFolder;

        public string RemoteMonitorFloder
        {
            get { return _remoteMonitorFolder; }
            set
            {
                _remoteMonitorFolder = value;
                RaisePropertyChanged("RemoteMonitorFloder");
            }
        }

        #endregion

        #region 构造函数
        public RemoteMonitorModel(string monitorFloder, bool isSelected = false)
        {
            RemoteMonitorFloder = monitorFloder;
            IsSelected = isSelected;
        }
        #endregion
    }
}
