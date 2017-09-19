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
using System.Windows.Forms;

namespace FileTransfer.Sockets
{
    class SynchronousSocketManager
    {
        #region 常量
        private const int CONNECTED_MAXCOUNT = 5;
        //64k
        private const int BUFFER_SIZE = 65536;
        private const int SOCKET_SEND_TIMEOUT = 5000;
        private const int SOCKET_RECEIVE_TIMEOUT = 5000;
        #endregion

        #region 变量
        private SynchronousSocket _server;
        private SynchronousSocket _client;
        private IPAddress _localIP;
        private int _localListenPort = -1;
        private static ILog _logger = LogManager.GetLogger(typeof(SynchronousSocketManager));
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
        #endregion

        #region 构造函数
        public SynchronousSocketManager()
        { }
        #endregion

        #region 事件
        public delegate void SendFileProgerssEventHandler(string monitor, string sendFile, double progerss);
        public SendFileProgerssEventHandler SendFileProgress;

        public delegate void AcceptFileProgressEventHandler(string monitorIp, string monitorDirectory, string sendFile, double progress);
        public AcceptFileProgressEventHandler AcceptFileProgress;

        public delegate void CompleteSendFileEventHandler(string monitor);
        public CompleteSendFileEventHandler CompleteSendFile;

        public delegate void CompleteAcceptFileEventHandler(string monitorIp, string monitorDirectory);
        public CompleteAcceptFileEventHandler CompleteAcceptFile;
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
                //配置Socket的Timeout
                socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
                byte[] headerBytes = new byte[16];
                int byteRec = socket.Receive(headerBytes, 0, 16, SocketFlags.None);
                string headerMsg = Encoding.Unicode.GetString(headerBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
                switch (headerMsg)
                {
                    case "$RMF#":
                        //获取本地监控文件夹信息
                        List<string> monitorFloders = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.Select(m => m.MonitorDirectory).ToList();
                        //通过连接Socket返回（发送）监控文件夹信息
                        SendMoniterFolders(socket, monitorFloders);
                        break;
                    case "$RFS#":
                        ReceiveSubscribInfo(socket);
                        break;
                    case "$BTF#":
                        ReceiveFiles(socket);
                        break;
                    default:
                        _logger.Error(string.Format("套接字所接受字节数据无法转换为有效数据！转换结果为：{0}", headerMsg));
                        break;
                }
            }
            catch (SocketException se)
            {
                _logger.Error(string.Format("本地接收远端套接字的发送数据时，发生套接字异常！SocketException ErrorCode:{0}", se.ErrorCode));
                CloseSocket(socket);
                MessageBox.Show("本地接收远端套接字的发送数据时，发生套接字异常！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("本地接收远端套接字的发送数据时，发生异常！异常为：{0}", e.Message));
                CloseSocket(socket);
                MessageBox.Show("本地接收远端套接字的发送数据时，发生异常！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReceiveFiles(System.Net.Sockets.Socket socket)
        {
            //获取远端发送方的IP信息
            byte[] receiveBytes = new byte[32];
            int byteRec = socket.Receive(receiveBytes, 0, 32, SocketFlags.None);
            string monitorIp = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            //获取监控文件夹信息
            receiveBytes = new byte[4];
            byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
            int floderLength = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
            receiveBytes = new byte[floderLength];
            byteRec = socket.Receive(receiveBytes, 0, floderLength, SocketFlags.None);
            string monitorDirectory = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            //获取文件总数
            receiveBytes = new byte[8];
            byteRec = socket.Receive(receiveBytes, 0, 8, SocketFlags.None);
            long fileNum = BitConverter.ToInt64(receiveBytes.Take(byteRec).ToArray(), 0);
            //获取接收文件夹集合
            List<string> acceptDirectories = SimpleIoc.Default.GetInstance<MainViewModel>().SubscribeCollection.Where(s => s.MonitorIP == monitorIp && s.MonitorDirectory == monitorDirectory).Select(s => s.AcceptDirectory).ToList();
            long fileNumIndex = 0;
            while (fileNumIndex < fileNum)
            {
                //获取发送的文件大小
                receiveBytes = new byte[8];
                byteRec = socket.Receive(receiveBytes, 0, 8, SocketFlags.None);
                long fileSize = BitConverter.ToInt64(receiveBytes, 0);
                //获取发送的文件名
                receiveBytes = new byte[4];
                byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
                int fileNameLength = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
                receiveBytes = new byte[fileNameLength];
                byteRec = socket.Receive(receiveBytes, 0, fileNameLength, SocketFlags.None);
                string fileName = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
                //设置接受文件的文件名
                List<string> acceptFiles = acceptDirectories.Select(d => fileName.Replace(monitorDirectory, d)).ToList();
                //检查文件夹是否存在
                acceptFiles.ForEach(f => { IOHelper.Instance.CheckAndCreateDirectory(f); });
                //日志记录
                acceptFiles.ForEach(file => { _logger.Info(string.Format("[File]{0}[FileReceiveState]{1}", file, @"开始接收")); });
                //设置文件流
                List<FileStream> fileStreams = acceptFiles.Select(f => new FileStream(f, FileMode.Create, FileAccess.Write)).ToList();
                //接收文件
                long index = 0;
                while (index < fileSize)
                {
                    //接收进度事件
                    if (AcceptFileProgress != null)
                    {
                        double progress = index * 1.0 / fileSize;
                        AcceptFileProgress(monitorIp, monitorDirectory, fileName, progress);
                    }
                    fileStreams.ForEach(fs => fs.Seek(index, SeekOrigin.Begin));
                    int tempSize = 0;
                    if (index + BUFFER_SIZE < fileSize)
                        tempSize = BUFFER_SIZE;
                    else
                        tempSize = (int)(fileSize - index);
                    byte[] buffer = new byte[tempSize];
                    byteRec = socket.Receive(buffer, 0, tempSize, SocketFlags.None);
                    fileStreams.ForEach(fs => { fs.Write(buffer.Take(byteRec).ToArray(), 0, byteRec); });
                    index += byteRec;
                    Thread.Sleep(5);
                }
                //关闭文件流
                fileStreams.ForEach(fs => { fs.Close(); });
                //接收进度事件
                if (AcceptFileProgress != null)
                {
                    AcceptFileProgress(monitorIp, monitorDirectory, fileName, 1.0);
                }
                //日志记录
                acceptFiles.ForEach(file => { _logger.Info(string.Format("[File]{0}[FileReceiveState]{1}", file, @"完成接收")); });
                //自加一
                fileNumIndex++;
                Thread.Sleep(50);
            }
            //发出所有文件接收完毕事件
            if (CompleteAcceptFile != null)
                CompleteAcceptFile(monitorIp, monitorDirectory);
            //发送断开信息
            byte[] disconnectBytes = new byte[16];
            Encoding.Unicode.GetBytes("$DSK#").CopyTo(disconnectBytes, 0);
            socket.Send(disconnectBytes, 0, 16, SocketFlags.None);
        }

        private void ReceiveSubscribInfo(System.Net.Sockets.Socket socket)
        {
            //获取订阅者IP
            byte[] ipBytes = new byte[64];
            int byteRec = socket.Receive(ipBytes, 64, SocketFlags.None);
            string ipAddressPort = Encoding.Unicode.GetString(ipBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            //获取订阅的监控文件夹
            byte[] receiveBytes = new byte[4];
            byteRec = socket.Receive(receiveBytes, 0, 4, SocketFlags.None);
            int receiveNum = BitConverter.ToInt32(receiveBytes.Take(byteRec).ToArray(), 0);
            receiveBytes = new byte[receiveNum];
            byteRec = socket.Receive(receiveBytes, 0, receiveNum, SocketFlags.None);
            string monitorDirectory = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec);
            SimpleIoc.Default.GetInstance<MainViewModel>().CompleteMonitorSetting(ipAddressPort, monitorDirectory);
            receiveBytes = new byte[16];
            byteRec = socket.Receive(receiveBytes, 0, 16, SocketFlags.None);
            string endStr = Encoding.Unicode.GetString(receiveBytes.Take(byteRec).ToArray(), 0, byteRec).TrimEnd('\0');
            _logger.Info(string.Format("接收订阅信息{0}！", endStr == @"$EOF#" ? "成功" : "失败"));
            //发送断开信息
            byte[] disconnectBytes = new byte[16];
            Encoding.Unicode.GetBytes("$DSK#").CopyTo(disconnectBytes, 0);
            socket.Send(disconnectBytes, 0, 16, SocketFlags.None);
        }

        private int GetRemoteListenPort(System.Net.Sockets.Socket socket)
        {
            try
            {
                byte[] portBytes = new byte[16];
                int byteRec = socket.Receive(portBytes, 16, SocketFlags.None);
                string portStr = Encoding.Unicode.GetString(portBytes, 0, 16).TrimEnd('\0');
                int port = int.Parse(portStr);
                return port;
            }
            catch (SocketException se)
            {
                _logger.Error(string.Format("获取远端监听端口信息时发生套接字异常！SocketException ErrorCode：{0}", se.ErrorCode));
                return -1;
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("获取远端监听端口信息时发生异常！异常信息：{0}", e.Message));
                return -1;
            }
        }

        private Socket TryConnectRemote(IPEndPoint remote)
        {
            _client = new SynchronousSocket();
            int connectedCount = 1;
            Socket socket = _client.StartConnecting(remote);
            _logger.Info(string.Format("第{0}次向远端{1}:{2}发起连接{3}！", connectedCount, remote.Address, remote.Port, socket == null ? "失败" : "成功"));
            //连接远端不成功时，再尝试
            while (socket == null && connectedCount < CONNECTED_MAXCOUNT)
            {
                Thread.Sleep(500);
                socket = _client.StartConnecting(remote);
                connectedCount++;
                _logger.Info(string.Format("第{0}次向远端{1}:{2}发起连接{3}！", connectedCount, remote.Address, remote.Port, socket == null ? "失败" : "成功"));
            }
            return socket;
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
                    _logger.Error(string.Format("与远端{0}:{1}尝试{2}次（最大连接次数）连接后失败！", remote.Address, remote.Port, CONNECTED_MAXCOUNT));
                    MessageBox.Show("无法与远端进行连接！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _logger.Error(string.Format("请求远端{0}:{1}监控文件夹信息时发生套接字异常！SocketException ErroCode:{2}", remote.Address, remote.Port, se.ErrorCode));
                CloseSocket(socket);
                return null;
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("请求远端{0}:{1}监控文件夹信息时发生异常！异常信息：{2}", remote.Address, remote.Port, e.Message));
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
                _logger.Info(string.Format("完成监控文件夹信息的接收{0}!", endStr == @"$EOF#" ? "成功" : "失败"));
                return moniterFloders;
            }
            catch (SocketException se)
            {
                _logger.Error(string.Format("接收监控文件夹信息时发生套接字异常！SocketException ErroCode:{0}", se.ErrorCode));
                CloseSocket(socket);
                return null;
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("接收监控文件夹信息时发生异常！异常信息：{0}", e.Message));
                CloseSocket(socket);
                return null;
            }
        }

