using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Sockets
{
    public class ReceiveSubscribeInfo : ReceiveProcess
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(ReceiveSubscribeInfo));
        #endregion

        #region 方法
        public override void SocketPorcess(Socket socket)
        {
            ////获取订阅者IP
            //byte[] ipBytes = new byte[64];
            //int byteRec = socket.Receive(ipBytes, 64, SocketFlags.None);
            //string ipAddressPort = Encoding.Unicode.GetString(ipBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            //获取订阅者IP和端口
            string ipStr = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            byte[] portBytes = new byte[4];
            int byteRec = socket.Receive(portBytes, 4, SocketFlags.None);
            int remotePort = BitConverter.ToInt32(portBytes, 0);
            string ipAddressPort = string.Format("{0}:{1}", ipStr, remotePort);
            //获取订阅的监控文件夹
            byte[] receiveBytes = new byte[4];
            byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
            int receiveNum = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
            receiveBytes = new byte[receiveNum];
            byteRec = socket.Receive(receiveBytes, 0, receiveNum, SocketFlags.None);
            string monitorDirectory = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec);
            receiveBytes = new byte[16];
            byteRec = socket.Receive(receiveBytes, 0, 16, SocketFlags.None);
            string endStr = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            if (endStr == @"$EOF#")
                SimpleIoc.Default.GetInstance<MainViewModel>().CompleteMonitorSetting(monitorDirectory, ipAddressPort);
            else
            {
                string msg = string.Format("接收{0}的订阅信息后，未能成功接收结束消息头（$EOF#）！", ipAddressPort);
                _logger.Warn(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", msg));
            }
            //发送断开信息
            byte[] disconnectBytes = new byte[16];
            Encoding.Unicode.GetBytes("$DSK#").CopyTo(disconnectBytes, 0);
            socket.Send(disconnectBytes, 0, 16, SocketFlags.None);
        }
        #endregion
    }
}
