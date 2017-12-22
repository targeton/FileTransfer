using FileTransfer.Configs;
using FileTransfer.DbHelper.Entitys;
using FileTransfer.FileWatcher;
using FileTransfer.IO;
using FileTransfer.LogToDb;
using FileTransfer.Models;
using FileTransfer.Sockets;
using FileTransfer.Utils;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using log4net;
using Microsoft.WindowsAPICodePack.Dialogs;
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
        private System.Timers.Timer _checkConnectTimer;
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
                ConfigHelper.Instance.SaveListenPortSetting(value);
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
                ConfigHelper.Instance.SaveScanPeridSetting(value);
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

        private string _exceptionSavePath;

        public string ExceptionSavePath
        {
            get { return _exceptionSavePath; }
            set
            {
                _exceptionSavePath = value;
                RaisePropertyChanged("ExceptionSavePath");
            }
        }


        #endregion

        #region 命令
        public RelayCommand<bool> ControlMonitorCommand { get; set; }
        public RelayCommand AddMonitorCommand { get; set; }
        public RelayCommand<object> DeleteMonitorSettingCommand { get; set; }
        public RelayCommand<object> DeleteSubscribeSettingCommand { get; set; }
        public RelayCommand QueryLogsCommand { get; set; }
        public RelayCommand LoadedCommand { get; set; }
        public RelayCommand ClosedCommand { get; set; }
        public RelayCommand AddSubscibeCommand { get; set; }
        public RelayCommand<bool> SetListenPortCommand { get; set; }
        public RelayCommand SetSendExceptionCommand { get; set; }
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
            DeleteMonitorSettingCommand = new RelayCommand<object>(ExecuteDeleteMonitorSettingCommand);
            DeleteSubscribeSettingCommand = new RelayCommand<object>(ExecuteDeleteSubscribeSettingCommand);
            QueryLogsCommand = new RelayCommand(ExecuteQueryLogsCommand);
            LoadedCommand = new RelayCommand(ExecuteLoadedCommand);
            ClosedCommand = new RelayCommand(ExecuteClosedCommand);
            AddSubscibeCommand = new RelayCommand(ExecuteAddSubscibeCommand, CanExecuteAddSubscibeCommand);
            SetListenPortCommand = new RelayCommand<bool>(ExecuteSetListenPortCommand);
            SetSendExceptionCommand = new RelayCommand(ExecuteSetSendExceptionCommand);
        }

        private void ExecuteControlMonitorCommand(bool control)
        {
            if (control)
            {
                try
                {
                    NotifyText = @"开始监控前检测......";
                    bool? checkFlag = CheckMonitorSettings();
                    if (checkFlag == false)
                    {
                        NotifyText = @"";
                        return;
                    }
                    //开启监控
                    FileWatcherHelper.Instance.StartMoniter(checkFlag == true);
                    MonitorFlag = false;
                    NotifyText = @"监控开启中......";
                }
                catch (Exception e)
                {
                    string msg = string.Format("开启监控过程中发生异常！异常信息：{0}", e.Message);
                    _logger.Error(msg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
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

        private bool? CheckMonitorSettings()
        {
            //检查是否存在监控文件夹
            if (MonitorCollection.Count <= 0)
            {
                MessageBox.Show("当前未配置任务监控！请配置！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            foreach (var monitor in MonitorCollection)
            {
                if (IOHelper.Instance.HasDirectory(monitor.MonitorDirectory))
                    continue;
                MessageBox.Show(string.Format("当前计算机内不存在{0}监控文件夹！请检查", monitor.MonitorDirectory), "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            //检查监控文件夹内是否有文件或子文件夹
            List<string> files = new List<string>();
            List<string> directories = new List<string>();
            foreach (var monitor in MonitorCollection)
            {
                List<string> filesPath = IOHelper.Instance.GetAllFiles(monitor.MonitorDirectory);
                if (filesPath != null && filesPath.Count > 0)
                    files.AddRange(filesPath);
                List<string> directoriesPath = IOHelper.Instance.GetAllSubDirectories(monitor.MonitorDirectory);
                if (directoriesPath != null && directoriesPath.Count > 0)
                    directories.AddRange(directoriesPath);
            }
            if (files.Count > 0 || directories.Count > 0)
            {
                if (MessageBox.Show("监控目录集合中存在文件或子文件夹，是否忽略？\n（是：删除文件及子文件夹，否：将监控文件及子文件夹）", "提醒", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    IOHelper.Instance.DeleteFiles(files);
                    IOHelper.Instance.DeleteDirectories(directories);
                    return true;
                }
                else
                    return null;
            }
            return true;
        }

        private void ExecuteAddMonitorCommand()
        {
            Messenger.Default.Send<string>("ShowAddMonitorView");
            //string selectedPath = IOHelper.Instance.SelectFloder(@"请选择监控文件夹目录");
            //if (string.IsNullOrEmpty(selectedPath))
            //{
            //    MessageBox.Show("未选中任何文件夹！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}
            //if (IOHelper.Instance.IsConflict(selectedPath, ExceptionSavePath))
            //{
            //    MessageBox.Show("所选文件夹与发送异常转存路径冲突！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //else
            //{
            //    if (MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == selectedPath) == null)
            //    {
            //        MonitorCollection.Add(new MonitorModel() { MonitorDirectory = selectedPath, DeleteFiles = true });
            //        ConfigHelper.Instance.SaveSettings();
            //    }
            //    else
            //        MessageBox.Show("所选文件夹已在监控目录中！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
        }

        private void ExecuteDeleteMonitorSettingCommand(object deleteItem)
        {
            var monitorModel = deleteItem as MonitorModel;
            if (monitorModel == null) return;
            //检查发送标志位（若为true则不允许删除配置）
            if (!_monitorFlag)
            {
                MessageBox.Show("当前正在监控文件夹，不允许删除任何监控配置项！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //删除监控配置
            MonitorCollection.Remove(monitorModel);
            ConfigHelper.Instance.SaveSettings();
            if (monitorModel.SubscribeInfos == null || monitorModel.SubscribeInfos.Count == 0) return;
            //删除监控配置后通知相关订阅方，删除相关配置
            Task.Factory.StartNew(() =>
            {
                foreach (var subscribeInfo in monitorModel.SubscribeInfos)
                {
                    SynchronousSocketManager.Instance.SendDeleteMonitorInfo(UtilHelper.Instance.GetIPEndPoint(subscribeInfo.SubscribeIP), monitorModel.MonitorDirectory);
                }
            });
        }

        private void ExecuteDeleteSubscribeSettingCommand(object deleteItem)
        {
            var subscribeModel = deleteItem as SubscribeModel;
            if (subscribeModel == null) return;
            //检查接收标志位（若为true则不允许删除配置）
            if (SynchronousSocketManager.Instance.ReceivingFlag)
            {
                MessageBox.Show("当前正在接收，不允许删除任何接收配置项！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //删除接收配置
            SubscribeCollection.Remove(subscribeModel);
            ConfigHelper.Instance.SaveSettings();
            //删除接收配置后，综合接收配置决定是否通知监控端删除订阅信息
            Task.Factory.StartNew(() =>
            {
                if (SubscribeCollection.FirstOrDefault(s => s.MonitorIP == subscribeModel.MonitorIP && s.MonitorDirectory == subscribeModel.MonitorDirectory) == null)
                    SynchronousSocketManager.Instance.SendUnregisterSubscribeInfo(UtilHelper.Instance.GetIPEndPoint(string.Format("{0}:{1}", subscribeModel.MonitorIP, subscribeModel.MonitorListenPort)), subscribeModel.MonitorDirectory);
            });
        }

        private void ExecuteQueryLogsCommand()
        {
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
                ExceptionSavePath = ConfigHelper.Instance.ExceptionSavePath;
                //订阅事件
                //SynchronousSocketManager.Instance.SendFileProgress += ShowSendProgress;
                //SynchronousSocketManager.Instance.AcceptFileProgress += ShowAcceptProgress;
                //SynchronousSocketManager.Instance.CompleteSendFile += ShowCompleteSendFile;
                //SynchronousSocketManager.Instance.CompleteAcceptFile += ShowCompleteAcceptFile;
                _logger.Info("主窗体加载完毕!");
            });
            t.ContinueWith(v =>
            {
                SynchronousSocketManager.Instance.StartListening(ListenPort);
                //连接监测定时器初始化
                InitialCheckConnectTimer();
                //通知监控端订阅端上线
                NotifyOnlineOffline();
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
            var monitorModel = MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == monitor);
            if (monitorModel == null) return;
            monitorModel.SubscribeInfos.ToList().ForEach(s =>
            {
                s.TransferFileName = @"";
                s.TransferPercent = 0.0;
            });
        }

        public void ShowAcceptProgress(string monitorIp, string monitorDirectory, string acceptDirectory, string receiveFile, double progress)
        {
            var subscribeModel = SubscribeCollection.FirstOrDefault(s => s.MonitorIP == monitorIp && s.MonitorDirectory == monitorDirectory && s.AcceptDirectory == acceptDirectory);
            if (subscribeModel == null) return;
            subscribeModel.AcceptFileName = receiveFile;
            subscribeModel.AcceptFilePercent = progress;
        }

        public void ShowSendProgress(string monitor, string remote, string sendFile, double progerss)
        {
            var monitorModel = MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == monitor);
            if (monitorModel == null) return;
            monitorModel.SubscribeInfos.Where(s => s.SubscribeIP == remote).ToList().ForEach(s =>
            {
                s.TransferFileName = sendFile;
                s.TransferPercent = progerss;
            });
        }

        private void ExecuteClosedCommand()
        {
            SaveSettings();
            DisposeCheckConnectTimer();
            //通知监控端订阅端下线
            NotifyOnlineOffline(false);
            SynchronousSocketManager.Instance.StopListening();
            _logger.Info("主窗体卸载完毕!");
        }

        private void SaveSettings()
        {
            var monitors = MonitorCollection.ToList();
            var subscribes = SubscribeCollection.ToList();
            ConfigHelper.Instance.SaveSettings(monitors, subscribes, ListenPort, ScanPeriod, ExceptionSavePath);
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
                    MessageBox.Show("当前监听端口正在接收信息！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                SynchronousSocketManager.Instance.StopListening();
                CanSetListenPort = true;
            }
        }

        private void ExecuteSetSendExceptionCommand()
        {
            var dlg = new FolderBrowserDialog();
            dlg.Description = @"请选择转存文件夹目录";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = dlg.SelectedPath;
                //TODO:判断转存文件夹和监控文件夹是否有冲突
                foreach (var monitor in MonitorCollection)
                {
                    if (IOHelper.Instance.IsConflict(monitor.MonitorDirectory, selectedPath))
                    {
                        MessageBox.Show("所选文件夹与监控目录冲突！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                ExceptionSavePath = selectedPath;
            }
        }

        #endregion

        #region 公共方法
        public void CompleteMonitorSetting(string monitorDirectory, string subscribeIP)
        {
            var monitor = MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == monitorDirectory);
            if (monitor == null) return;
            if (monitor.SubscribeInfos == null)
                monitor.SubscribeInfos = new ObservableCollection<SubscribeInfoModel>();
            if (monitor.SubscribeInfos.Where(s => s.SubscribeIP == subscribeIP).Count() > 0) return;
            SubscribeInfoModel infoModel = new SubscribeInfoModel() { SubscribeIP = subscribeIP, CanConnect = true };
            var collection = new ObservableCollection<SubscribeInfoModel>();
            foreach (var subscribe in monitor.SubscribeInfos)
            {
                collection.Add(subscribe);
            }
            collection.Add(infoModel);
            monitor.SubscribeInfos = collection;
            ConfigHelper.Instance.SaveSettings();
        }

        public void RemoveMonitorSetting(string monitorDirectory, string subscribeIP)
        {
            var monitor = MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == monitorDirectory);
            if (monitor == null) return;
            if (monitor.SubscribeInfos == null || monitor.SubscribeInfos.Count == 0) return;
            var collection = new ObservableCollection<SubscribeInfoModel>();
            foreach (var s in monitor.SubscribeInfos)
            {
                collection.Add(s);
            }
            var subscribes = monitor.SubscribeInfos.Where(s => s.SubscribeIP == subscribeIP).ToList();
            foreach (var s in subscribes)
            {
                collection.Remove(s);
            }
            monitor.SubscribeInfos = collection;
            ConfigHelper.Instance.SaveSettings();
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
            ConfigHelper.Instance.SaveSettings();
        }

        public void RefreshConnectStatus(string monitorDirectory, string subscribeIP, bool online = true)
        {
            var monitor = MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == monitorDirectory);
            if (monitor == null) return;
            if (monitor.SubscribeInfos == null || monitor.SubscribeInfos.Count == 0) return;
            var subscribeInfo = monitor.SubscribeInfos.FirstOrDefault(s => s.SubscribeIP == subscribeIP);
            if (subscribeInfo == null) return;
            subscribeInfo.CanConnect = online;
            var collection = monitor.SubscribeInfos;
            monitor.SubscribeInfos = collection;
        }
        #endregion

        #region 检测与订阅端连接，通知监控端上线/下线
        private void InitialCheckConnectTimer()
        {
            CheckRemoteConnect();
            _checkConnectTimer = new System.Timers.Timer();
            _checkConnectTimer.Interval = 10000;
            _checkConnectTimer.Elapsed += _checkConnectTimer_Elapsed;
            _checkConnectTimer.Start();
            _logger.Info(string.Format("启动检测远端连接状态的定时起，定时周期为{0}毫秒", _checkConnectTimer.Interval));
        }

        private void DisposeCheckConnectTimer()
        {
            if (_checkConnectTimer != null)
            {
                _checkConnectTimer.Stop();
                _checkConnectTimer.Elapsed -= _checkConnectTimer_Elapsed;
            }
            _checkConnectTimer = null;
        }

        private void _checkConnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _checkConnectTimer.Stop();
            try
            {
                CheckRemoteConnect();
            }
            catch (Exception exception)
            {
                string msg = string.Format("定时检测远端连接状态时出现异常，异常为：{0}", exception.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
            }
            finally
            {
                _checkConnectTimer.Start();
            }
        }

        private void CheckRemoteConnect()
        {
            List<MonitorModel> monitors = new List<MonitorModel>();
            lock (MonitorCollection)
            {
                foreach (var m in MonitorCollection)
                {
                    monitors.Add(m);
                }
            }
            foreach (var monitor in monitors)
            {
                if (monitor.SubscribeInfos == null || monitor.SubscribeInfos.Count == 0) continue;
                foreach (var subscribeInfo in monitor.SubscribeInfos)
                {
                    if (string.IsNullOrEmpty(subscribeInfo.SubscribeIP)) continue;
                    if (SynchronousSocketManager.Instance.CanConnectRemote(UtilHelper.Instance.GetIPEndPoint(subscribeInfo.SubscribeIP)))
                        subscribeInfo.CanConnect = true;
                    else
                        subscribeInfo.CanConnect = false;
                }
                //刷新前端界面
                var info = monitor.SubscribeInfos;
                monitor.SubscribeInfos = info;
            }
            RaisePropertyChanged("MonitorCollection");
        }

        private void NotifyOnlineOffline(bool online = true)
        {
            var collection = SubscribeCollection.Select(s => string.Format("{0}:{1}|{2}", s.MonitorIP, s.MonitorListenPort, s.MonitorDirectory)).ToList().Distinct();
            foreach (var str in collection)
            {
                string[] strArray = str.Split(new char[] { '|' });
                if (strArray.Length != 2) continue;
                string monitorIP = strArray[0];
                string monitorDirectory = strArray[1];
                SynchronousSocketManager.Instance.SendOnlineOfflineInfo(UtilHelper.Instance.GetIPEndPoint(monitorIP), monitorDirectory, online);
            }
        }
        #endregion

        #endregion
    }
}
