using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FileTransfer.Models
{
    public class SubscribeModel : ObservableObject
    {
        #region 属性
        private string _monitorIP;
        [XmlAttribute("MonitorIP")]
        public string MonitorIP
        {
            get { return _monitorIP; }
            set
            {
                _monitorIP = value;
                RaisePropertyChanged("MonitorIP");
            }
        }

        private int _monitorListenPort;
        [XmlAttribute("MonitorListenPort")]
        public int MonitorListenPort
        {
            get { return _monitorListenPort; }
            set
            {
                _monitorListenPort = value;
                RaisePropertyChanged("MonitorListenPort");
            }
        }

        private string _monitorDirectory;
        [XmlAttribute("MonitorDirectory")]
        public string MonitorDirectory
        {
            get { return _monitorDirectory; }
            set
            {
                _monitorDirectory = value;
                RaisePropertyChanged("MonitorDirectory");
            }
        }

        private string _acceptDirectory;
        [XmlAttribute("AcceptDirectory")]
        public string AcceptDirectory
        {
            get { return _acceptDirectory; }
            set
            {
                _acceptDirectory = value;
                RaisePropertyChanged("AcceptDirectory");
            }
        }
        private string _acceptFileName;
        [XmlIgnore]
        public string AcceptFileName
        {
            get { return _acceptFileName; }
            set
            {
                _acceptFileName = value;
                RaisePropertyChanged("AcceptFileName");
            }
        }
        private double _acceptFilePercent;
        [XmlIgnore]
        public double AcceptFilePercent
        {
            get { return _acceptFilePercent; }
            set
            {
                _acceptFilePercent = value;
                RaisePropertyChanged("AcceptFilePercent");
            }
        }

        #endregion
    }
}
