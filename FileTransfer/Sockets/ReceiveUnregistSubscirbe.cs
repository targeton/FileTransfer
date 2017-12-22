using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Sockets
{
    public class ReceiveUnregistSubscirbe : ReceiveProcess
    {
        #region 变量

        #endregion

        #region 方法
        public override void SocketPorcess(Socket socket)
        {
            //获取要注销的IP、端口和监控文件夹信息
            string remoteIPStr = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            byte[] receiveBytes = new byte[4];
            int byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
            //string subscribeIp = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            int remotePort = BitConverter.ToInt32(receiveBytes, 0);
            string subscribeIp = string.Format("{0}:{1}", remoteIPStr, remotePort);
            receiveBytes = new byte[4];
            byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
            int directoryLength = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
            receiveBytes = new byte[directoryLength];
            byteRec = socket.Receive(receiveBytes, 0, directoryLength, SocketFlags.None);
            string monitorDirectory = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            //注销本地订阅信息
            SimpleIoc.Default.GetInstance<MainViewModel>().RemoveMonitorSetting(monitorDirectory, subscribeIp);
            //发送断开信息
            byte[] disconnectBytes = new byte[16];
            Encoding.Unicode.GetBytes("$DSK#").CopyTo(disconnectBytes, 0);
            socket.Send(disconnectBytes, 0, 16, SocketFlags.None);
        }
        #endregion
    }
}
