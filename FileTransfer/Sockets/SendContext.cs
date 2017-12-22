using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Sockets
{
    public class SendContext
    {
        #region 变量
        private SendProcess _process = null;
        #endregion

        #region 构造函数
        public SendContext(string headMsg)
        {
            switch (headMsg)
            {
                //Request Monitor Floders
                case "$RMF#":
                    _process = new SendRequestMonitor(headMsg);
                    break;
                //
                case "$RFS#":
                    _process = new SendSubscribeInfo(headMsg);
                    break;
                case "$BTF#":
                    _process = new SendFiles(headMsg);
                    break;
                //Delete Monitor Floder
                case "$DMF#":
                    _process = new SendDeleteMonitor(headMsg);
                    break;
                case "$DSF#":
                    _process = new SendUnregisterSubscribe(headMsg);
                    break;
                //Check Connect Remote
                case "$CCR#":
                    _process = new SendCheckConnect(headMsg);
                    break;
                //ON/OffLine
                case "$OFL#":
                    _process = new SendOnlineOffline(headMsg);
                    break;
                default:
                    _process = new SendProcess(headMsg);
                    break;
            }
        }
        #endregion

        #region 方法
        public object ConnectToRemot(IPEndPoint remote, object[] param)
        {
            if (_process == null) return null;
            return _process.SendToServer(remote, param);
        }
        #endregion

    }
}
