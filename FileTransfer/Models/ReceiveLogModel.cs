using FileTransfer.DbHelper.Entitys;
using GalaSoft.MvvmLight;
using System;

namespace FileTransfer.Models
{
    public class ReceiveLogModel : ObservableObject
    {
        #region 属性
        private string _receiveFileName;

        public string ReceiveFileName
        {
            get { return _receiveFileName; }
            set
            {
                _receiveFileName = value;
                RaisePropertyChanged("ReceiveFileName");
            }
        }

        private DateTime _receiveFileTime;

        public DateTime ReceiveFileTime
        {
            get { return _receiveFileTime; }
            set
            {
                _receiveFileTime = value;
                RaisePropertyChanged("ReceiveFileTime");
            }
        }

        private string _monitorIP;

        public string MonitorIP
        {
            get { return _monitorIP; }
            set
            {
                _monitorIP = value;
                RaisePropertyChanged("MonitorIP");
            }
        }

        private string _monitorAlias;

        public string MonitorAlias
        {
            get { return _monitorAlias; }
            set
            {
                _monitorAlias = value;
                RaisePropertyChanged("MonitorAlias");
            }
        }

        private string _receiveFileState;

        public string ReceiveFileState
        {
            get { return _receiveFileState; }
            set
            {
                _receiveFileState = value;
                RaisePropertyChanged("ReceiveFileState");
            }
        }

        #endregion

        #region 构造函数
        public ReceiveLogModel()
        { }

        public ReceiveLogModel(string receiveFile, string monitorIP, string monitorAlias, string receiveState)
        {
            _receiveFileName = receiveFile;
            _monitorIP = monitorIP;
            _monitorAlias = monitorAlias;
            _receiveFileState = receiveState;
        }

        public ReceiveLogModel(DateTime receiveTime, string receiveFile, string monitorIP, string monitorAlias, string receiveState)
        {
            _receiveFileTime = receiveTime;
            _receiveFileName = receiveFile;
            _monitorIP = monitorIP;
            _monitorAlias = monitorAlias;
            _receiveFileState = receiveState;
        }

        public ReceiveLogModel(ReceiveLogEntity entity)
        {
            _receiveFileTime = entity.ReceiveDate;
            _receiveFileName = entity.ReceiveFile;
            _monitorIP = entity.MonitorIP;
            _monitorAlias = entity.MonitorAlias;
            _receiveFileState = entity.ReceiveState;
        }

        #endregion
    }
}