        /// <summary>
        /// 通过连接的客户端Socket将监控文件夹信息发回请求端
        /// </summary>
        /// <param name="socket">连接的客户端Socket</param>
        /// <param name="floders">监控文件夹集合</param>
        private void SendMoniterFolders(Socket socket, List<string> floders)
        {
            try
            {
                //配置Socket的发送Timeout
                socket.SendTimeout = SOCKET_SEND_TIMEOUT;
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
                _logger.Error(string.Format("发送监控文件夹信息时发生套接字异常！SocketException ErroCode:{0}", se.ErrorCode));
                CloseSocket(socket);
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("发送监控文件夹信息时发生异常！异常信息：{0}", e.Message));
                CloseSocket(socket);
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
                    _logger.Error(string.Format("与远端{0}:{1}尝试{2}次（最大连接次数）连接后失败！", remote.Address, remote.Port, CONNECTED_MAXCOUNT));
                    MessageBox.Show("无法与远端进行连接！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                //发送订阅端的IP地址和侦听端口
                string ipAddressPort = string.Format("{0}:{1}", LocalIPv4, LocalListenPort);
                sendBytes = new byte[64];
                Encoding.Unicode.GetBytes(ipAddressPort).CopyTo(sendBytes, 0);
                socket.Send(sendBytes, 0, 64, SocketFlags.None);
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
                    _logger.Info(string.Format("发送订阅信息后返回的结果解析异常（值：{0}，与$DSK#不符）！", msg));
                //关闭连接
                DisconnectSocket(socket);
            }
            catch (SocketException se)
            {
                _logger.Error(String.Format("向远端{0}:{1}发送订阅信息时发生套接字异常！SocketException ErrorCode:{2}", remote.Address, remote.Port, se.ErrorCode));
                CloseSocket(socket);
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("向远端{0}:{1}发送订阅信息时发生异常！异常：{2}", remote.Address, remote.Port, e.Message));
                CloseSocket(socket);
            }
        }

