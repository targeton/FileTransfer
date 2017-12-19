using FileTransfer.DbHelper.Entitys;
using GalaSoft.MvvmLight;
using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Models
{
    public class ErrorLogModel : ObservableObject
    {
        #region 属性
        private DateTime _errorOccurTime;
        public DateTime ErrorOccurTime
        {
            get { return _errorOccurTime; }
            set
            {
                _errorOccurTime = value;
                RaisePropertyChanged("ErrorOccurTime");
            }
        }

        private string _errorFlag;
        public string ErrorFlag
        {
            get { return _errorFlag; }
            set
            {
                _errorFlag = value;
                RaisePropertyChanged("ErrorFlag");
            }
        }

        private string _errorContent;
        public string ErrorContent
        {
            get { return _errorContent; }
            set
            {
                _errorContent = value;
                RaisePropertyChanged("ErrorContent");
            }
        }

        #endregion

        #region 构造函数
        public ErrorLogModel()
        { }

        public ErrorLogModel(ErrorLogEntity entity)
        {
            _errorOccurTime = entity.LogDate;
            _errorFlag = entity.LogLevel;
            _errorContent = entity.LogMessage;
        }
        #endregion
    }
}
