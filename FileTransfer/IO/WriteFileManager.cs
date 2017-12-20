using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Sockets;

namespace FileTransfer.IO
{
    /// <summary>
    /// 管理类（控制多个WriteFile对象并发向各自文件内进行写入操作）
    /// </summary>
    public class WriteFileManager
    {
        #region 变量
        private ILog _logger = LogManager.GetLogger(typeof(WriteFileManager));
        private List<WriteFile> _writers = null;
        private string _monitorIP;
        private string _monitorDirectory;
        private bool _hasOccurException = false;
        private string _exceptionSavePath;
        #endregion

        #region 构造函数
        public WriteFileManager(string monitorIP, string monitorDirectory)
        {
            _monitorIP = monitorIP;
            _monitorDirectory = monitorDirectory;
            List<string> acceptDirectories = GetAcceptDirectories(monitorIP, monitorDirectory);
            _writers = new List<WriteFile>();
            foreach (var directory in acceptDirectories)
            {
                var writer = new WriteFile(directory, monitorIP, monitorDirectory);
                _writers.Add(writer);
            }
        }

        private void SetExceptionFlag()
        {
            _hasOccurException = true;
        }

        private List<string> GetAcceptDirectories(string monitorIP, string monitorDirectory)
        {
            _exceptionSavePath = SimpleIoc.Default.GetInstance<MainViewModel>().ExceptionSavePath;
            List<string> acceptDirectories = SimpleIoc.Default.GetInstance<MainViewModel>().SubscribeCollection.Where(s => s.MonitorIP == monitorIP && s.MonitorDirectory == monitorDirectory).Select(s => s.AcceptDirectory).ToList();
            if (acceptDirectories.Count == 0)
            {
                _hasOccurException = true;
                string logMsg = string.Format("{0}发送来的文件无接收设置，转存至{1}！", monitorIP, _exceptionSavePath);
                _logger.Warn(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
                string.Format("{0}:{1}发送来的文件无接收设置，转存至{2}！", DateTime.Now, monitorIP, _exceptionSavePath).RefreshUINotifyText();
            }
            acceptDirectories.Add(_exceptionSavePath);
            return acceptDirectories;
        }
        #endregion

        #region 方法
        public void Add(WriteDataBuffer dataBuffer)
        {
            foreach (var writer in _writers)
            {
                if (writer.IsException)
                    continue;
                if (!IOHelper.Instance.HasDirectory(writer.Directory))
                {
                    writer.IsException = true;
                    _hasOccurException = true;
                    string logMsg = string.Format("无法检测到接收文件夹{0},本次接收的后续数据将转存", writer.Directory);
                    _logger.Error(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
                    continue;
                }
                if (writer.Directory == _exceptionSavePath && !_hasOccurException)
                    continue;
                writer.Add(dataBuffer);
            }
        }
        #endregion

    }
}
