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
        private ILog _logger = LogManager.GetLogger(typeof(LogsQueryViewModel));
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
        public RelayCommand RefreshSendLogsCommand { get; set; }
        public RelayCommand RefreshReceiveLogsCommand { get; set; }
        public RelayCommand RefreshMonitorLogsCommand { get; set; }
        public RelayCommand RefreshErrorLogsCommand { get; set; }
        #endregion

        #region 构造函数
        public LogsQueryViewModel()
        {
            InitialParams();
            InitialCommands();
        }
        #endregion

        #region 方法
        private void InitialParams()
        { }

        private void InitialCommands()
        {
            LoadCommand = new RelayCommand(ExecuteLoadCommand);
            RefreshSendLogsCommand = new RelayCommand(ExecuteRefreshSendLogsCommand);
            RefreshReceiveLogsCommand = new RelayCommand(ExecuteRefreshReceiveLogsCommand);
            RefreshMonitorLogsCommand = new RelayCommand(ExecuteRefreshMonitorLogsCommand);
            RefreshErrorLogsCommand = new RelayCommand(ExecuteRefreshErrorLogsCommand);
        }

        private void ExecuteLoadCommand()
        {
            Task.Factory.StartNew(() => { LoadAllLogs(); });
        }

        private void LoadAllLogs()
        {
            try
            {
                IList<SendLogEntity> sendLogResult = null;
                IList<ReceiveLogEntity> receiveLogResult = null;
                IList<MonitorLogEntity> monitorLogResult = null;
                IList<ErrorLogEntity> logResult = null;
                using (ISession session = DbAccessHelper.SessionFactory.OpenSession())
                {
                    sendLogResult = session.QueryOver<SendLogEntity>().OrderBy(log => log.SendDate).Desc.List();
                    receiveLogResult = session.QueryOver<ReceiveLogEntity>().OrderBy(log => log.ReceiveDate).Desc.List();
                    monitorLogResult = session.QueryOver<MonitorLogEntity>().OrderBy(log => log.MonitorDate).Desc.List();
                    logResult = session.QueryOver<ErrorLogEntity>().OrderBy(log => log.LogDate).Desc.List();
                }
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (sendLogResult != null)
                    {
                        foreach (var log in sendLogResult)
                        {
                            SendLogs.Add(new SendLogModel(log));
                        }
                    }
                    if (receiveLogResult != null)
                    {
                        foreach (var log in receiveLogResult)
                        {
                            ReceiveLogs.Add(new ReceiveLogModel(log));
                        }
                    }
                    if (monitorLogResult != null)
                    {
                        foreach (var log in monitorLogResult)
                        {
                            MonitorLogs.Add(new MonitorLogModel(log));
                        }
                    }
                    if (logResult != null)
                    {
                        foreach (var log in logResult)
                        {
                            ErrorLogs.Add(new ErrorLogModel(log));
                        }
                    }
                }), null);
            }
            catch (Exception e)
            {
                _logger.Warn(string.Format("从数据库查询日志时，发生异常{0}", e.Message));
            }
        }

        private void ExecuteRefreshSendLogsCommand()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    IList<SendLogEntity> sendLogResult = null;
                    using (ISession session = DbAccessHelper.SessionFactory.OpenSession())
                    {
                        sendLogResult = session.QueryOver<SendLogEntity>().OrderBy(log => log.SendDate).Desc.List();
                    }
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        if (sendLogResult == null) return;
                        SendLogs = new ObservableCollection<SendLogModel>();
                        foreach (var log in sendLogResult)
                        {
                            SendLogs.Add(new SendLogModel(log));
                        }
                    }), null);
                }
                catch (Exception e)
                {
                    _logger.Warn(string.Format("从数据库查询发送日志时，发生异常{0}", e.Message));
                }
            });
        }

        private void ExecuteRefreshReceiveLogsCommand()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    IList<ReceiveLogEntity> receiveLogResult = null;
                    using (ISession session = DbAccessHelper.SessionFactory.OpenSession())
                    {
                        receiveLogResult = session.QueryOver<ReceiveLogEntity>().OrderBy(log => log.ReceiveDate).Desc.List();
                    }
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        if (receiveLogResult == null) return;
                        ReceiveLogs = new ObservableCollection<ReceiveLogModel>();
                        foreach (var log in receiveLogResult)
                        {
                            ReceiveLogs.Add(new ReceiveLogModel(log));
                        }
                    }), null);
                }
                catch (Exception e)
                {
                    _logger.Warn(string.Format("从数据库查询接收日志时，发生异常{0}", e.Message));
                }
            });
        }

        private void ExecuteRefreshMonitorLogsCommand()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    IList<MonitorLogEntity> monitorLogResult = null;
                    using (ISession session = DbAccessHelper.SessionFactory.OpenSession())
                    {
                        monitorLogResult = session.QueryOver<MonitorLogEntity>().OrderBy(log => log.MonitorDate).Desc.List();
                    }
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        if (monitorLogResult == null) return;
                        MonitorLogs = new ObservableCollection<MonitorLogModel>();
                        foreach (var log in monitorLogResult)
                        {
                            MonitorLogs.Add(new MonitorLogModel(log));
                        }
                    }), null);
                }
                catch (Exception e)
                {
                    _logger.Warn(string.Format("从数据库查询监控日志时，发生异常{0}", e.Message));
                }
            });
        }

        private void ExecuteRefreshErrorLogsCommand()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    IList<ErrorLogEntity> logResult = null;
                    using (ISession session = DbAccessHelper.SessionFactory.OpenSession())
                    {
                        logResult = session.QueryOver<ErrorLogEntity>().OrderBy(log => log.LogDate).Desc.List();
                    }
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        if (logResult == null) return;
                        ErrorLogs = new ObservableCollection<ErrorLogModel>();
                        foreach (var log in logResult)
                        {
                            ErrorLogs.Add(new ErrorLogModel(log));
                        }
                    }), null);
                }
                catch (Exception e)
                {
                    _logger.Warn(string.Format("从数据库查询其他日志时，发生异常{0}", e.Message));
                }
            });
        }
        #endregion
    }
}
