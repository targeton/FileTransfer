using System.Collections.Generic;

namespace FileTransfer.FileWatcher
{
    public class MonitorChanges
    {
        #region 属性
        private string _monitorAlias;

        public string MonitorAlias
        {
            get { return _monitorAlias; }
            set { _monitorAlias = value; }
        }

        private string _monitorDirectory;

        public string MonitorDirectory
        {
            get { return _monitorDirectory; }
            set { _monitorDirectory = value; }
        }


        private List<string> _subscribeIPs;

        public List<string> SubscribeIPs
        {
            get { return _subscribeIPs; }
            set { _subscribeIPs = value; }
        }

        private List<string> _fileChanges;

        public List<string> FileChanges
        {
            get { return _fileChanges; }
            set { _fileChanges = value; }
        }


        #endregion
    }
}
