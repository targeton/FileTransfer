using FileTransfer.Models;
using FileTransfer.Utils;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using log4net;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileTransfer.ViewModels
{
    public class LogsQueryViewModel : ViewModelBase
    {
        #region 变量
        private string _logsFilePath;
        //private static ILog _logger = LogManager.GetLogger(typeof(LogsQueryViewModel));
        private readonly string _fileLogPattern;
        private readonly string _sendLogPattern;
        private readonly string _receiveLogPattern;
        private readonly string _errorLogPattern;
        private readonly string _monitorLogPattern;

        #endregion

        #region 属性
        private ObservableCollection<SendLogModel> _sendLogs;
        public ObservableCollection<SendLogModel> SendLogs
        {
            get { return _sendLogs ?? (_sendLogs = new ObservableCollection<SendLogModel>()); }
            set
            {
                _sendLogs = value;
                RaisePropertyChanged("SendLogs");
            }
        }

        private ObservableCollection<ReceiveLogModel> _receiveLogs;
        public ObservableCollection<ReceiveLogModel> ReceiveLogs
        {
            get { return _receiveLogs ?? (_receiveLogs = new ObservableCollection<ReceiveLogModel>()); }
            set
            {
                _receiveLogs = value;
                RaisePropertyChanged("ReceiveLogs");
            }
        }

        private ObservableCollection<ErrorLogModel> _errorLogs;
        public ObservableCollection<ErrorLogModel> ErrorLogs
        {
            get { return _errorLogs ?? (_errorLogs = new ObservableCollection<ErrorLogModel>()); }
            set
            {
                _errorLogs = value;
                RaisePropertyChanged("ErrorLogs");
            }
        }

        private ObservableCollection<MonitorLogModel> _monitorLogs;
        public ObservableCollection<MonitorLogModel> MonitorLogs
        {
            get { return _monitorLogs ?? (_monitorLogs = new ObservableCollection<MonitorLogModel>()); }
            set
            {
                _monitorLogs = value;
                RaisePropertyChanged("MonitorLogs");
            }
        }

        #endregion

        #region 命令
        public RelayCommand LoadCommand { get; set; }
        #endregion

        #region 构造函数
        public LogsQueryViewModel()
        {
            _fileLogPattern = @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}),\d{3} \[\d+] [A-Z,a-z,0-9, ]+.*?- \[File]([\s\S]*?)";
            _sendLogPattern = @"\[File]([\s\S]*?)\[SubscribeIP]([\s\S]*?)\[FileSendState]([\s\S]*?)";
            _receiveLogPattern = @"\[File]([\s\S]*?)\[MonitorIP]([\s\S]*?)\[MonitorDirectory]([\s\S]*?)\[FileReceiveState]([\s\S]*?)";
            _errorLogPattern = @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}),\d{3} \[\d+] (WARN|ERROR|FATAL){1} [A-Z,a-z,0-9, ]+.*?- ([\s\S]*?)";
            _monitorLogPattern = @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}),\d{3} \[\d+] [A-Z,a-z,0-9, ]+.*?- \[MonitorFile]([\s\S]*?)";
            InitialParams();
            InitialCommands();
        }
        #endregion

        #region 方法
        private void InitialParams()
        {
            _logsFilePath = Path.Combine(Environment.CurrentDirectory, "logs");
        }

        private void InitialCommands()
        {
            LoadCommand = new RelayCommand(ExecuteLoadCommand);
        }

        private void ExecuteLoadCommand()
        {
            ////判断文件夹是否存在
            //if (!Directory.Exists(_logsFilePath)) return;
            ////获取所有日志
            //List<string> logFiles = Directory.GetFiles(_logsFilePath, @"File*.log*").ToList();
            //foreach (var logFile in logFiles)
            //{
            //    GetLogs(logFile);
            //}
            try
            {
                using (ISession session = DbAccessHelper.SessionFactory.OpenSession())
                {
                    var sendLogResult = session.CreateCriteria(typeof(SendLogEntity)).List<SendLogEntity>();
                    var receiveLogResult = session.CreateCriteria(typeof(ReceiveLogEntity)).List<ReceiveLogEntity>();
                    var monitorLogResult = session.CreateCriteria(typeof(MonitorLogEntity)).List<MonitorLogEntity>();
                    var logResult = session.CreateCriteria(typeof(ErrorLogEntity)).List<ErrorLogEntity>();
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        foreach (var log in sendLogResult)
                        {
                            SendLogs.Add(new SendLogModel(log));
                        }
                        foreach (var log in receiveLogResult)
                        {
                            ReceiveLogs.Add(new ReceiveLogModel(log));
                        }
                        foreach (var log in monitorLogResult)
                        {
                            MonitorLogs.Add(new MonitorLogModel(log));
                        }
                        foreach (var log in logResult)
                        {
                            ErrorLogs.Add(new ErrorLogModel(log));
                        }
                    }), null);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async void GetLogs(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("utf-8")))
                    {
                        while (sr.Peek() >= 0)
                        {
                            string logLine = await sr.ReadLineAsync();
                            Match match = Regex.Match(logLine, _fileLogPattern, RegexOptions.RightToLeft);
                            if (!match.Success)
                            {
                                Match errorMatch = Regex.Match(logLine, _errorLogPattern, RegexOptions.RightToLeft);
                                if (errorMatch.Success)
                                {
                                    string dateTime = errorMatch.Groups[1].Value;
                                    string logType = errorMatch.Groups[2].Value;
                                    string content = errorMatch.Groups[3].Value;
                                    ErrorLogs.Add(new ErrorLogModel() { ErrorOccurTime = DateTime.ParseExact(dateTime, "yyyy-MM-dd HH:mm:ss", null), ErrorFlag = logType, ErrorContent = content });
                                }
                                else
                                {
                                    Match monitorMatch = Regex.Match(logLine, _monitorLogPattern, RegexOptions.RightToLeft);
                                    if (!monitorMatch.Success) continue;
                                    string dateTime = monitorMatch.Groups[1].Value;
                                    string content = monitorMatch.Groups[2].Value;
                                    MonitorLogs.Add(new MonitorLogModel(dateTime, content));
                                }
                            }
                            else
                            {
                                string dateTime = match.Groups[1].Value;
                                string logMsg = string.Format("[File]{0}", match.Groups[2].Value);
                                Match sendMatch = Regex.Match(logMsg, _sendLogPattern, RegexOptions.RightToLeft);
                                if (sendMatch.Success)
                                {
                                    string sendFile = sendMatch.Groups[1].Value;
                                    string subscribeIP = sendMatch.Groups[2].Value;
                                    string sendState = sendMatch.Groups[3].Value;
                                    SendLogs.Add(new SendLogModel() { SendFileTime = DateTime.ParseExact(dateTime, "yyyy-MM-dd HH:mm:ss", null), SendFileName = sendFile, SubscribeIP = subscribeIP, SendFileState = sendState });
                                }
                                else
                                {
                                    Match receiveMatch = Regex.Match(logMsg, _receiveLogPattern, RegexOptions.RightToLeft);
                                    if (!receiveMatch.Success) continue;
                                    string receiveFile = receiveMatch.Groups[1].Value;
                                    string monitorIP = receiveMatch.Groups[2].Value;
                                    string monitorDirectory = receiveMatch.Groups[3].Value;
                                    string receiveState = receiveMatch.Groups[4].Value;
                                    ReceiveLogs.Add(new ReceiveLogModel() { ReceiveFileTime = DateTime.ParseExact(dateTime, "yyyy-MM-dd HH:mm:ss", null), ReceiveFileName = receiveFile, MonitorIP = monitorIP, MonitorDirectory = monitorDirectory, ReceiveFileState = receiveState });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Instance.Logger.Error(string.Format("加载日志异常！异常信息为：{0}", e.Message));
                MessageBox.Show(string.Format("加载日志异常！异常信息为：{0}", e.Message), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
    }
}
