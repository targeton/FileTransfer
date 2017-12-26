using log4net;
using System.Net.Sockets;
using System.Text;

namespace FileTransfer.Sockets
{
    public class ReceiveCheckConnect : ReceiveProcess
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(ReceiveCheckConnect));
        #endregion

        #region 方法
        public override void SocketPorcess(Socket socket)
        {
            //发送连接成功的反馈信息(had connected remote)
            byte[] feedbackBytes = new byte[16];
            Encoding.Unicode.GetBytes("$HCR#").CopyTo(feedbackBytes, 0);
            socket.Send(feedbackBytes, 0, 16, SocketFlags.None);
        }
        #endregion
    }
}