        public void SendMonitorChanges(string monitorDirectory, List<string> monitorIncrement)
        {
            SendFiles(monitorDirectory, monitorIncrement);
            //Task.Factory.StartNew(() => { SendFiles(monitorDirectory, monitorIncrement); });
        }

        private void SendFiles(string monitorDirectory, List<string> monitorIncrement)
        {
            List<Socket> sockets = new List<Socket>();
            try
            {
                List<string> subscribeIPs = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.Where(m => m.MonitorDirectory == monitorDirectory).Select(m => m.SubscribeIP).ToList();
                if (subscribeIPs == null || subscribeIPs.Count <= 0 || subscribeIPs.Any(s => string.IsNullOrEmpty(s)))
                {
                    _logger.Info(string.Format("监控设置中可能没有{0}的监控文件夹或者没有远端机器订阅该监控文件夹！", monitorDirectory));
                    return;
                }
                List<IPEndPoint> endPoints = subscribeIPs.Select(s => UtilHelper.Instance.GetIPEndPoint(s)).ToList();
                //先建立连接
                endPoints.ForEach(ep =>
                {
                    //尝试连接
                    Socket socket = TryConnectRemote(ep);
                    if (socket == null)
                    {
                        _logger.Error(string.Format("发送监控文件夹{0}内文件时与远端{1}:{2}尝试{3}次（最大连接次数）连接后失败！", monitorDirectory, ep.Address, ep.Port, CONNECTED_MAXCOUNT));
                        MessageBox.Show("传送文件时无法与远端进行连接！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    //设置Timeout
                    socket.SendTimeout = SOCKET_SEND_TIMEOUT;
                    socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
                    sockets.Add(socket);
                });
                //socket集合一起发送头消息
                //设置消息头(占据16位)
                byte[] sendBytes = new byte[16];
                byte[] msgBytes = Encoding.Unicode.GetBytes(@"$BTF#");
                msgBytes.CopyTo(sendBytes, 0);
                //发送消息
                sockets.ForEach(socket => socket.Send(sendBytes, 0, 16, SocketFlags.None));
                //发送本地IP
                sendBytes = new byte[32];
                byte[] ipBytes = Encoding.Unicode.GetBytes(LocalIPv4);
                ipBytes.CopyTo(sendBytes, 0);
                sockets.ForEach(socket => socket.Send(sendBytes, 0, 32, SocketFlags.None));
                //发送订阅的监控文件夹
                sendBytes = new byte[4];
                byte[] monitorBytes = Encoding.Unicode.GetBytes(monitorDirectory);
                sendBytes = BitConverter.GetBytes(monitorBytes.Length);
                sockets.ForEach(socket => socket.Send(sendBytes, 0, 4, SocketFlags.None));
                sockets.ForEach(socket => socket.Send(monitorBytes, 0, monitorBytes.Length, SocketFlags.None));
                //发送文件总数
                sendBytes = new byte[8];
                long fileNum = monitorIncrement.Count;
                byte[] fileNumBytes = BitConverter.GetBytes(fileNum);
                fileNumBytes.CopyTo(sendBytes, 0);
                sockets.ForEach(socket => socket.Send(sendBytes, 0, 8, SocketFlags.None));
                //发送增量文件信息
                monitorIncrement.ForEach(file =>
                {
                    //发送文件大小
                    sendBytes = new byte[8];
                    long fileSize = UtilHelper.Instance.GetFileSize(file);
                    BitConverter.GetBytes(fileSize).CopyTo(sendBytes, 0);
                    sockets.ForEach(socket => socket.Send(sendBytes, 0, 8, SocketFlags.None));
                    //发送文件名（先发送文件名长度，再发送文件名）
                    sendBytes = new byte[4];
                    byte[] fileNameBytes = Encoding.Unicode.GetBytes(file);
                    BitConverter.GetBytes(fileNameBytes.Length).CopyTo(sendBytes, 0);
                    sockets.ForEach(socket => socket.Send(sendBytes, 0, 4, SocketFlags.None));
                    sockets.ForEach(socket => socket.Send(fileNameBytes, 0, fileNameBytes.Length, SocketFlags.None));
                    //日志记录
                    _logger.Info(string.Format("[File]{0}[FileSendState]{1}", file, @"开始发送"));
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        long index = 0;
                        while (index < fileSize)
                        {
                            //发送进度事件
                            if (SendFileProgress != null)
                            {
                                double progress = index * 1.0 / fileSize;
                                SendFileProgress(monitorDirectory, file, progress);
                            }
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
                            sockets.ForEach(socket => socket.Send(buffer, 0, tempSize, SocketFlags.None));
                            index += tempSize;
                            Thread.Sleep(5);
                        }
                        //发送进度事件
                        if (SendFileProgress != null)
                        {
                            SendFileProgress(monitorDirectory, file, 1.0);
                        }
                    }
                    //日志记录
                    _logger.Info(string.Format("[File]{0}[FileSendState]{1}", file, @"发送完成"));
                    //发送完单个文件后，根据情况选择是否删除
                    Task.Factory.StartNew(() =>
                    {
                        if (SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.Where(m => m.MonitorDirectory == monitorDirectory).Any(m => m.DeleteFiles == true))
                            IOHelper.Instance.DeleteFile(file);
                    });
                    Thread.Sleep(50);
                });
                //所有文件发送完成后，根据情况选择是否删除子目录
                Task.Factory.StartNew(() =>
                {
                    if (SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.Where(m => m.MonitorDirectory == monitorDirectory).Any(m => m.DeleteSubdirectory == true))
                        IOHelper.Instance.DeleteDirectories(IOHelper.Instance.GetAllSubDirectories(monitorDirectory));
                });
                //发出所有文件发送完毕的事件
                if (CompleteSendFile != null)
                    CompleteSendFile(monitorDirectory);
                //接收回馈消息并关闭socket
                sockets.ForEach(socket =>
                {
                    //接收返回信息
                    byte[] receiveBytes = new byte[16];
                    int byteRec = socket.Receive(receiveBytes, 0, 16, SocketFlags.None);
                    string msg = Encoding.Unicode.GetString(receiveBytes, 0, 16).TrimEnd('\0');
                    if (msg != "$DSK#")
                        _logger.Info(string.Format("发送文件后返回的结果解析异常（值：{0}，与$DSK#不符）！", msg));
                    DisconnectSocket(socket);
                });
            }
            catch (SocketException se)
            {
                _logger.Error(String.Format("向远端发送监控文件夹内的文件信息时发生套接字异常！SocketException ErrorCode:{0}", se.ErrorCode));
                sockets.ForEach(socket => CloseSocket(socket));
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("向远端发送监控文件夹内的文件时发生异常！异常：{0}", e.Message));
                sockets.ForEach(socket => CloseSocket(socket));
            }
        }

        private void ReceiveAllBytes(Socket socket, byte[] buffer, int offset, int size, SocketFlags flag)
        {

        }

        private void DisconnectSocket(Socket socket)
        {
            if (socket == null) return;
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(false);
            _logger.Info(string.Format("Socket{0}断开与远端的连接！", socket.Connected ? "未能成功" : "成功"));
            socket = null;
        }

        private void CloseSocket(Socket socket)
        {
            if (socket == null) return;
            socket.Close();
            _logger.Info(string.Format("关闭Socket连接并释放所有资源！"));
            socket = null;
        }

        #endregion

    }
}
