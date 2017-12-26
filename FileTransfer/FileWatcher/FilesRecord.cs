using System.Collections.Generic;
using FileTransfer.Models;

namespace FileTransfer.FileWatcher
{
    public class FilesRecord
    {
        #region 属性
        public string MonitorAlias { get; set; }
        public string MonitorDirectory { get; set; }
        public SubscribeInfoModel SubscribeInfo { get; set; }
        public List<string> IncrementFiles { get; set; }
        public List<string> IncompleteSendFiles { get; set; }
        #endregion
    }
}
