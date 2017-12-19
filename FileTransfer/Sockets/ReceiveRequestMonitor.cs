using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileTransfer.Sockets
{
    public class ReceiveRequestMonitor : ReceiveProcess
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(ReceiveRequestMonitor));
        #endregion

        #region 重构函数
        public override void SocketPorcess(Socket socket)
        {
            List<string> floders = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.Select(m => m.MonitorDirectory).ToList();
            try
            {
                ////配置Socket的发送Timeout
                //socket.SendTimeout = SOCKET_SEND_TIMEOUT;
                //先发送总个数
                int floderNum = floders.Count;
                byte[] numBytes = new byte[4];
                BitConverter.GetBytes(floderNum).CopyTo(numBytes, 0);
                socket.Send(numBytes, 0, 4, SocketFlags.None);
                //依次发送监控目录（先发送字符串转换为byte数组的长度，再发送byte数组）
                int index = 0;
                while (index < floderNum)
                {
                    numBytes = new byte[4];
                    byte[] sendBytes = Encoding.Unicode.GetBytes(floders[index]);
                    BitConverter.GetBytes(sendBytes.Length).CopyTo(numBytes, 0);
                    socket.Send(numBytes, 0, 4, SocketFlags.None);
                    socket.Send(sendBytes, 0, sendBytes.Length, SocketFlags.None);
                    index++;
                    Thread.Sleep(1);
                }
                //最后发送结束标志
                byte[] endBytes = new byte[16];
                Encoding.Unicode.GetBytes(@"$EOF#").CopyTo(endBytes, 0);
                socket.Send(endBytes, 0, 16, SocketFlags.None);
            }
            catch (SocketException se)
            {
                string msg = string.Format("发送监控文件夹信息时发生套接字异常！SocketException ErroCode:{0}", se.ErrorCode);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                socket.CloseSocket();
                string.Format("{0}：发送监控文件夹信息时发生套接字异常！", DateTime.Now).RefreshUINotifyText();
            }
            catch (Exception e)
            {
                string msg = string.Format("发送监控文件夹信息时发生异常！异常信息：{0}", e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                socket.CloseSocket();
                string.Format("{0}：发送监控文件夹信息时发生异常！", DateTime.Now).RefreshUINotifyText();
            }
        }
        #endregion

    }
}
