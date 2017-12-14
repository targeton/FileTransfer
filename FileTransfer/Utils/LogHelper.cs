using log4net;
using System;
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
    }
}
