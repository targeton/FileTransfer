using FileTransfer.Models;
using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using FileTransfer.LogToDb;
using FileTransfer.DbHelper.Entitys;
using FileTransfer.IO;

namespace FileTransfer.FileWatcher
{
    public class FileWatcherHelper
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(FileWatcherHelper));
        //保存文件增量发送的类
        private List<SendFileProcess> _sendFileProcessList = new List<SendFileProcess>();
        //记录监控文件夹发生的文件信息变化
        private Dictionary<string, List<string>> _monitorAliasChanges;
        //定时器，用于定时监控
        private Timer _timer;
        #endregion

        #region 属性

        #endregion

        #region 单例
        private static FileWatcherHelper _instance;
        public static FileWatcherHelper Instance
        {
            get { return _instance ?? (_instance = new FileWatcherHelper()); }
        }
        #endregion

        #region 事件
        public delegate void NotifyMonitorChangesEventHandle(List<MonitorChanges> increments);
        public NotifyMonitorChangesEventHandle NotifyMonitorChanges;
        #endregion

        #region 方法
        public void StartMoniter(bool ignore = true)
        {
            //初始化监控文件夹的文件信息
            InitialMonitorChanges(ignore);
            //初始化定时器并启动
            _timer = new Timer();
            _timer.Interval = SimpleIoc.Default.GetInstance<MainViewModel>().ScanPeriod * 1000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            _logger.Info(string.Format("启动监控文件夹的扫描定时器，定时刷新时间间隔为{0}毫秒", _timer.Interval));
        }

        private void InitialMonitorChanges(bool ignore = true)
        {
            var monitors = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.ToList();
            //var monitorAlias = monitors.Select(m => m.MonitorAlias).Distinct().ToList();
            _monitorAliasChanges = new Dictionary<string, List<string>>();
            foreach (var monitor in monitors)
            {
                //IOHelper中配置各监控文件夹删除文件和删除子文件夹的配置
                bool deleteFile = monitor.DeleteFiles;
                bool deleteSubdirectory = monitor.DeleteSubdirectory;
                IOHelper.Instance.SetDeleteSetting(monitor.MonitorAlias, deleteFile, deleteSubdirectory);
                //获取监控文件夹内的初始文件状态(根据ignore决定是否监控原有文件，默认不监控)
                if (ignore)
                {
                    List<string> files = IOHelper.Instance.GetAllFiles(monitor.MonitorDirectory);
                    if (files == null || files.Count <= 0)
                        files = new List<string>();
                    _monitorAliasChanges.Add(monitor.MonitorAlias, files);
                }
                else
                {
                    _monitorAliasChanges.Add(monitor.MonitorAlias, new List<string>());
                }
            }
        }

        public void StopMoniter()
        {
            //关闭定时器
            if (_timer == null) return;
            _timer.Stop();
            _timer.Elapsed -= _timer_Elapsed;
            _timer = null;
            _logger.Info(string.Format("关闭监控文件夹的扫描定时器"));
        }

        //定时处理任务
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PauseMonitor();
            List<string> keyList = _monitorAliasChanges.Keys.ToList();
            List<MonitorChanges> changes = new List<MonitorChanges>();
            foreach (string monitorAlias in keyList)
            {
                var monitor = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.FirstOrDefault(m => m.MonitorAlias == monitorAlias);
                if (monitor == null) return;
                var monitorDirectory = monitor.MonitorDirectory;
                if (!IOHelper.Instance.HasDirectory(monitorDirectory))
                    continue;
                //获取现有文件信息
                List<string> nowFiles = IOHelper.Instance.GetAllFiles(monitorDirectory);
                if (nowFiles == null) continue;
                //获取原有文件信息
                List<string> oldFiles = _monitorAliasChanges[monitorAlias];
                //相比之前文件信息集的增量
                List<string> incrementFiles = nowFiles.Except(oldFiles).ToList();
                //记录被其他线程占用的文件信息
                List<string> usedIncrementFiles = new List<string>();
                foreach (var increment in incrementFiles)
                {
                    if (TryAccessFile(increment)) continue;
                    usedIncrementFiles.Add(increment);
                }
                //更新：nowFiles记录当前未被占用的文件集，incrementFiles记录当前未被占用的增量集
                nowFiles = nowFiles.Except(usedIncrementFiles).ToList();
                incrementFiles = incrementFiles.Except(usedIncrementFiles).ToList();
                //记录现在监控文件夹内的信息
                _monitorAliasChanges[monitorAlias] = nowFiles;
                //如果没有增量，则继续遍历
                if (incrementFiles == null || incrementFiles.Count <= 0)
                    continue;
                string infoMsg = string.Format("监控文件夹{0}内新增{1}个文件", monitorAlias, incrementFiles.Count);
                _logger.Info(infoMsg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "INFO", infoMsg));
                DateTime monitorTime = DateTime.Now;
                foreach (var file in incrementFiles)
                {
                    LogHelper.Instance.MonitorLogger.Add(new MonitorLogEntity(monitorTime, file));
                }
                //获取当前监控文件夹是否有订阅者(有订阅则记录文件夹内改变)
                if (monitor.SubscribeInfos == null || monitor.SubscribeInfos.Count == 0) continue;
                changes.Add(new MonitorChanges() { MonitorAlias = monitorAlias, MonitorDirectory = monitorDirectory, FileChanges = incrementFiles });
            }
            if (changes != null && changes.Count > 0)
                ProcessChanges(changes);
            RecoverMonitor();
        }

        private bool TryAccessFile(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                { }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void PauseMonitor()
        {
            if (_timer == null) return;
            _timer.Stop();
        }

        public void RecoverMonitor()
        {
            if (_timer == null) return;
            _timer.Start();
        }

        private void ProcessChanges(List<MonitorChanges> changes)
        {
            foreach (var change in changes)
            {
                var process = _sendFileProcessList.FirstOrDefault(p => p.MonitorAlias == change.MonitorAlias && p.MonitorDirectory == change.MonitorDirectory);
                if (process == null)
                {
                    process = new SendFileProcess(change.MonitorAlias, change.MonitorDirectory);
                    _sendFileProcessList.Add(process);
                }
                process.Add(change.FileChanges);
            }
        }

        #endregion
    }
}
