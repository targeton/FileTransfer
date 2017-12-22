using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Sockets
{
    public class SendCheckConnect : SendProcess
    {
        #region 构造函数
        public SendCheckConnect(string headMsg)
            : base(headMsg)
        { }
        #endregion

        #region 方法
        protected override object SendParams(IPEndPoint remote, object[] param)
        {
            byte[] receiveBytes = new byte[16];
            int byteRec = _client.Receive(receiveBytes, 16, SocketFlags.None);
            string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
            if (msg == @"$HCR#")
                return true;
            else
                return false;
        }
        #endregion

    }
}
