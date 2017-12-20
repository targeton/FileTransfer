using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Sockets;

namespace FileTransfer.IO
{
    /// <summary>
    /// 管理类（控制多个WriteFile对象并发向各自文件内进行写入操作）
    /// </summary>
    public class WriteFileManager
    {
        #region 变量
        private ILog _logger = LogManager.GetLogger(typeof(WriteFileManager));
        private List<WriteFile> _writers = null;
        #endregion

        #region 构造函数
        public WriteFileManager(string monitorIP, string monitorDirectory)
        {
            List<string> acceptDirectories = GetAcceptDirectories(monitorIP, monitorDirectory);
            _writers = new List<WriteFile>();
            foreach (var directory in acceptDirectories)
            {
                _writers.Add(new WriteFile(directory, monitorIP, monitorDirectory));
            }
        }

        private List<string> GetAcceptDirectories(string monitorIP, string monitorDirectory)
        {
            List<string> acceptDirectories = SimpleIoc.Default.GetInstance<MainViewModel>().SubscribeCollection.Where(s => s.MonitorIP == monitorIP && s.MonitorDirectory == monitorDirectory).Select(s => s.AcceptDirectory).ToList();
            if (acceptDirectories.Count == 0)
            {
                string savePath = SimpleIoc.Default.GetInstance<MainViewModel>().SendExceptionSavePath;
                string logMsg = string.Format("{0}发送来的文件无接收设置，转存至{1}！", monitorIP, savePath);
                _logger.Warn(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new ErrorLogEntity(DateTime.Now, "WARN", logMsg));
                string.Format("{0}:{1}发送来的文件无接收设置，转存至{2}！", DateTime.Now, monitorIP, savePath).RefreshUINotifyText();
                acceptDirectories.Add(savePath);
            }
            return acceptDirectories;
        }
        #endregion

        #region 方法
        public void Add(WriteDataBuffer dataBuffer)
        {
            foreach (var writer in _writers)
            {
                if (writer.IsException)
                    continue;
                writer.Add(dataBuffer);
            }
        }
        #endregion

    }
}
