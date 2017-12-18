using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer
{
    public class LogHelper
    {
        #region 变量
        private ILog _logger = LogManager.GetLogger("LogToSqlite");
        private ILog _sendLogger = LogManager.GetLogger("SendLogToSqlite");
        private ILog _receiveLogger = LogManager.GetLogger("ReceiveLogToSqlite");
        private ILog _monitorLogger = LogManager.GetLogger("MonitorLogToSqlite");
        private Task _logTask = null;
        private ConcurrentQueue<object> _logsQueue = new ConcurrentQueue<object>();
        #endregion

        #region 单例
        private static LogHelper _instance = new LogHelper();
        public static LogHelper Instance
        {
            get { return _instance; }
        }
        #endregion

        #region 属性
        public ILog Logger { get { return _logger; } }
        public ILog SendLogger { get { return _sendLogger; } }
        public ILog ReceiveLogger { get { return _receiveLogger; } }
        public ILog MonitorLogger { get { return _monitorLogger; } }
        #endregion

        #region 方法
        public void AddLog(object log)
        {
            _logsQueue.Enqueue(log);
            if (_logTask == null || _logTask.IsCompleted == true)
            {
                _logTask = Task.Factory.StartNew(() =>
                {
                    while (_logsQueue.Count > 0)
                    {
                        object insertLog = null;
                        if (!_logsQueue.TryDequeue(out insertLog))
                            continue;
                        if (insertLog == null)
                            continue;
                        switch (insertLog.GetType().ToString())
                        {
                            case "FileTransfer.Models.SendLogEntity":
                                ThreadContext.Properties["SendDate"] = ((FileTransfer.Models.SendLogEntity)insertLog).SendDate;
                                _sendLogger.Info(insertLog);
                                break;
                            case "FileTransfer.Models.ReceiveLogEntity":
                                ThreadContext.Properties["ReceiveDate"] = ((FileTransfer.Models.ReceiveLogEntity)insertLog).ReceiveDate;
                                _receiveLogger.Info(insertLog);
                                break;
                            case "FileTransfer.Models.MonitorLogEntity":
                                ThreadContext.Properties["MonitorDate"] = ((FileTransfer.Models.MonitorLogEntity)insertLog).MonitorDate;
                                _monitorLogger.Info(insertLog);
                                break;
                            case "FileTransfer.Models.ErrorLogEntity":
                                _logger.Info(insertLog);
                                break;
                            default:
                                break;
                        }
                    }
                });
            }
        }
        #endregion


    }
}
