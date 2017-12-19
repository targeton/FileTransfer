using FileTransfer.FileWatcher;
using FileTransfer.Utils;
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
using FileTransfer.Models;
using FileTransfer.LogToDb;
using FileTransfer.DbHelper.Entitys;

namespace FileTransfer.Sockets
{
    class SynchronousSocketManager
    {
        #region 常量
        private const int CONNECTED_MAXCOUNT = 1;
        //64k
        private const int BUFFER_SIZE = 65536;
        private const int SOCKET_SEND_TIMEOUT = 0;
        private const int SOCKET_RECEIVE_TIMEOUT = 0;
        #endregion

        #region 变量
        private SynchronousSocket _server;
        private SynchronousSocket _client;
        private IPAddress _localIP;
        private int _localListenPort = -1;
        private static ILog _logger = LogManager.GetLogger(typeof(SynchronousSocketManager));
        ////为保证每个监控文件夹内的文件发送能够顺序发送（避免并行发送）而设置的变量，以下变量只可以某个单一线程内使用
        //private Dictionary<string, Task> _monitorTaskDic = new Dictionary<string, Task>();
        ////线程安全的字典变量（监控文件夹内文件增量）
        //private ConcurrentDictionary<string, ConcurrentQueue<List<string>>> _monitorIncrementCache = new ConcurrentDictionary<string, ConcurrentQueue<List<string>>>();
        #endregion

        #region 单例
        private static SynchronousSocketManager _instance;

        public static SynchronousSocketManager Instance
        {
            get { return _instance ?? (_instance = new SynchronousSocketManager()); }
        }

        #endregion

        #region 属性
        public IPAddress LocalIP
        {
            get
            {
                return _localIP ?? (_localIP = (Dns.GetHostEntry(Dns.GetHostName())).AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork));
            }
        }

        public string LocalIPv4
        {
            get { return LocalIP.ToString(); }
        }

        public int LocalListenPort
        {
            get { return _localListenPort; }
        }

        private bool _sendingFilesFlag;

        public bool SendingFilesFlag
        {
            get { return _sendingFilesFlag; }
        }

        private bool _recevingFlag;

        public bool ReceivingFlag
        {
            get { return _recevingFlag; }
        }


        #endregion

        #region 构造函数
        public SynchronousSocketManager()
        { }
        #endregion

        #region 事件
        //public delegate void SendFileProgerssEventHandler(string monitor, string remote, string sendFile, double progerss);
        //public SendFileProgerssEventHandler SendFileProgress;

        //public delegate void AcceptFileProgressEventHandler(string monitorIp, string monitorDirectory, string sendFile, double progress);
        //public AcceptFileProgressEventHandler AcceptFileProgress;

        //public delegate void CompleteSendFileEventHandler(string monitor);
        //public CompleteSendFileEventHandler CompleteSendFile;

        //public delegate void CompleteAcceptFileEventHandler(string monitorIp, string monitorDirectory);
        //public CompleteAcceptFileEventHandler CompleteAcceptFile;
        #endregion

        #region 方法
        public void StartListening(int port)
        {
            Task.Factory.StartNew(() =>
            {
                _server = new SynchronousSocket();
                _server.SocketConnected += _server_SocketConnected;
                _localListenPort = port;
                _server.StartListening(LocalIP, port);
            });
        }

        public void StopListening()
        {
            if (_server == null) return;
            _server.StopListening();
            _server = null;
        }

