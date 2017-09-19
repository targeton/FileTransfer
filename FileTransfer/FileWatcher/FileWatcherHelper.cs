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

namespace FileTransfer.FileWatcher
{
    class FileWatcherHelper
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(FileWatcherHelper));
        //记录监控文件夹发生的文件信息变化
        private Dictionary<string, List<string>> _monitorDirectoryChanges;
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
        public Action<string, List<string>> NotifyMonitorIncrement;
        #endregion

        #region 方法
        public void StartMoniter()
        {
            //初始化监控文件夹的文件信息
            InitialMonitorChanges();
            //初始化定时器并启动
            _timer = new Timer();
            _timer.Interval = 5000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            _logger.Info(string.Format("启动监控文件夹的扫描定时器，定时刷新时间间隔为{0}毫秒", _timer.Interval));
        }

        private void InitialMonitorChanges()
        {
            _monitorDirectoryChanges = new Dictionary<string, List<string>>();
            List<string> monitorDirectories = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.Where(m => !string.IsNullOrEmpty(m.SubscribeIP)).Select(m => m.MonitorDirectory).Distinct().ToList();
            foreach (var monitorDirectory in monitorDirectories)
            {
                List<string> files = IOHelper.Instance.GetAllFiles(monitorDirectory);
                if (files == null || files.Count <= 0)
                    files = new List<string>();
                _monitorDirectoryChanges.Add(monitorDirectory, files);
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
            List<string> keyList = _monitorDirectoryChanges.Keys.ToList();
            foreach (string monitorDirectory in keyList)
            {
                if (!IOHelper.Instance.HasMonitorDirectory(monitorDirectory))
                    continue;
                List<string> nowFiles = IOHelper.Instance.GetAllFiles(monitorDirectory);
                //相比之前文件信息集的增量
                List<string> incrementFiles = nowFiles.Except(_monitorDirectoryChanges[monitorDirectory]).ToList();
                //记录现在监控文件夹内的信息
                _monitorDirectoryChanges[monitorDirectory] = nowFiles;
                //如果没有增量，则继续遍历
                if (incrementFiles == null || incrementFiles.Count <= 0)
                    continue;
                _logger.Info(string.Format("监控文件夹{0}内新增{1}个文件", monitorDirectory, incrementFiles.Count));
                //通知注册事件的类处理增量信息
                if (NotifyMonitorIncrement != null)
                    NotifyMonitorIncrement(monitorDirectory, incrementFiles);
            }
        }

        public void AddNewMonitor(string monitorDirectory)
        {
            _timer.Stop();
            if (!_monitorDirectoryChanges.Keys.Contains(monitorDirectory))
            {
                List<string> files = IOHelper.Instance.GetAllFiles(monitorDirectory);
                _monitorDirectoryChanges.Add(monitorDirectory, files);
                _logger.Info(string.Format("新增监控文件夹{0}(已被订阅)", monitorDirectory));
            }
            _timer.Start();
        }

        #endregion
    }
}
