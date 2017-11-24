using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FileTransfer.Models
{
    public class MonitorModel : ObservableObject
    {
        #region 属性
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

        //private string _subscribeIP;
        //[XmlAttribute("SubscribeIP")]
        //public string SubscribeIP
        //{
        //    get { return _subscribeIP; }
        //    set
        //    {
        //        _subscribeIP = value;
        //        RaisePropertyChanged("SubscribeIP");
        //    }
        //}

        private bool _deleteFiles;
        [XmlAttribute("DeleteFiles")]
        public bool DeleteFiles
        {
            get { return _deleteFiles; }
            set
            {
                _deleteFiles = value;
                RaisePropertyChanged("DeleteFiles");
            }
        }

        private bool _deleteSubdirecory;
        [XmlAttribute("DeleteSubdirectory")]
        public bool DeleteSubdirectory
        {
            get { return _deleteSubdirecory; }
            set
            {
                _deleteSubdirecory = value;
                RaisePropertyChanged("DeleteSubdirectory");
            }
        }

        private ObservableCollection<SubscribeInfoModel> _subscribeInfos;
        [XmlArray("SubscribeInfos"), XmlArrayItem("SubscribeInfo")]
        public ObservableCollection<SubscribeInfoModel> SubscribeInfos
        {
            get { return _subscribeInfos ; }
            set
            {
                _subscribeInfos = value;
                RaisePropertyChanged("SubscribeInfos");
            }
        }

        private string _transferFileName;
        [XmlIgnore]
        public string TransferFileName
        {
            get { return _transferFileName; }
            set
            {
                _transferFileName = value;
                RaisePropertyChanged("TransferFileName");
            }
        }

        private double _transferPercent;
        [XmlIgnore]
        public double TransferPercent
        {
            get { return _transferPercent; }
            set
            {
                _transferPercent = value;
                RaisePropertyChanged("TransferPercent");
            }
        }

        #endregion
    }
}
