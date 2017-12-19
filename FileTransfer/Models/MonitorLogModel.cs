using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using log4net;
using FileTransfer.DbHelper.Entitys;

namespace FileTransfer.Models
{
    public class MonitorLogModel : ObservableObject
    {
        #region 变量
        //private static ILog _logger = LogManager.GetLogger(typeof(MonitorLogModel));
        #endregion

        #region 属性
        private DateTime _monitorChangedTime;
        public DateTime MonitorChangedTime
        {
            get { return _monitorChangedTime; }
            set
            {
                _monitorChangedTime = value;
                RaisePropertyChanged("MonitorChangedTime");
            }
        }

        private string _monitorChangedFile;
        public string MonitorChangedFile
        {
            get { return _monitorChangedFile; }
            set
            {
                _monitorChangedFile = value;
                RaisePropertyChanged("MonitorChangedFile");
            }
        }

        #endregion

        #region 构造函数
        public MonitorLogModel()
        { }

        public MonitorLogModel(string file)
        {
            _monitorChangedFile = file;
        }

        public MonitorLogModel(DateTime changeTime, string file)
        {
            _monitorChangedTime = changeTime;
            _monitorChangedFile = file;
        }

        public MonitorLogModel(MonitorLogEntity entity)
        {
            _monitorChangedTime = entity.MonitorDate;
            _monitorChangedFile = entity.ChangedFile;
        }
        #endregion
    }
}
