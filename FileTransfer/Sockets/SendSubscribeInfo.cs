using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileTransfer.Sockets
{
    public class SendSubscribeInfo : SendProcess
    {
        #region 构造函数
        public SendSubscribeInfo(string headMsg)
            : base(headMsg)
        { }
        #endregion

        #region 方法
        protected override object SendParams(IPEndPoint remote, object[] param)
        {
            if (param == null || param.Length != 2) return null;
            var localListenPort = (int)param[0];
            var monitorAlias = (string)param[1];
            byte[] sendBytes = new byte[4];
            byte[] portBytes = BitConverter.GetBytes(localListenPort);
            portBytes.CopyTo(sendBytes, 0);
            _client.Send(sendBytes, 0, 4, SocketFlags.None);
            //发送订阅的监控文件夹
            byte[] floderBytes = Encoding.Unicode.GetBytes(monitorAlias);
            sendBytes = new byte[4];
            BitConverter.GetBytes(floderBytes.Length).CopyTo(sendBytes, 0);
            _client.Send(sendBytes, 0, 4, SocketFlags.None);
            _client.Send(floderBytes, 0, floderBytes.Length, SocketFlags.None);
            sendBytes = new byte[16];
            Encoding.Unicode.GetBytes(@"$EOF#").CopyTo(sendBytes, 0);
            _client.Send(sendBytes, 0, 16, SocketFlags.None);
            //接收返回信息
            byte[] receiveBytes = new byte[16];
            int byteRec = _client.Receive(receiveBytes, 16, SocketFlags.None);
            string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
            if (msg != "$DSK#")
            {
                string logMsg = string.Format("发送订阅信息后接收的反馈消息头异常（值：{0}，与$DSK#不符）！", msg);
                _logger.Warn(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
            }
            return null;
        }
        #endregion
    }
}
