using FileTransfer.DbHelper.Entitys;
using FileTransfer.IO;
using FileTransfer.LogToDb;
using FileTransfer.Utils;
using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FileTransfer.Sockets
{
    public class SendFiles : SendProcess
    {
        #region 变量
        private const int BUFFER_SIZE = 65536;
        #endregion
        #region 构造函数
        public SendFiles(string headMsg)
            : base(headMsg)
        { }
        #endregion

        #region 方法
        protected override object SendParams(IPEndPoint remote, object[] param)
        {
            List<string> sendedFiles = new List<string>();
            if (param == null || param.Length != 3) return sendedFiles;
            var monitorAlias = (string)param[0];
            var monitorDirectory = (string)param[1];
            var monitorIncrement = param[2] as List<string>;
            if (monitorIncrement == null) return sendedFiles;
            try
            {
                //发送被订阅的监控别名
                byte[] sendBytes = new byte[4];
                byte[] monitorBytes = Encoding.Unicode.GetBytes(monitorAlias);
                sendBytes = BitConverter.GetBytes(monitorBytes.Length);
                _client.Send(sendBytes, 0, 4, SocketFlags.None);
                _client.Send(monitorBytes, 0, monitorBytes.Length, SocketFlags.None);
                //发送文件总数
                sendBytes = new byte[8];
                long fileNum = monitorIncrement.Count;
                byte[] fileNumBytes = BitConverter.GetBytes(fileNum);
                fileNumBytes.CopyTo(sendBytes, 0);
                _client.Send(sendBytes, 0, 8, SocketFlags.None);
                //发送增量文件信息
                foreach (var file in monitorIncrement)
                {
                    //发送初始进度
                    SimpleIoc.Default.GetInstance<MainViewModel>().ShowSendProgress(monitorAlias, remote.ToString(), file, 0.0);
                    //发送文件大小
                    sendBytes = new byte[8];
                    long fileSize = UtilHelper.Instance.GetFileSize(file);
                    BitConverter.GetBytes(fileSize).CopyTo(sendBytes, 0);
                    _client.Send(sendBytes, 0, 8, SocketFlags.None);
                    //发送相对文件名（先发送文件名长度，再发送文件名）
                    string relativeFile = IOHelper.Instance.GetRelativePath(monitorDirectory, file);
                    sendBytes = new byte[4];
                    byte[] fileNameBytes = Encoding.Unicode.GetBytes(relativeFile);
                    BitConverter.GetBytes(fileNameBytes.Length).CopyTo(sendBytes, 0);
                    _client.Send(sendBytes, 0, 4, SocketFlags.None);
                    _client.Send(fileNameBytes, 0, fileNameBytes.Length, SocketFlags.None);
                    //日志记录
                    LogHelper.Instance.SendLogger.Add(new SendLogEntity(DateTime.Now, file, remote.ToString(), @"开始发送"));
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        long index = 0;
                        while (index < fileSize)
                        {
                            //发送进度事件
                            double progress = index * 1.0 / fileSize;
                            SimpleIoc.Default.GetInstance<MainViewModel>().ShowSendProgress(monitorAlias, remote.ToString(), file, progress);
                            //设置文件流的当前位置
                            fs.Seek(index, SeekOrigin.Begin);
                            //计算发送长度
                            int tempSize = 0;
                            if (index + BUFFER_SIZE < fileSize)
                            {
                                tempSize = BUFFER_SIZE;
                            }
                            else
                            {
                                tempSize = (int)(fileSize - index);
                            }
                            byte[] buffer = new byte[tempSize];
                            fs.Read(buffer, 0, tempSize);
                            _client.Send(buffer, 0, tempSize, SocketFlags.None);
                            index += tempSize;
                            Thread.Sleep(1);
                        }
                    }
                    //记录发送文件
                    sendedFiles.Add(file);
                    //发送进度事件
                    SimpleIoc.Default.GetInstance<MainViewModel>().ShowSendProgress(monitorAlias, remote.ToString(), file, 1.0);
                    //日志记录
                    LogHelper.Instance.SendLogger.Add(new SendLogEntity(DateTime.Now, file, remote.ToString(), @"完成发送"));
                }
            }
            catch (Exception e)
            {
                string logMsg = string.Format("发送{0}下的文件至远端{1}的过程中发生异常！异常：{2}", monitorAlias, remote, e.Message);
                _logger.Error(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
            }
            return sendedFiles;
        }
        #endregion
    }
}
