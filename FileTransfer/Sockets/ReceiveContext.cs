using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Sockets
{
    public class ReceiveContext
    {
        #region 变量
        ReceiveProcess _process = null;
        #endregion

        #region 构造函数
        public ReceiveContext(string headMsg)
        {
            switch (headMsg)
            {
                //Request Monitor Floders
                case "$RMF#":
                    _process = new ReceiveRequestMonitor();
                    break;
                //
                case "$RFS#":
                    _process = new ReceiveSubscribeInfo();
                    break;
                case "$BTF#":
                    _process = new ReceiveFiles();
                    break;
                //Delete Monitor Floder
                case "$DMF#":
                    _process = new ReceiveDeleteMonitor();
                    break;
                case "$DSF#":
                    _process = new ReceiveUnregeistSubscirbe();
                    break;
                //Check Connect Remote
                case "$CCR#":
                    _process = new ReceiveCheckConnect();
                    break;
                //ON/OffLine
                case "$OFL#":
                    _process = new ReceiveOnlineOffline();
                    break;
                default:
                    _process = new ReceiveProcess();
                    break;
            }
        }
        #endregion

        #region 方法
        public void Process(Socket socket)
        {
            if (_process == null) return;
            _process.SocketPorcess(socket);
        }
        #endregion

    }
}
