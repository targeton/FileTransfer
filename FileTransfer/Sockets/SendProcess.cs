using FileTransfer.LogToDb;
using log4net;
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
    public class SendProcess
    {
        #region 变量
        private string _headMsg = string.Empty;
        //private const int CONNECTED_WAITTIME = 50;
        private const int SOCKET_SEND_TIMEOUT = 0;
        private const int SOCKET_RECEIVE_TIMEOUT = 0;
        protected ILog _logger;
        protected Socket _client = null;
        #endregion

        public SendProcess(string headMsg)
        {
            _headMsg = headMsg;
            _logger = LogManager.GetLogger(this.GetType());
        }

        #region 方法
        //子类实现具体的发送内容
        protected virtual object SendParams(IPEndPoint remote, object[] param)
        {
            return null;
        }

        public object SendToServer(IPEndPoint remote, object[] param)
        {
            object feedback = null;
            try
            {
                if (TryConnectRemote(remote))
                {
                    SendHeadMsg();
                    feedback = SendParams(remote, param);
                }
            }
            catch (SocketException se)
            {
                string logMsg = string.Format("{0}与远端{1}连接过程中发生套接字异常！异常：{2}", this.GetType().ToString(), remote, se.Message);
                _logger.Error(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new DbHelper.Entitys.ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
                logMsg.RefreshUINotifyText();
            }
            catch (Exception e)
            {
                string logMsg = string.Format("{0}与远端{1}连接过程中发生异常！异常：{2}", this.GetType().ToString(), remote, e.Message);
                _logger.Error(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new DbHelper.Entitys.ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
                logMsg.RefreshUINotifyText();
            }
            finally
            {
                //断开Socket连接
                DisconnectSocket();
            }
            return feedback;
        }

        private bool TryConnectRemote(IPEndPoint remote)
        {
            var synchronousSocket = new SynchronousSocket();
            _client = synchronousSocket.StartConnecting(remote);
            if (_client == null)
            {
                string logMsg = string.Format("{0}向远端{1}发起连接失败！", this.GetType().ToString(), remote);
                _logger.Warn(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new DbHelper.Entitys.ErrorLogEntity(DateTime.Now, "WARN", logMsg));
                logMsg.RefreshUINotifyText();
                return false;
            }
            return true;
        }

        private void SendHeadMsg()
        {
            //设置Timeout
            _client.SendTimeout = SOCKET_SEND_TIMEOUT;
            _client.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
            //发送消息头
            byte[] sendBytes = new byte[16];
            byte[] headBytes = Encoding.Unicode.GetBytes(_headMsg);
            headBytes.CopyTo(sendBytes, 0);
            _client.Send(sendBytes, 0, 16, SocketFlags.None);
        }

        private void DisconnectSocket()
        {
            if (_client == null) return;
            try
            {
                _client.Shutdown(SocketShutdown.Both);
                _client.Disconnect(false);
                _client = null;
            }
            catch (SocketException se)
            {
                string msg = string.Format("Socket进行DisConnect操作时，发生套接字异常！ErrorCode：{0}", se.ErrorCode);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new DbHelper.Entitys.ErrorLogEntity(DateTime.Now, "ERROR", msg));
                msg.RefreshUINotifyText();
            }
            catch (Exception e)
            {
                string msg = string.Format("Socket进行DisConnect操作时，发生异常！异常：{0}", e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new DbHelper.Entitys.ErrorLogEntity(DateTime.Now, "ERROR", msg));
                msg.RefreshUINotifyText();
            }
            finally
            {
                _client = null;
            }
        }


        #endregion
    }
}
