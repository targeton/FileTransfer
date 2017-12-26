using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileTransfer.Sockets
{
    public class SendOnlineOffline : SendProcess
    {
        #region 构造函数
        public SendOnlineOffline(string headMsg)
            : base(headMsg)
        { }
        #endregion

        #region 方法
        protected override object SendParams(IPEndPoint remote, object[] param)
        {
            if (param == null || param.Length != 3) return null;
            var monitorAlias = (string)param[0];
            var localListentPort = (int)param[1];
            var online = (bool)param[2];
            //发送本地端口和监控文件夹
            byte[] directoryBytes = Encoding.Unicode.GetBytes(monitorAlias);
            int byteLength = 8 + directoryBytes.Length;
            byte[] sendBytes = new byte[byteLength];
            BitConverter.GetBytes(localListentPort).CopyTo(sendBytes, 0);
            BitConverter.GetBytes(directoryBytes.Length).CopyTo(sendBytes, 4);
            directoryBytes.CopyTo(sendBytes, 8);
            _client.Send(sendBytes, 0, byteLength, SocketFlags.None);
            //发送上线的消息头"$ON#"或者下线的消息头"$OFF#"
            if (online)
            {
                sendBytes = new byte[16];
                //on/offline
                byte[] tempBytes = Encoding.Unicode.GetBytes(@"$ON#");
                tempBytes.CopyTo(sendBytes, 0);
                //发送消息头
                _client.Send(sendBytes, 0, 16, SocketFlags.None);
            }
            else
            {
                sendBytes = new byte[16];
                //on/offline
                byte[] tempBytes = Encoding.Unicode.GetBytes(@"$OFF#");
                tempBytes.CopyTo(sendBytes, 0);
                //发送消息头
                _client.Send(sendBytes, 0, 16, SocketFlags.None);
            }
            //接收返回信息
            byte[] receiveBytes = new byte[16];
            int byteRec = _client.Receive(receiveBytes, 16, SocketFlags.None);
            string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
            if (msg != "$DSK#")
            {
                string logMsg = string.Format("向{0}发送上线下线信息后接收的反馈消息头异常（值：{1}，与$DSK#不符）！", remote, msg);
                _logger.Warn(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
            }
            return null;
        }
        #endregion
    }
}