        private void _server_SocketConnected(System.Net.Sockets.Socket socket)
        {
            try
            {
                //设置接收标志位
                _recevingFlag = true;
                //配置Socket的Timeout
                socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
                //获取消息头
                string headMsg = socket.GetHeadMsg(16);
                //根据消息头处理
                ReceiveContext context = new ReceiveContext(headMsg);
                context.Process(socket);
                //回复接收标志位
                _recevingFlag = false;
            }
            catch (SocketException se)
            {
                //回复接收标志位
                _recevingFlag = false;
                string msg = string.Format("本地接收远端套接字的发送数据时，发生套接字异常！SocketException ErrorCode:{0}", se.ErrorCode);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：本地接收数据时发生套接字异常！套接字错误码：{1}", DateTime.Now, se.ErrorCode));
            }
            catch (Exception e)
            {
                //回复接收标志位
                _recevingFlag = false;
                string msg = string.Format("本地接收远端套接字的发送数据时，发生异常！异常为：{0}", e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：本地接收数据时发生异常！", DateTime.Now));
            }
        }


        private Socket TryConnectRemote(IPEndPoint remote)
        {
            _client = new SynchronousSocket();
            int connectedCount = 1;
            Socket socket = _client.StartConnecting(remote);
            if (socket == null)
            {
                string msg = string.Format("第{0}次向远端{1}发起连接失败！", connectedCount, remote);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
            }
            //连接远端不成功时，再尝试
            while (socket == null && connectedCount < CONNECTED_MAXCOUNT)
            {
                Thread.Sleep(500);
                socket = _client.StartConnecting(remote);
                connectedCount++;
                if (socket == null)
                {
                    string msg = string.Format("第{0}次向远端{1}发起连接失败！", connectedCount, remote);
                    _logger.Error(msg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                }
            }
            return socket;
        }

        public bool CanConnectRemote(IPEndPoint remote)
        {
            _client = new SynchronousSocket();
            Socket socket = _client.StartConnecting(remote);
            if (socket == null)
                return false;
            else
            {
                //设置消息头(占据16位)
                byte[] sendBytes = new byte[16];
                byte[] msgBytes = Encoding.Unicode.GetBytes(@"$CCR#");
                msgBytes.CopyTo(sendBytes, 0);
                //发送消息头
                socket.Send(sendBytes, 0, 16, SocketFlags.None);
                //接收返回信息
                byte[] receiveBytes = new byte[16];
                int byteRec = socket.Receive(receiveBytes, 16, SocketFlags.None);
                string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
                DisconnectSocket(socket);
                if (msg == @"$HCR#")
                    return true;
                else
                    return false;
            }

        }

        /// <summary>
        /// 请求远端监控文件夹信息
        /// </summary>
        /// <param name="remote"></param>
        /// <returns>远端监控文件夹信息</returns>
        public List<string> RequestRemoteMoniterFloders(IPEndPoint remote)
        {
            Socket socket = null;
            try
            {
                //尝试与远端连接
                socket = TryConnectRemote(remote);
                if (socket == null)
                {
                    string msg = string.Format("请求远端监控文件夹时与远端{0}尝试{1}次（最大连接次数）连接后失败！", remote, CONNECTED_MAXCOUNT);
                    _logger.Error(msg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                    return null;
                }
                //设置Timeout
                socket.SendTimeout = SOCKET_SEND_TIMEOUT;
                socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
                //设置消息头(占据16位)
                byte[] sendBytes = new byte[16];
                byte[] msgBytes = Encoding.Unicode.GetBytes(@"$RMF#");
                msgBytes.CopyTo(sendBytes, 0);
                //发送消息
                socket.Send(sendBytes, 0, 16, SocketFlags.None);
                //接收返回信息
                var result = GetMonitorFolders(socket);
                //关闭与远端的连接
                DisconnectSocket(socket);
                return result;
            }
            catch (SocketException se)
            {
                string msg = string.Format("请求远端{0}监控文件夹信息时发生套接字异常！SocketException ErroCode:{1}", remote, se.ErrorCode);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                return null;
            }
            catch (Exception e)
            {
                string msg = string.Format("请求远端{0}监控文件夹信息时发生异常！异常信息：{1}", remote, e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                return null;
            }
        }

        private List<string> GetMonitorFolders(Socket socket)
        {
            try
            {
                //bool isEnd = false;
                List<String> moniterFloders = new List<string>();
                byte[] receiveBytes = new byte[4];
                int byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
                int flodersNum = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
                int index = 0;
                while (index < flodersNum)
                {
                    receiveBytes = new byte[4];
                    byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
                    int receiveNum = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
                    receiveBytes = new byte[receiveNum];
                    byteRec = socket.Receive(receiveBytes, 0, receiveNum, SocketFlags.None);
                    string floder = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec);
                    moniterFloders.Add(floder);
                    index++;
                    Thread.Sleep(1);
                }
                //接收结束标志
                receiveBytes = new byte[16];
                byteRec = socket.Receive(receiveBytes, 0, 16, SocketFlags.None);
                string endStr = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
                if (endStr != @"$EOF#")
                {
                    string msg = string.Format("完成监控文件夹信息的接收后接收的反馈消息头异常（$EOF#）!");
                    _logger.Warn(msg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", msg));
                }

                return moniterFloders;
            }
            catch (SocketException se)
            {
                string msg = string.Format("接收监控文件夹信息时发生套接字异常！SocketException ErroCode:{0}", se.ErrorCode);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：接收监控文件夹信息时发生套接字异常！", DateTime.Now));
                return null;
            }
            catch (Exception e)
            {
                string msg = string.Format("接收监控文件夹信息时发生异常！异常信息：{0}", e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：接收监控文件夹信息时发生异常！", DateTime.Now));
                return null;
            }
        }


        public void SendSubscribeInfo(IPEndPoint remote, string monitorDirectory)
        {
            Socket socket = null;
            try
            {
                //尝试连接
                socket = TryConnectRemote(remote);
                if (socket == null)
                {
                    string logMsg = string.Format("发送订阅消息时与远端{0}尝试{1}次（最大连接次数）连接后失败！", remote, CONNECTED_MAXCOUNT);
                    _logger.Error(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
                    RefreshUINotifyText(string.Format("{0}：发送订阅消息时无法与远端{1}连接！", DateTime.Now, remote));
                    return;
                }
                //设置Timeout
                socket.SendTimeout = SOCKET_SEND_TIMEOUT;
                socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
                //设置消息头(占据16位)
                byte[] sendBytes = new byte[16];
                byte[] msgBytes = Encoding.Unicode.GetBytes(@"$RFS#");
                msgBytes.CopyTo(sendBytes, 0);
                //发送消息
                socket.Send(sendBytes, 0, 16, SocketFlags.None);
                ////发送订阅端的IP地址和侦听端口
                //string ipAddressPort = string.Format("{0}:{1}", LocalIPv4, LocalListenPort);
                //sendBytes = new byte[64];
                //Encoding.Unicode.GetBytes(ipAddressPort).CopyTo(sendBytes, 0);
                //socket.Send(sendBytes, 0, 64, SocketFlags.None);
                //发送订阅端的侦听端口
                sendBytes = new byte[4];
                byte[] portBytes = BitConverter.GetBytes(LocalListenPort);
                portBytes.CopyTo(sendBytes, 0);
                socket.Send(sendBytes, 0, 4, SocketFlags.None);
                //发送订阅的监控文件夹
                byte[] floderBytes = Encoding.Unicode.GetBytes(monitorDirectory);
                sendBytes = new byte[4];
                BitConverter.GetBytes(floderBytes.Length).CopyTo(sendBytes, 0);
                socket.Send(sendBytes, 0, 4, SocketFlags.None);
                socket.Send(floderBytes, 0, floderBytes.Length, SocketFlags.None);
                sendBytes = new byte[16];
                Encoding.Unicode.GetBytes(@"$EOF#").CopyTo(sendBytes, 0);
                socket.Send(sendBytes, 0, 16, SocketFlags.None);
                //接收返回信息
                byte[] receiveBytes = new byte[16];
                int byteRec = socket.Receive(receiveBytes, 16, SocketFlags.None);
                string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
                if (msg != "$DSK#")
                {
                    string logMsg = string.Format("发送订阅信息后接收的反馈消息头异常（值：{0}，与$DSK#不符）！", msg);
                    _logger.Warn(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
                }

                //关闭连接
                DisconnectSocket(socket);
            }
            catch (SocketException se)
            {
                string logMsg = String.Format("向远端{0}发送订阅信息时发生套接字异常！SocketException ErrorCode:{1}", remote, se.ErrorCode);
                _logger.Error(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：向远端{1}发送订阅信息时发生套接字异常！", DateTime.Now, remote));
            }
            catch (Exception e)
            {
                string logMsg = String.Format("向远端{0}发送订阅信息时发生异常！异常：{1}", remote, e.Message);
                _logger.Error(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：向远端{1}发送订阅信息时发生异常！", DateTime.Now, remote));
            }
        }

        public Task<FilesRecord>[] SendFiles(string monitorDirectory, List<string> monitorIncrement)
        {
            //获取订阅该监控文件夹的订阅信息
            var monitorModel = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == monitorDirectory);
            if (monitorModel == null)
            {
                string msg = string.Format("{0}：准备发送{1}下的文件时无法获取订阅信息！", DateTime.Now, monitorDirectory);
                _logger.Warn(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", msg));
                RefreshUINotifyText(string.Format("{0}：准备发送{1}下的文件时无法获取订阅信息！", DateTime.Now, monitorDirectory));
                return null;
            }
            List<SubscribeInfoModel> subscribeInfos = monitorModel.SubscribeInfos.ToList();
            if (subscribeInfos == null || subscribeInfos.Count == 0)
            {
                string msg = string.Format("{0}：准备发送{1}下的文件时发现无任何订阅信息！", DateTime.Now, monitorDirectory);
                _logger.Warn(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", msg));
                RefreshUINotifyText(string.Format("{0}：准备发送{1}下的文件时发现无任何订阅信息！", DateTime.Now, monitorDirectory));
                return null;
            }
            try
            {
                //多线程发送文件
                List<Task<FilesRecord>> tasks = new List<Task<FilesRecord>>();
                foreach (var subscribeInfo in subscribeInfos)
                {
                    tasks.Add(Task<FilesRecord>.Factory.StartNew(new Func<object, FilesRecord>(SendFilesToSubscribe), new FilesRecord() { MonitorDirectory = monitorDirectory, SubscribeInfo = subscribeInfo, IncrementFiles = monitorIncrement }));
                }
                return tasks.ToArray();
            }
            catch (Exception e)
            {
                string msg = string.Format("利用任务并行库（TPL）发送文件过程中发生异常！异常：{0}", e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                return null;
            }
        }

        private FilesRecord SendFilesToSubscribe(object obj)
        {
            FilesRecord record = obj as FilesRecord;
            if (record == null || record.SubscribeInfo == null || record.IncrementFiles == null) return null;
            string monitorDirectory = record.MonitorDirectory;
            List<string> monitorIncrement = record.IncrementFiles;
            record.IncompleteSendFiles = monitorIncrement;
            SubscribeInfoModel info = record.SubscribeInfo;
            string remoteEndPoint = info.SubscribeIP;
            List<string> sendedFiles = new List<string>();
            List<string> unsendedFiles = new List<string>();
            Socket socket = null;
            //当前订阅IP不可连接则结束任务
            try
            {
                if (info.CanConnect == false) return record;
                IPEndPoint ep = UtilHelper.Instance.GetIPEndPoint(info.SubscribeIP);
                socket = TryConnectRemote(ep);
                if (socket == null)
                {
                    string logMsg = string.Format("发送{0}下文件时无法与远端{1}连接", monitorDirectory, remoteEndPoint);
                    _logger.Error(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
                    RefreshUINotifyText(string.Format("{0}：发送{1}下文件时无法与远端{2}连接", DateTime.Now, monitorDirectory, remoteEndPoint));
                    info.CanConnect = false;
                    return record;
                }
                //设置Timeout
                socket.SendTimeout = SOCKET_SEND_TIMEOUT;
                socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
                //设置消息头(占据16位)
                byte[] sendBytes = new byte[16];
                byte[] msgBytes = Encoding.Unicode.GetBytes(@"$BTF#");
                msgBytes.CopyTo(sendBytes, 0);
                //发送消息
                socket.Send(sendBytes, 0, 16, SocketFlags.None);
                ////发送本地IP
                //sendBytes = new byte[32];
                //byte[] ipBytes = Encoding.Unicode.GetBytes(LocalIPv4);
                //ipBytes.CopyTo(sendBytes, 0);
                //socket.Send(sendBytes, 0, 32, SocketFlags.None);
                //发送订阅的监控文件夹
                sendBytes = new byte[4];
                byte[] monitorBytes = Encoding.Unicode.GetBytes(monitorDirectory);
                sendBytes = BitConverter.GetBytes(monitorBytes.Length);
                socket.Send(sendBytes, 0, 4, SocketFlags.None);
                socket.Send(monitorBytes, 0, monitorBytes.Length, SocketFlags.None);
                //发送文件总数
                sendBytes = new byte[8];
                long fileNum = monitorIncrement.Count;
                byte[] fileNumBytes = BitConverter.GetBytes(fileNum);
                fileNumBytes.CopyTo(sendBytes, 0);
                socket.Send(sendBytes, 0, 8, SocketFlags.None);
                //发送增量文件信息
                foreach (var file in monitorIncrement)
                {
                    //发送初始进度
                    SimpleIoc.Default.GetInstance<MainViewModel>().ShowSendProgress(monitorDirectory, remoteEndPoint, file, 0.0);
                    //发送文件大小
                    sendBytes = new byte[8];
                    long fileSize = UtilHelper.Instance.GetFileSize(file);
                    BitConverter.GetBytes(fileSize).CopyTo(sendBytes, 0);
                    socket.Send(sendBytes, 0, 8, SocketFlags.None);
                    //发送文件名（先发送文件名长度，再发送文件名）
                    sendBytes = new byte[4];
                    byte[] fileNameBytes = Encoding.Unicode.GetBytes(file);
                    BitConverter.GetBytes(fileNameBytes.Length).CopyTo(sendBytes, 0);
                    socket.Send(sendBytes, 0, 4, SocketFlags.None);
                    socket.Send(fileNameBytes, 0, fileNameBytes.Length, SocketFlags.None);
                    //日志记录
                    LogHelper.Instance.SendLogger.Add(new SendLogEntity(DateTime.Now, file, remoteEndPoint, @"开始发送"));
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        long index = 0;
                        while (index < fileSize)
                        {
                            //发送进度事件
                            double progress = index * 1.0 / fileSize;
                            SimpleIoc.Default.GetInstance<MainViewModel>().ShowSendProgress(monitorDirectory, remoteEndPoint, file, progress);
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
                            socket.Send(buffer, 0, tempSize, SocketFlags.None);
                            index += tempSize;
                            Thread.Sleep(1);
                        }
                    }
                    //发送进度事件
                    SimpleIoc.Default.GetInstance<MainViewModel>().ShowSendProgress(monitorDirectory, remoteEndPoint, file, 1.0);
                    //日志记录
                    LogHelper.Instance.SendLogger.Add(new SendLogEntity(DateTime.Now, file, remoteEndPoint, @"完成发送"));
                    //记录发送文件
                    sendedFiles.Add(file);
                }
                ////发出所有文件发送完毕的事件
                //if (CompleteSendFile != null)
                //    CompleteSendFile(monitorDirectory);
                //接收返回信息
                byte[] receiveBytes = new byte[16];
                int byteRec = socket.Receive(receiveBytes, 0, 16, SocketFlags.None);
                string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
                if (msg != "$DSK#")
                {
                    string logMsg = string.Format("向{0}发送所有文件后接收的反馈消息头异常（值：{1}，与$DSK#不符）！", ep, msg);
                    _logger.Warn(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
                }
                DisconnectSocket(socket);
            }
            catch (SocketException se)
            {
                CloseSocket(socket);
                string msg = string.Format("发送{0}下文件至远端{1}过程中发生SocketException！ErroCode：{2}", monitorDirectory, remoteEndPoint, se.ErrorCode);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                RefreshUINotifyText(string.Format("{0}：发送{1}下文件至远端{2}过程中发生SocketException！", DateTime.Now, monitorDirectory, remoteEndPoint));
            }
            catch (Exception e)
            {
                CloseSocket(socket);
                string msg = string.Format("发送{0}下文件至远端{1}过程中发生异常！异常：{2}", monitorDirectory, remoteEndPoint, e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                RefreshUINotifyText(string.Format("{0}：发送{1}下文件至远端{2}过程中发生异常！", DateTime.Now, monitorDirectory, remoteEndPoint));
            }
            finally
            {
                unsendedFiles = monitorIncrement.Except(sendedFiles).ToList();
                record.IncompleteSendFiles = unsendedFiles;
                if (unsendedFiles.Count > 0)
                    foreach (var file in unsendedFiles)
                    {
                        string msg = string.Format("未能向{0}发送文件{1}", remoteEndPoint, file);
                        _logger.Warn(msg);
                        LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                    }
            }
            return record;
        }

        public void SendDeleteMonitorInfo(IPEndPoint remote, string monitorDirectory)
        {
            Socket socket = null;
            try
            {
                //尝试与远端连接
                socket = TryConnectRemote(remote);
                if (socket == null)
                {
                    string logMsg = string.Format("与远端{0}尝试{1}次（最大连接次数）连接后失败！", remote, CONNECTED_MAXCOUNT);
                    _logger.Error(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
                    RefreshUINotifyText(string.Format("{0}：向远端{1}通知删除监控文件夹信息时连接异常！", DateTime.Now, remote));
                    return;
                }
                //设置Timeout
                socket.SendTimeout = SOCKET_SEND_TIMEOUT;
                socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
                //设置消息头(占据16位)
                byte[] sendBytes = new byte[16];
                byte[] msgBytes = Encoding.Unicode.GetBytes(@"$DMF#");
                msgBytes.CopyTo(sendBytes, 0);
                //发送消息头
                socket.Send(sendBytes, 0, 16, SocketFlags.None);
                //发送本地IP和监控文件夹
                byte[] directoryBytes = Encoding.Unicode.GetBytes(monitorDirectory);
                //前4位为文件夹byte数组的长度，后面为文件夹byte数组
                sendBytes = new byte[4 + directoryBytes.Length];
                //Encoding.Unicode.GetBytes(LocalIPv4).CopyTo(sendBytes, 0);
                BitConverter.GetBytes(directoryBytes.Length).CopyTo(sendBytes, 0);
                directoryBytes.CopyTo(sendBytes, 4);
                socket.Send(sendBytes, 0);
                //接收返回信息
                byte[] receiveBytes = new byte[16];
                int byteRec = socket.Receive(receiveBytes, 16, SocketFlags.None);
                string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
                if (msg != "$DSK#")
                {
                    string logMsg = string.Format("向{0}发送删除监控文件夹信息后接收的反馈消息头异常（值：{1}，与$DSK#不符）！", remote, msg);
                    _logger.Warn(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
                }
                //关闭连接
                DisconnectSocket(socket);
            }
            catch (SocketException se)
            {
                string msg = String.Format("向远端发送删除监控文件夹信息时发生套接字异常！SocketException ErrorCode:{0}", se.ErrorCode);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：向远端{1}通知删除监控文件夹信息时套接字异常！ErrorCode：{2}", DateTime.Now, remote, se.ErrorCode));
            }
            catch (Exception e)
            {
                string msg = String.Format("向远端发送删除监控文件夹信息时发生异常！SocketException ErrorCode:{0}", e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：向远端{1}通知删除监控文件夹信息时异常！", DateTime.Now, remote));
            }
        }

        public void SendUnregisterSubscribeInfo(IPEndPoint remote, string monitorDirectory)
        {
            Socket socket = null;
            try
            {
                //尝试与远端连接
                socket = TryConnectRemote(remote);
                if (socket == null)
                {
                    string logMsg = string.Format("与远端{0}尝试{1}次（最大连接次数）连接后失败！", remote, CONNECTED_MAXCOUNT);
                    _logger.Error(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
                    RefreshUINotifyText(string.Format("{0}：向远端{1}通知取消订阅监控时连接异常！", DateTime.Now, remote));
                    return;
                }
                //设置Timeout
                socket.SendTimeout = SOCKET_SEND_TIMEOUT;
                socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
                //设置消息头(占据16位)
                byte[] sendBytes = new byte[16];
                byte[] msgBytes = Encoding.Unicode.GetBytes(@"$DSF#");
                msgBytes.CopyTo(sendBytes, 0);
                //发送消息头
                socket.Send(sendBytes, 0, 16, SocketFlags.None);
                //发送本地接收端口和监控文件夹
                byte[] directoryBytes = Encoding.Unicode.GetBytes(monitorDirectory);
                //string ipStr = string.Format("{0}:{1}", LocalIPv4, LocalListenPort);
                int byteLength = 8 + directoryBytes.Length;
                sendBytes = new byte[byteLength];
                byte[] portBytes = BitConverter.GetBytes(LocalListenPort);
                portBytes.CopyTo(sendBytes, 0);
                BitConverter.GetBytes(directoryBytes.Length).CopyTo(sendBytes, 4);
                directoryBytes.CopyTo(sendBytes, 8);
                socket.Send(sendBytes, 0, byteLength, SocketFlags.None);
                //接收返回信息
                byte[] receiveBytes = new byte[16];
                int byteRec = socket.Receive(receiveBytes, 16, SocketFlags.None);
                string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
                if (msg != "$DSK#")
                {
                    string logMsg = string.Format("向{0}发送注销订阅信息后接收的反馈消息头异常（值：{1}，与$DSK#不符）！", remote, msg);
                    _logger.Warn(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
                }
                //关闭连接
                DisconnectSocket(socket);
            }
            catch (SocketException se)
            {
                string msg = String.Format("向远端发送注销订阅信息时发生套接字异常！SocketException ErrorCode:{0}", se.ErrorCode);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：向远端{1}通知取消订阅监控时套接字异常！异常：{2}", DateTime.Now, remote, se.ErrorCode));
            }
            catch (Exception e)
            {
                string msg = String.Format("向远端发送注销订阅信息时发生异常！SocketException ErrorCode:{0}", e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：向远端{1}通知取消订阅监控时异常！", DateTime.Now, remote));
            }
        }

        public void SendOnlineOfflineInfo(IPEndPoint remote, string monitorDirectory, bool online = true)
        {
            Socket socket = null;
            try
            {
                //尝试与远端连接
                socket = TryConnectRemote(remote);
                if (socket == null)
                {
                    string logMsg = string.Format("与远端{0}尝试{1}次（最大连接次数）连接后失败！", remote, CONNECTED_MAXCOUNT);
                    _logger.Error(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
                    RefreshUINotifyText(string.Format("{0}：向远端{1}发送上线下线信息时连接异常！", DateTime.Now, remote));
                    return;
                }
                //设置Timeout
                socket.SendTimeout = SOCKET_SEND_TIMEOUT;
                socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
                //设置消息头(占据16位)
                byte[] sendBytes = new byte[16];
                //on/offline
                byte[] msgBytes = Encoding.Unicode.GetBytes(@"$OFL#");
                msgBytes.CopyTo(sendBytes, 0);
                //发送消息头
                socket.Send(sendBytes, 0, 16, SocketFlags.None);
                //发送本地端口和监控文件夹
                byte[] directoryBytes = Encoding.Unicode.GetBytes(monitorDirectory);
                //string ipStr = string.Format("{0}:{1}", LocalIPv4, LocalListenPort);
                int byteLength = 8 + directoryBytes.Length;
                sendBytes = new byte[byteLength];
                //Encoding.Unicode.GetBytes(ipStr).CopyTo(sendBytes, 0);
                BitConverter.GetBytes(LocalListenPort).CopyTo(sendBytes, 0);
                BitConverter.GetBytes(directoryBytes.Length).CopyTo(sendBytes, 4);
                directoryBytes.CopyTo(sendBytes, 8);
                socket.Send(sendBytes, 0, byteLength, SocketFlags.None);
                //发送上线的消息头"$ON#"或者下线的消息头"$OFF#"
                if (online)
                {
                    sendBytes = new byte[16];
                    //on/offline
                    byte[] tempBytes = Encoding.Unicode.GetBytes(@"$ON#");
                    tempBytes.CopyTo(sendBytes, 0);
                    //发送消息头
                    socket.Send(sendBytes, 0, 16, SocketFlags.None);
                }
                else
                {
                    sendBytes = new byte[16];
                    //on/offline
                    byte[] tempBytes = Encoding.Unicode.GetBytes(@"$OFF#");
                    tempBytes.CopyTo(sendBytes, 0);
                    //发送消息头
                    socket.Send(sendBytes, 0, 16, SocketFlags.None);
                }
                //接收返回信息
                byte[] receiveBytes = new byte[16];
                int byteRec = socket.Receive(receiveBytes, 16, SocketFlags.None);
                string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
                if (msg != "$DSK#")
                {
                    string logMsg = string.Format("向{0}发送上线下线信息后接收的反馈消息头异常（值：{1}，与$DSK#不符）！", remote, msg);
                    _logger.Warn(logMsg);
                    LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
                }
                //关闭连接
                DisconnectSocket(socket);
            }
            catch (SocketException se)
            {
                string msg = String.Format("向远端发送上线下线信息时发生套接字异常！SocketException ErrorCode:{0}", se.ErrorCode);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：向远端{1}发送上线下线信息时套接字异常！异常：{2}", DateTime.Now, remote, se.ErrorCode));
            }
            catch (Exception e)
            {
                string msg = String.Format("向远端发送上线下线信息时发生异常！SocketException ErrorCode:{0}", e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                CloseSocket(socket);
                RefreshUINotifyText(string.Format("{0}：向远端{1}发送上线下线信息时异常！", DateTime.Now, remote));
            }
        }

        private static void RefreshUINotifyText(string notify)
        {
            if (SimpleIoc.Default.IsRegistered<MainViewModel>())
                SimpleIoc.Default.GetInstance<MainViewModel>().NotifyText = notify;
        }

        private void DisconnectSocket(Socket socket)
        {
            if (socket == null) return;
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Disconnect(false);
                socket = null;
            }
            catch (SocketException se)
            {
                string msg = string.Format("Socket进行DisConnect操作时，发生套接字异常！ErrorCode：{0}", se.ErrorCode);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
            }
            catch (Exception e)
            {
                string msg = string.Format("Socket进行DisConnect操作时，发生异常！异常：{0}", e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
            }
            finally
            {
                socket = null;
            }
        }

        private void CloseSocket(Socket socket)
        {
            if (socket == null) return;
            socket.Close();
            //_logger.Info(string.Format("关闭Socket连接并释放所有资源！"));
            socket = null;
        }

        #endregion

    }
}
