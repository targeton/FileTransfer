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
using FileTransfer.IO;

namespace FileTransfer.Sockets
{
    class SynchronousSocketManager
    {
        #region 常量

        #endregion

        #region 变量
        private SynchronousSocket _server;
        //private SynchronousSocket _client;
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
                ////配置Socket的Timeout
                //socket.ReceiveTimeout = SOCKET_RECEIVE_TIMEOUT;
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
                socket.CloseSocket();
                string.Format("{0}：本地接收数据时发生套接字异常！套接字错误码：{1}", DateTime.Now, se.ErrorCode).RefreshUINotifyText();
            }
            catch (Exception e)
            {
                //回复接收标志位
                _recevingFlag = false;
                string msg = string.Format("本地接收远端套接字的发送数据时，发生异常！异常为：{0}", e.Message);
                _logger.Error(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "ERROR", msg));
                socket.CloseSocket();
                string.Format("{0}：本地接收数据时发生异常！", DateTime.Now).RefreshUINotifyText();
            }
        }

        public bool CanConnectRemote(IPEndPoint remote)
        {
            SendContext context = new SendContext(@"$CCR#");
            object[] param = null;
            object feedback = context.ConnectToRemote(remote, param);
            if (feedback == null || feedback.GetType() != typeof(bool))
                return false;
            else
                return (bool)feedback;
        }

        /// <summary>
        /// 请求远端监控文件夹信息
        /// </summary>
        /// <param name="remote"></param>
        /// <returns>远端监控文件夹信息</returns>
        public List<string> RequestRemoteMoniterFloders(IPEndPoint remote)
        {
            SendContext context = new SendContext(@"$RMF#");
            object[] param = null;
            object feedback = context.ConnectToRemote(remote, param);
            List<string> result = feedback as List<string>;
            return result;
        }

        public void SendSubscribeInfo(IPEndPoint remote, string monitorAlias)
        {
            SendContext context = new SendContext(@"$RFS#");
            object[] param = new object[2];
            param[0] = LocalListenPort;
            param[1] = monitorAlias;
            context.ConnectToRemote(remote, param);
        }

        public Task<FilesRecord>[] SendFiles(string monitorAlias, string monitorDirectory, List<string> monitorIncrement)
        {
            //获取订阅该监控文件夹的订阅信息
            var monitorModel = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.FirstOrDefault(m => m.MonitorAlias == monitorAlias);
            if (monitorModel == null)
            {
                string msg = string.Format("{0}：准备发送{1}下的文件时无法获取订阅信息！", DateTime.Now, monitorAlias);
                _logger.Warn(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", msg));
                string.Format("{0}：准备发送{1}下的文件时无法获取订阅信息！", DateTime.Now, monitorAlias).RefreshUINotifyText();
                return null;
            }
            List<SubscribeInfoModel> subscribeInfos = monitorModel.SubscribeInfos.ToList();
            if (subscribeInfos == null || subscribeInfos.Count == 0)
            {
                string msg = string.Format("{0}：准备发送{1}下的文件时发现无任何订阅信息！", DateTime.Now, monitorAlias);
                _logger.Warn(msg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", msg));
                string.Format("{0}：准备发送{1}下的文件时发现无任何订阅信息！", DateTime.Now, monitorAlias).RefreshUINotifyText();
                return null;
            }
            try
            {
                //多线程发送文件
                List<Task<FilesRecord>> tasks = new List<Task<FilesRecord>>();
                foreach (var subscribeInfo in subscribeInfos)
                {
                    tasks.Add(Task<FilesRecord>.Factory.StartNew(
                        new Func<object, FilesRecord>(SendFilesToSubscribe), new FilesRecord()
                        {
                            MonitorAlias = monitorAlias,
                            MonitorDirectory = monitorDirectory,
                            SubscribeInfo = subscribeInfo,
                            IncrementFiles = monitorIncrement
                        }));
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
            string monitorAlias = record.MonitorAlias;
            string monitorDirectory = record.MonitorDirectory;
            List<string> monitorIncrement = record.IncrementFiles;
            record.IncompleteSendFiles = monitorIncrement;
            SubscribeInfoModel info = record.SubscribeInfo;
            string remoteEndPoint = info.SubscribeIP;

            SendContext context = new SendContext(@"$BTF#");
            object[] param = new object[3];
            param[0] = monitorAlias;
            param[1] = monitorDirectory;
            param[2] = monitorIncrement;
            IPEndPoint remote = UtilHelper.Instance.GetIPEndPoint(remoteEndPoint);
            object feedback = context.ConnectToRemote(remote, param);

            List<string> sendedFiles = feedback as List<string>;
            if (sendedFiles != null)
            {
                List<string> unsendedFiles = monitorIncrement.Except(sendedFiles).ToList();
                record.IncompleteSendFiles = unsendedFiles;
            }
            return record;
        }

        public void SendDeleteMonitorInfo(IPEndPoint remote, string monitorAlias)
        {
            SendContext context = new SendContext(@"$DMF#");
            object[] param = new object[1];
            param[0] = monitorAlias;
            context.ConnectToRemote(remote, param);
        }

        public void SendUnregisterSubscribeInfo(IPEndPoint remote, string monitorAlias)
        {
            SendContext context = new SendContext(@"$DSF#");
            object[] param = new object[2];
            param[0] = monitorAlias;
            param[1] = LocalListenPort;
            context.ConnectToRemote(remote, param);
        }

        public void SendOnlineOfflineInfo(IPEndPoint remote, string monitorAlias, bool online = true)
        {
            SendContext context = new SendContext(@"$OFL#");
            object[] param = new object[3];
            param[0] = monitorAlias;
            param[1] = LocalListenPort;
            param[2] = online;
            context.ConnectToRemote(remote, param);
        }
        #endregion

    }
}
