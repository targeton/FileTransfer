using FileTransfer.IO;
using log4net;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FileTransfer.Sockets
{
    public class ReceiveFiles : ReceiveProcess
    {
        #region 变量
        private int BUFFER_SIZE = 65536;
        private static ILog _logger = LogManager.GetLogger(typeof(ReceiveFiles));
        #endregion

        #region 方法
        public override void SocketPorcess(Socket socket)
        {
            //获取远端发送方的IP信息
            //byte[] receiveBytes = new byte[32];
            //int byteRec = socket.Receive(receiveBytes, 0, 32, SocketFlags.None);
            //string monitorIp = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            string monitorIp = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            //获取监控别名信息
            byte[] receiveBytes = new byte[4];
            int byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
            int floderLength = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
            receiveBytes = new byte[floderLength];
            byteRec = socket.Receive(receiveBytes, 0, floderLength, SocketFlags.None);
            string monitorAlias = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            //获取文件总数
            receiveBytes = new byte[8];
            byteRec = socket.Receive(receiveBytes, 0, 8, SocketFlags.None);
            long fileNum = BitConverter.ToInt64(receiveBytes.Take(byteRec).ToArray(), 0);
            //创建接收管理对象
            WriteFileManager writeManager = new WriteFileManager(monitorIp, monitorAlias);
            long fileNumIndex = 0;
            while (fileNumIndex < fileNum)
            {
                //获取发送的文件大小
                receiveBytes = new byte[8];
                byteRec = socket.Receive(receiveBytes, 0, 8, SocketFlags.None);
                long fileSize = BitConverter.ToInt64(receiveBytes, 0);
                writeManager.Add(new WriteDataBuffer() { DataType = WriteDataType.FileSize, DataBuffer = receiveBytes });
                //获取发送的相对文件名
                receiveBytes = new byte[4];
                byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
                int fileNameLength = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
                receiveBytes = new byte[fileNameLength];
                byteRec = socket.Receive(receiveBytes, 0, fileNameLength, SocketFlags.None);
                writeManager.Add(new WriteDataBuffer() { DataType = WriteDataType.FileName, DataBuffer = receiveBytes });
                //接收数据
                long index = 0;
                if (fileSize <= 0)
                {
                    writeManager.Add(new WriteDataBuffer() { DataType = WriteDataType.FileContent });
                    fileNumIndex++;
                    Thread.Sleep(1);
                    continue;
                }
                while (index < fileSize)
                {
                    int tempSize = 0;
                    if (index + BUFFER_SIZE < fileSize)
                        tempSize = BUFFER_SIZE;
                    else
                        tempSize = (int)(fileSize - index);
                    byte[] buffer = new byte[tempSize];
                    byteRec = socket.Receive(buffer, 0, tempSize, SocketFlags.None);
                    index += byteRec;
                    writeManager.Add(new WriteDataBuffer() { DataType = WriteDataType.FileContent, DataBuffer = buffer.Take(byteRec).ToArray() });
                    Thread.Sleep(1);
                }
                //自加一
                fileNumIndex++;
                Thread.Sleep(1);
            }
            //发送断开信息
            byte[] disconnectBytes = new byte[16];
            Encoding.Unicode.GetBytes("$DSK#").CopyTo(disconnectBytes, 0);
            socket.Send(disconnectBytes, 0, 16, SocketFlags.None);
        }
        #endregion
    }
}
