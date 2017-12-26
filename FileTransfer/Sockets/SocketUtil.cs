using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace FileTransfer.Sockets
{
    public static class SocketUtil
    {
        public static void CloseSocket(this Socket socket)
        {
            if (socket == null) return;
            socket.Close();
            socket = null;
        }

        public static void RefreshUINotifyText(this string notify)
        {
            if (SimpleIoc.Default.IsRegistered<MainViewModel>())
                SimpleIoc.Default.GetInstance<MainViewModel>().NotifyText = notify;
        }

        public static string GetHeadMsg(this Socket socket, int size)
        {
            byte[] msgBytes = new byte[size];
            int byteRec = socket.Receive(msgBytes, 0, size, SocketFlags.None);
            return Encoding.Unicode.GetString(msgBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
        }

    }
}
