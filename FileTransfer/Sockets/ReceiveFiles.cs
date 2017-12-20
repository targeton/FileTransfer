using FileTransfer.DbHelper.Entitys;
using FileTransfer.FileWatcher;
using FileTransfer.IO;
using FileTransfer.LogToDb;
using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            //获取监控文件夹信息
            byte[] receiveBytes = new byte[4];
            int byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
            int floderLength = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
            receiveBytes = new byte[floderLength];
            byteRec = socket.Receive(receiveBytes, 0, floderLength, SocketFlags.None);
            string monitorDirectory = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            //获取文件总数
            receiveBytes = new byte[8];
            byteRec = socket.Receive(receiveBytes, 0, 8, SocketFlags.None);
            long fileNum = BitConverter.ToInt64(receiveBytes.Take(byteRec).ToArray(), 0);
            ////获取接收文件夹集合
            //List<string> acceptDirectories = SimpleIoc.Default.GetInstance<MainViewModel>().SubscribeCollection.Where(s => s.MonitorIP == monitorIp && s.MonitorDirectory == monitorDirectory).Select(s => s.AcceptDirectory).ToList();
            //if (acceptDirectories.Count == 0)
            //{
            //    string savePath = SimpleIoc.Default.GetInstance<MainViewModel>().SendExceptionSavePath;
            //    string logMsg = string.Format("{0}发送来的文件无接收设置，转存至{1}！", monitorIp, savePath);
            //    _logger.Warn(logMsg);
            //    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
            //    string.Format("{0}:{1}发送来的文件无接收设置，转存至{2}！", DateTime.Now, monitorIp, savePath).RefreshUINotifyText();
            //    acceptDirectories.Add(savePath);
            //}
            WriteFileManager writeManager = new WriteFileManager(monitorIp, monitorDirectory);
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
                //string relativeFileName = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
                writeManager.Add(new WriteDataBuffer() { DataType = WriteDataType.FileName, DataBuffer = receiveBytes });
                ////设置接收文件的文件名
                //List<string> acceptFiles = acceptDirectories.Select(d => System.IO.Path.Combine(d, relativeFileName)).ToList();
                //检查文件夹是否存在
                //acceptFiles.ForEach(f => { IOHelper.Instance.CheckAndCreateDirectory(f); });
                //日志记录
                //acceptFiles.ForEach(file => { LogHelper.Instance.ReceiveLogger.Add(new ReceiveLogEntity(DateTime.Now, file, monitorIp, monitorDirectory, @"开始接收")); });
                //设置文件流
                //List<FileStream> fileStreams = acceptFiles.Select(f => new FileStream(f, FileMode.Create, FileAccess.Write)).ToList();
                //接收文件
                long index = 0;
                while (index < fileSize)
                {
                    ////接收进度事件
                    //double progress = index * 1.0 / fileSize;
                    //SimpleIoc.Default.GetInstance<MainViewModel>().ShowAcceptProgress(monitorIp, monitorDirectory, fileName, progress);
                    ////文件流操作
                    //fileStreams.ForEach(fs => fs.Seek(index, SeekOrigin.Begin));
                    int tempSize = 0;
                    if (index + BUFFER_SIZE < fileSize)
                        tempSize = BUFFER_SIZE;
                    else
                        tempSize = (int)(fileSize - index);
                    byte[] buffer = new byte[tempSize];
                    byteRec = socket.Receive(buffer, 0, tempSize, SocketFlags.None);
                    index += byteRec;
                    //fileStreams.ForEach(fs => { fs.Write(buffer.Take(byteRec).ToArray(), 0, byteRec); });
                    writeManager.Add(new WriteDataBuffer() { DataType = WriteDataType.FileContent, DataBuffer = buffer.Take(byteRec).ToArray() });
                    Thread.Sleep(1);
                }
                //关闭文件流
                //fileStreams.ForEach(fs => { fs.Close(); });
                //接收进度事件
                //SimpleIoc.Default.GetInstance<MainViewModel>().ShowAcceptProgress(monitorIp, monitorDirectory, fileName, 1.0);
                //日志记录
                //acceptFiles.ForEach(file => { LogHelper.Instance.ReceiveLogger.Add(new ReceiveLogEntity(DateTime.Now, file, monitorIp, monitorDirectory, @"完成接收")); });
                //自加一
                fileNumIndex++;
                Thread.Sleep(10);
            }
            //发出所有文件接收完毕事件
            //if (SynchronousSocketManager.Instance.CompleteAcceptFile != null)
            //    SynchronousSocketManager.Instance.CompleteAcceptFile(monitorIp, monitorDirectory);
            //发送断开信息
            byte[] disconnectBytes = new byte[16];
            Encoding.Unicode.GetBytes("$DSK#").CopyTo(disconnectBytes, 0);
            socket.Send(disconnectBytes, 0, 16, SocketFlags.None);
        }
        #endregion
    }
}
