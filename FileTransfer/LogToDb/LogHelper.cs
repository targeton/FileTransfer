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

        }
        #endregion

    }
}
