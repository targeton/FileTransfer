using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Sockets
{
    public class SendProcess
    {
        #region 变量
        private Socket _client = null;
        private string _headMsg = string.Empty;
        #endregion

        public SendProcess(string headMsg)
        {
            _headMsg = headMsg;
        }

        #region 方法
        protected virtual void SendData(IPEndPoint remote, object param)
        { }

        public void SendToServer(IPEndPoint remote, object param)
        {
            //TODO：尝试连接
            SendData(remote, param);
        }

        #endregion
    }
}
