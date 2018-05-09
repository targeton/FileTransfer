using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using log4net;
using System;
using System.Net.Sockets;

namespace FileTransfer.Sockets
{
    public class ReceiveProcess
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(ReceiveProcess));
        #endregion

        #region 构造

        #endregion

        #region 方法
        public virtual void SocketPorcess(Socket socket)
        {
            string logMsg = string.Format("套接字所接收的通信字节数据无法转换为有效的消息头！");
            _logger.Warn(logMsg);
            LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
        }
        #endregion

    }
}
