using FileTransfer.Configs;
using FileTransfer.FileWatcher;
using FileTransfer.Models;
using FileTransfer.Sockets;
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
            AddSubscibeCommand = new RelayCommand(ExecuteAddSubscibeCommand);
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
                MonitorCollection.Remove(model1);
            else
            {
                var model2 = deleteItem as SubscribeModel;
                if (model2 == null) return;
                SubscribeCollection.Remove(model2);
            }
        }

        private void ExecuteQueryLogsCommand()
        {

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
                //订阅事件
                FileWatcherHelper.Instance.NotifyMonitorIncrement = SynchronousSocketManager.Instance.SendMonitorChanges;
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
            ConfigHelper.Instance.SaveSettings(MonitorCollection.ToList(), SubscribeCollection.ToList(), ListenPort);
            SynchronousSocketManager.Instance.StopListening();
            _logger.Info("主窗体卸载完毕!");
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
                SynchronousSocketManager.Instance.StopListening();
                CanSetListenPort = true;
            }
        }

        #endregion

        #region 公共方法
        public void CompleteMonitorSetting(string subscribeIP, string monitorDirectory)
        {
            var monitor = MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == monitorDirectory);
            if (monitor == null) return;
            if (string.IsNullOrEmpty(monitor.SubscribeIP))
                monitor.SubscribeIP = subscribeIP;
            else if (monitor.SubscribeIP == subscribeIP)
                return;
            else
            {
                MonitorCollection.Add(new MonitorModel() { MonitorDirectory = monitorDirectory, SubscribeIP = subscribeIP });
                FileWatcherHelper.Instance.AddNewMonitor(monitorDirectory);
            }
        }
        #endregion

        #endregion
    }
}
