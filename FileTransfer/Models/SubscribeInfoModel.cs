using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FileTransfer.Models
{
    public class SubscribeInfoModel : ObservableObject
    {
        #region 属性
        private string _subscribeIP;
        [XmlAttribute("SubscribeIP")]
        public string SubscribeIP
        {
            get { return _subscribeIP; }
            set
            {
                _subscribeIP = value;
                RaisePropertyChanged("SubscribeIP");
            }
        }

        private bool _canConnect;
        [XmlIgnore]
        public bool CanConnect
        {
            get { return _canConnect; }
            set
            {
                _canConnect = value;
                RaisePropertyChanged("CanConnect");
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
