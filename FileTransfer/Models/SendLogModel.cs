using FileTransfer.DbHelper.Entitys;
using GalaSoft.MvvmLight;
using System;

namespace FileTransfer.Models
{
    public class SendLogModel : ObservableObject
    {
        #region 属性
        private string _sendFileName;

        public string SendFileName
        {
            get { return _sendFileName; }
            set
            {
                _sendFileName = value;
                RaisePropertyChanged("SendFileName");
            }
        }

        private DateTime _sendFileTime;

        public DateTime SendFileTime
        {
            get { return _sendFileTime; }
            set
            {
                _sendFileTime = value;
                RaisePropertyChanged("SendFileTime");
            }
        }

        private string _subscribeIP;

        public string SubscribeIP
        {
            get { return _subscribeIP; }
            set
            {
                _subscribeIP = value;
                RaisePropertyChanged("SubscribeIP");
            }
        }

        private string _sendFileState;

        public string SendFileState
        {
            get { return _sendFileState; }
            set
            {
                _sendFileState = value;
                RaisePropertyChanged("SendFileState");
            }
        }

        #endregion

        #region 构造函数
        public SendLogModel()
        { }

        public SendLogModel(string sendFile, string subscribeIP, string sendState)
        {
            _sendFileName = sendFile;
            _subscribeIP = subscribeIP;
            _sendFileState = sendState;
        }

        public SendLogModel(DateTime sendTime, string sendFile, string subscribeIP, string sendState)
        {
            _sendFileTime = sendTime;
            _sendFileName = sendFile;
            _subscribeIP = subscribeIP;
            _sendFileState = sendState;
        }

        public SendLogModel(SendLogEntity entity)
        {
            _sendFileTime = entity.SendDate;
            _sendFileName = entity.SendFile;
            _subscribeIP = entity.SubscribeIP;
            _sendFileState = entity.SendState;
        }
        #endregion
    }
}
