using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileTransfer.Sockets
{
    public class ReceiveDeleteMonitor : ReceiveProcess
    {
        #region 变量

        #endregion

        #region 方法
        public override void SocketPorcess(Socket socket)
        {
            //获取远端发送方的IP信息
            //byte[] receiveBytes = new byte[32];
            //int byteRec = socket.Receive(receiveBytes, 0, 32, SocketFlags.None);
            //string monitorIp = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            string monitorIp = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            //获取监控文件夹信息
            byte[] receiveBytes = new byte[4];
            int byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
            int floderLength = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
            receiveBytes = new byte[floderLength];
            byteRec = socket.Receive(receiveBytes, 0, floderLength, SocketFlags.None);
            string monitorAlias = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            //删除本地对应监控的接收配置
            SimpleIoc.Default.GetInstance<MainViewModel>().RemoveAcceptSettings(monitorIp, monitorAlias);
            //发送断开信息
            byte[] disconnectBytes = new byte[16];
            Encoding.Unicode.GetBytes("$DSK#").CopyTo(disconnectBytes, 0);
            socket.Send(disconnectBytes, 0, 16, SocketFlags.None);
        }
        #endregion
    }
}
