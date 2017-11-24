using FileTransfer.Configs;
using FileTransfer.FileWatcher;
using FileTransfer.Models;
using FileTransfer.Sockets;
using FileTransfer.Utils;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileTransfer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(MainViewModel));
        #endregion

        #region 属性
        private ObservableCollection<MonitorModel> _monitorCollection;

        public ObservableCollection<MonitorModel> MonitorCollection
        {
            get { return _monitorCollection ?? (_monitorCollection = new ObservableCollection<MonitorModel>()); }
            set
            {
                _monitorCollection = value;
                RaisePropertyChanged("MonitorCollection");
            }
        }

        private ObservableCollection<SubscribeModel> _subscribeCollection;

        public ObservableCollection<SubscribeModel> SubscribeCollection
        {
            get { return _subscribeCollection ?? (_subscribeCollection = new ObservableCollection<SubscribeModel>()); }
            set
            {
                _subscribeCollection = value;
                RaisePropertyChanged("SubscribeCollection");
            }
        }

        private bool _monitorFlag;

        public bool MonitorFlag
        {
            get { return _monitorFlag; }
            set
            {
                _monitorFlag = value;
                RaisePropertyChanged("MonitorFlag");
            }
        }

        private int _listenPort;

        public int ListenPort
        {
            get { return _listenPort; }
            set
            {
                _listenPort = value;
                RaisePropertyChanged("ListenPort");
            }
        }

        private bool _canSetListenPort;

        public bool CanSetListenPort
        {
            get { return _canSetListenPort; }
            set
            {
                _canSetListenPort = value;
                RaisePropertyChanged("CanSetListenPort");
            }
        }

        private int _scanPeriod;

        public int ScanPeriod
        {
            get { return _scanPeriod; }
            set
            {
                _scanPeriod = value;
                RaisePropertyChanged("ScanPeriod");
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
        public RelayCommand<bool> ControlMonitorCommand { get; set; }
        public RelayCommand AddMonitorCommand { get; set; }
        public RelayCommand<object> DeleteSettingCommand { get; set; }
        public RelayCommand QueryLogsCommand { get; set; }
        public RelayCommand LoadedCommand { get; set; }
        public RelayCommand ClosedCommand { get; set; }
        public RelayCommand AddSubscibeCommand { get; set; }
        public RelayCommand<bool> SetListenPortCommand { get; set; }
        #endregion

        #region 构造函数
        public MainViewModel()
        {
            InitialParams();
            InitialCommands();
        }
        #endregion

        #region 方法
        private void InitialParams()
        {
            MonitorFlag = true;
            CanSetListenPort = false;
        }

        #region 执行命令

        private void InitialCommands()
        {
            ControlMonitorCommand = new RelayCommand<bool>(ExecuteControlMonitorCommand);
            AddMonitorCommand = new RelayCommand(ExecuteAddMonitorCommand);
            DeleteSettingCommand = new RelayCommand<object>(ExecuteDeleteMonitorCommand);
            QueryLogsCommand = new RelayCommand(ExecuteQueryLogsCommand);
            LoadedCommand = new RelayCommand(ExecuteLoadedCommand);
            ClosedCommand = new RelayCommand(ExecuteClosedCommand);
            AddSubscibeCommand = new RelayCommand(ExecuteAddSubscibeCommand, CanExecuteAddSubscibeCommand);
            SetListenPortCommand = new RelayCommand<bool>(ExecuteSetListenPortCommand);
        }

        private void ExecuteControlMonitorCommand(bool control)
        {
            if (control)
            {
                try
                {
                    NotifyText = @"开始监控前检测......";
                    if (!CheckMonitorSettings())
                    {
                        NotifyText = @"";
                        return;
                    }
                    //开启监控
                    FileWatcherHelper.Instance.StartMoniter();
                    MonitorFlag = false;
                    NotifyText = @"监控开启中......";
                }
                catch (Exception e)
                {
                    _logger.Error(string.Format("开启监控过程中发生异常！异常信息：{0}", e.Message));
                    MessageBox.Show("开启监控过程中发生异常！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                //关闭监控
                FileWatcherHelper.Instance.StopMoniter();
                MonitorFlag = true;
                NotifyText = @"监控已关闭！";
            }
        }

        private bool CheckMonitorSettings()
        {
            //检查是否存在监控文件夹
            if (MonitorCollection.Count <= 0)
            {
                MessageBox.Show("当前未配置任务监控！请配置！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            foreach (var monitor in MonitorCollection)
            {
                if (IOHelper.Instance.HasMonitorDirectory(monitor.MonitorDirectory))
                    continue;
                MessageBox.Show(string.Format("当前计算机内不存在{0}监控文件夹！请检查", monitor.MonitorDirectory), "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            //检查是否设置删除文件
            foreach (var monitor in MonitorCollection)
            {
                if (!monitor.DeleteFiles)
                    continue;
                List<string> filesPath = IOHelper.Instance.GetAllFiles(monitor.MonitorDirectory);
                if (filesPath == null)
                {
                    MessageBox.Show(@"获取监控目录下的文件时发生异常！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (filesPath.Count <= 0)
                    continue;
                if (MessageBox.Show(string.Format("监控文件夹{0}内含有文件，与配置不符，开启监控前是否将内部文件删除？", monitor.MonitorDirectory), "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    IOHelper.Instance.DeleteFiles(filesPath);
            }
            //检查是否设置删除子文件夹
            foreach (var monitor in MonitorCollection)
            {
                if (!monitor.DeleteSubdirectory)
                    continue;
                List<string> directoriesPath = IOHelper.Instance.GetAllSubDirectories(monitor.MonitorDirectory);
                if (directoriesPath == null)
                    MessageBox.Show(@"获取监控目录下的子文件夹时发生异常！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (directoriesPath.Count <= 0)
                    continue;
                if (MessageBox.Show(string.Format("监控文件夹{0}内含有子文件夹，与配置不符，开启监控前是否将内部子文件夹删除？", monitor.MonitorDirectory), "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    IOHelper.Instance.DeleteDirectories(directoriesPath);
            }
            return true;
        }

        private void ExecuteAddMonitorCommand()
        {
            var dlg = new FolderBrowserDialog();
            dlg.Description = @"请选择监控文件夹目录";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = dlg.SelectedPath;
                if (MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == selectedPath) == null)
                    MonitorCollection.Add(new MonitorModel() { MonitorDirectory = selectedPath });
                else
                    MessageBox.Show("所选文件夹已在监控目录中！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExecuteDeleteMonitorCommand(object deleteItem)
        {
            var model1 = deleteItem as MonitorModel;
            if (model1 != null)
            {
                //检查发送标志位（若为true则不允许删除配置）
                if (SynchronousSocketManager.Instance.SendingFilesFlag)
                {
                    MessageBox.Show("当前正在发送文件，不允许删除任何监控配置项！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //删除监控配置
                MonitorCollection.Remove(model1);
                if (string.IsNullOrEmpty(model1.SubscribeIP)) return;
                //删除监控配置后通知相关订阅方，删除相关配置
                SynchronousSocketManager.Instance.SendDeleteMonitorInfo(UtilHelper.Instance.GetIPEndPoint(model1.SubscribeIP), model1.MonitorDirectory);
            }
            else
            {
                var model2 = deleteItem as SubscribeModel;
                if (model2 == null) return;
                //检查接收标志位（若为true则不允许删除配置）
                if (SynchronousSocketManager.Instance.ReceivingFlag)
                {
                    MessageBox.Show("当前正在接收，不允许删除任何接收配置项！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //删除接收配置
                SubscribeCollection.Remove(model2);
                //删除接收配置后，综合接收配置决定是否通知监控端删除订阅信息
                if (SubscribeCollection.FirstOrDefault(s => s.MonitorIP == model2.MonitorIP) == null)
                    SynchronousSocketManager.Instance.SendUnregisterSubscribeInfo(UtilHelper.Instance.GetIPEndPoint(string.Format("{0}:{1}", model2.MonitorIP, model2.MonitorListenPort)), model2.MonitorDirectory);
            }
        }

        private void ExecuteQueryLogsCommand()
        {
            if (SynchronousSocketManager.Instance.SendingFilesFlag || SynchronousSocketManager.Instance.ReceivingFlag)
            {
                MessageBox.Show("当前程序正在接发数据！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Messenger.Default.Send<string>("ShowLogsQueryView");
        }

        private void ExecuteLoadedCommand()
        {
            var t = Task.Factory.StartNew(() =>
            {
                ConfigHelper.Instance.Initial();
                var monitorTemp = new ObservableCollection<MonitorModel>();
                ConfigHelper.Instance.MonitorSettings.ForEach(m => monitorTemp.Add(m));
                MonitorCollection = new ObservableCollection<MonitorModel>(monitorTemp);
                var subscribeTemp = new ObservableCollection<SubscribeModel>();
                ConfigHelper.Instance.SubscribeSettings.ForEach(s => subscribeTemp.Add(s));
                SubscribeCollection = new ObservableCollection<SubscribeModel>(subscribeTemp);
                ListenPort = ConfigHelper.Instance.ListenPort;
                ScanPeriod = ConfigHelper.Instance.ScanPeriod;
                //订阅事件
                FileWatcherHelper.Instance.NotifyMonitorChanges = SynchronousSocketManager.Instance.SendMonitorChanges;
                SynchronousSocketManager.Instance.SendFileProgress += ShowSendProgress;
                SynchronousSocketManager.Instance.AcceptFileProgress += ShowAcceptProgress;
                SynchronousSocketManager.Instance.CompleteSendFile += ShowCompleteSendFile;
                SynchronousSocketManager.Instance.CompleteAcceptFile += ShowCompleteAcceptFile;
                _logger.Info("主窗体加载完毕!");
            });
            t.ContinueWith(v =>
            {
                SynchronousSocketManager.Instance.StartListening(ListenPort);
            });
        }

        private void ShowCompleteAcceptFile(string monitorIp, string monitorDirectory)
        {
            SubscribeCollection.Where(s => s.MonitorIP == monitorIp && s.MonitorDirectory == monitorDirectory).ToList().ForEach(s =>
            {
                s.AcceptFileName = @"";
                s.AcceptFilePercent = 0.0;
            });
        }

        private void ShowCompleteSendFile(string monitor)
        {
            MonitorCollection.Where(m => m.MonitorDirectory == monitor).ToList().ForEach(m =>
            {
                m.TransferFileName = @"";
                m.TransferPercent = 0.0;
            });
        }

        private void ShowAcceptProgress(string monitorIp, string monitorDirectory, string sendFile, double progress)
        {
            SubscribeCollection.Where(s => s.MonitorIP == monitorIp && s.MonitorDirectory == monitorDirectory).ToList().ForEach(s =>
            {
                s.AcceptFileName = sendFile.Replace(monitorDirectory, s.AcceptDirectory);
                s.AcceptFilePercent = progress;
            });
        }

        private void ShowSendProgress(string monitor, string sendFile, double progerss)
        {
            MonitorCollection.Where(m => m.MonitorDirectory == monitor).ToList().ForEach(m =>
            {
                m.TransferFileName = sendFile;
                m.TransferPercent = progerss;
            });
        }

        private void ExecuteClosedCommand()
        {
            var monitors = MonitorCollection.ToList();
            var subscribes = SubscribeCollection.ToList();
            ConfigHelper.Instance.SaveSettings(monitors, subscribes, ListenPort, ScanPeriod);
            SynchronousSocketManager.Instance.StopListening();
            _logger.Info("主窗体卸载完毕!");
        }

        private bool CanExecuteAddSubscibeCommand()
        {
            return !SynchronousSocketManager.Instance.ReceivingFlag && !SynchronousSocketManager.Instance.SendingFilesFlag;
        }

        private void ExecuteAddSubscibeCommand()
        {
            Messenger.Default.Send<string>("ShowSubscribeView");
        }

        private void ExecuteSetListenPortCommand(bool canSet)
        {
            if (canSet)
            {
                SynchronousSocketManager.Instance.StartListening(ListenPort);
                CanSetListenPort = false;
            }
            else
            {
                //判断是否有接收（有的话则不允许关闭监听）
                if (SynchronousSocketManager.Instance.ReceivingFlag)
                {
                    MessageBox.Show("当前监听端口正在接收信息！暂不允许关闭！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                SynchronousSocketManager.Instance.StopListening();
                CanSetListenPort = true;
            }
        }

        #endregion

        #region 公共方法
        public void CompleteMonitorSetting(string subscribeIP, string monitorDirectory)
        {
            if (MonitorCollection.Any(m => m.MonitorDirectory == monitorDirectory && m.SubscribeIP == subscribeIP)) return;
            var monitor = MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == monitorDirectory);
            if (monitor == null) return;
            if (string.IsNullOrEmpty(monitor.SubscribeIP))
                monitor.SubscribeIP = subscribeIP;
            else
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MonitorCollection.Add(new MonitorModel() { MonitorDirectory = monitorDirectory, SubscribeIP = subscribeIP });
                }));
            }
        }

        public void RemoveAcceptSettings(string monitorIP, string monitorDirectory)
        {
            var accepts = SubscribeCollection.Where(s => s.MonitorIP == monitorIP && s.MonitorDirectory == monitorDirectory).ToList();
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var accept in accepts)
                {
                    SubscribeCollection.Remove(accept);
                }
            }));

        }
        #endregion

        #endregion
    }
}
