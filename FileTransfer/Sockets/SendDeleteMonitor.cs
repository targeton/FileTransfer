using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileTransfer.Sockets
{
    public class SendDeleteMonitor : SendProcess
    {
        #region 构造函数
        public SendDeleteMonitor(string headMsg)
            : base(headMsg)
        { }
        #endregion

        #region 方法
        protected override object SendParams(IPEndPoint remote, object[] param)
        {
            if (param == null || param.Length != 1)
                return null;
            var monitorAlias = (string)param[0];
            byte[] directoryBytes = Encoding.Unicode.GetBytes(monitorAlias);
            //前4位为文件夹byte数组的长度，后面为文件夹byte数组
            byte[] sendBytes = new byte[4 + directoryBytes.Length];
            //Encoding.Unicode.GetBytes(LocalIPv4).CopyTo(sendBytes, 0);
            BitConverter.GetBytes(directoryBytes.Length).CopyTo(sendBytes, 0);
            directoryBytes.CopyTo(sendBytes, 4);
            _client.Send(sendBytes, 0);
            //接收返回信息
            byte[] receiveBytes = new byte[16];
            int byteRec = _client.Receive(receiveBytes, 16, SocketFlags.None);
            string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
            if (msg != "$DSK#")
            {
                string logMsg = string.Format("向{0}发送删除监控文件夹信息后接收的反馈消息头异常（值：{1}，与$DSK#不符）！", remote, msg);
                _logger.Warn(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
            }
            return null;
        }
        #endregion

    }
}
