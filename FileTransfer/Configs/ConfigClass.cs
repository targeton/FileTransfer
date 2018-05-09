using FileTransfer.Models;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace FileTransfer.Configs
{
    [XmlRoot("Settings")]
    public class ConfigClass
    {
        #region 属性
        private List<MonitorModel> _monitorSettings;
        [XmlArray("MonitorSettings"), XmlArrayItem("MonitorSetting")]
        public List<MonitorModel> MonitorSettings
        {
            get { return _monitorSettings; }
            set { _monitorSettings = value; }
        }
        private List<SubscribeModel> _subscribeSettings;
        [XmlArray("SubscribeSettings"), XmlArrayItem("SubscribeSetting")]
        public List<SubscribeModel> SubscribeSettings
        {
            get { return _subscribeSettings; }
            set { _subscribeSettings = value; }
        }
        private int _listenPort;
        [XmlElement("ListenPort")]
        public int ListenPort
        {
            get { return _listenPort; }
            set { _listenPort = value; }
        }

        private int _scanPeriod;
        [XmlElement("ScanPeriod")]
        public int ScanPeriod
        {
            get { return _scanPeriod; }
            set { _scanPeriod = value; }
        }

        private string _exceptionSavePath;
        [XmlElement("IncompleteSendSavePath")]
        public string ExceptionSavePath
        {
            get { return _exceptionSavePath; }
            set { _exceptionSavePath = value; }
        }


        #endregion

        #region 构造函数
        public ConfigClass()
        { }

        public ConfigClass(List<MonitorModel> monitors, List<SubscribeModel> subscribes, int listenPort, int scanPeriod, string savePath)
        {
            MonitorSettings = monitors;
            SubscribeSettings = subscribes;
            ListenPort = listenPort;
            ScanPeriod = scanPeriod;
            ExceptionSavePath = savePath;
        }
        #endregion
    }

}
