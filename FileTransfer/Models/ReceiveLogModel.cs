using FileTransfer.DbHelper.Entitys;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private string _monitorDirectory;

        public string MonitorDirectory
        {
            get { return _monitorDirectory; }
            set
            {
                _monitorDirectory = value;
                RaisePropertyChanged("MonitorDirectory");
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

        public ReceiveLogModel(string receiveFile, string monitorIP, string monitorDirectory, string receiveState)
        {
            _receiveFileName = receiveFile;
            _monitorIP = monitorIP;
            _monitorDirectory = monitorDirectory;
            _receiveFileState = receiveState;
        }

        public ReceiveLogModel(DateTime receiveTime, string receiveFile, string monitorIP, string monitorDirectory, string receiveState)
        {
            _receiveFileTime = receiveTime;
            _receiveFileName = receiveFile;
            _monitorIP = monitorIP;
            _monitorDirectory = monitorDirectory;
            _receiveFileState = receiveState;
        }

        public ReceiveLogModel(ReceiveLogEntity entity)
        {
            _receiveFileTime = entity.ReceiveDate;
            _receiveFileName = entity.ReceiveFile;
            _monitorIP = entity.MonitorIP;
            _monitorDirectory = entity.MonitorDirectory;
            _receiveFileState = entity.ReceiveState;
        }

        #endregion
    }
}
