using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileTransfer.Sockets
{
    public class SendRequestMonitor : SendProcess
    {
        #region 构造函数
        public SendRequestMonitor(string headMsg)
            : base(headMsg)
        { }
        #endregion

        #region 方法
        protected override object SendParams(IPEndPoint remote, object[] param)
        {
            return GetMonitorFolders();
        }

        private List<string> GetMonitorFolders()
        {
            List<String> moniterFloders = new List<string>();
            byte[] receiveBytes = new byte[4];
            int byteRec = _client.Receive(receiveBytes, 0, 4, SocketFlags.None);
            int flodersNum = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
            int index = 0;
            while (index < flodersNum)
            {
                receiveBytes = new byte[4];
                byteRec = _client.Receive(receiveBytes, 0, 4, SocketFlags.None);
                int receiveNum = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
                receiveBytes = new byte[receiveNum];
                byteRec = _client.Receive(receiveBytes, 0, receiveNum, SocketFlags.None);
                string floder = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec);
                moniterFloders.Add(floder);
                index++;
                Thread.Sleep(1);
            }
            //接收结束标志
            receiveBytes = new byte[16];
            byteRec = _client.Receive(receiveBytes, 0, 16, SocketFlags.None);
            string endStr = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            if (endStr != @"$EOF#")
            {
                string msg = string.Format("完成监控文件夹信息的接收后接收的反馈消息头异常（$EOF#）!");
                _logger.Warn(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", msg));
            }
            return moniterFloders;
        }

        #endregion
    }
}
