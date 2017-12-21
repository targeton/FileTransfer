using FileTransfer.DbHelper.Entitys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.LogToDb
{
    public class LogHelper
    {
        #region 变量
        private SendLogger _sendLogger = new SendLogger();
        private ReceiveLogger _receiveLogger = new ReceiveLogger();
        private MonitorLogger _monitorLogger = new MonitorLogger();
        private ErrorLogger _errorLogger = new ErrorLogger();
        #endregion

        #region 事件
        public Action<IEnumerable<SendLogEntity>> NotifyInsertSendLogs;
        public Action<IEnumerable<ReceiveLogEntity>> NotifyInsertReceiveLogs;
        public Action<IEnumerable<MonitorLogEntity>> NotifyInsertMonitorLogs;
        public Action<IEnumerable<ErrorLogEntity>> NotifyInsertErrorLogs;
        #endregion

        #region 单例
        private static LogHelper _instance = new LogHelper();
        public static LogHelper Instance
        {
            get { return _instance; }
        }
        #endregion

        #region 属性
        public SendLogger SendLogger { get { return _sendLogger; } }
        public ReceiveLogger ReceiveLogger { get { return _receiveLogger; } }
        public MonitorLogger MonitorLogger { get { return _monitorLogger; } }
        public ErrorLogger ErrorLogger { get { return _errorLogger; } }
        #endregion

        #region 构造函数
        public LogHelper()
        {
            SendLogger.NotifyInsertRows = NotifyInsertSends;
            ReceiveLogger.NotifyInsertRows = NotifyInsertReceives;
            MonitorLogger.NotifyInsertRows = NotifyInsertMonitors;
            ErrorLogger.NotifyInsertRows = NotifyInsertErrors;
        }

        private void NotifyInsertSends(IEnumerable<SendLogEntity> items)
        {
            if (NotifyInsertSendLogs != null)
                NotifyInsertSendLogs(items);
        }

        private void NotifyInsertReceives(IEnumerable<ReceiveLogEntity> items)
        {
            if (NotifyInsertReceiveLogs != null)
                NotifyInsertReceiveLogs(items);
        }

        private void NotifyInsertMonitors(IEnumerable<MonitorLogEntity> items)
        {
            if (NotifyInsertMonitorLogs != null)
                NotifyInsertMonitorLogs(items);
        }

        private void NotifyInsertErrors(IEnumerable<ErrorLogEntity> items)
        {
            if (NotifyInsertErrorLogs != null)
                NotifyInsertErrorLogs(items);
        }

        #endregion

    }
}
