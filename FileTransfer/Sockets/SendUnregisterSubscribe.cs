using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Sockets
{
    public class SendUnregisterSubscribe : SendProcess
    {
        #region 构造函数
        public SendUnregisterSubscribe(string headMsg)
            : base(headMsg)
        { }
        #endregion

        #region 方法
        protected override object SendParams(System.Net.IPEndPoint remote, object[] param)
        {
            if (param == null || param.Length != 2) return null;
            var monitorAlias = (string)param[0];
            var localListenPort = (int)param[1];
            //发送本地接收端口和监控文件夹
            byte[] directoryBytes = Encoding.Unicode.GetBytes(monitorAlias);
            int byteLength = 8 + directoryBytes.Length;
            byte[] sendBytes = new byte[byteLength];
            byte[] portBytes = BitConverter.GetBytes(localListenPort);
            portBytes.CopyTo(sendBytes, 0);
            BitConverter.GetBytes(directoryBytes.Length).CopyTo(sendBytes, 4);
            directoryBytes.CopyTo(sendBytes, 8);
            _client.Send(sendBytes, 0, byteLength, SocketFlags.None);
            //接收返回信息
            byte[] receiveBytes = new byte[16];
            int byteRec = _client.Receive(receiveBytes, 16, SocketFlags.None);
            string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
            if (msg != "$DSK#")
            {
                string logMsg = string.Format("向{0}发送注销订阅信息后接收的反馈消息头异常（值：{1}，与$DSK#不符）！", remote, msg);
                _logger.Warn(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
            }
            return null;
        }
        #endregion

    }
}
