using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Sockets;
using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using log4net;

namespace FileTransfer.FileWatcher
{
    /// <summary>
    /// 处理监控文件夹内增量文件的发送
    /// </summary>
    public class SendFileProcess
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(SendFileProcess));
        private ConcurrentQueue<List<string>> _queue = new ConcurrentQueue<List<string>>();
        private Task _sendTask;
        #endregion

        #region 属性
        private string _monitorDirectory;
        public string MonitorDirectroy
        {
            get { return _monitorDirectory; }
        }
        #endregion

        #region 构造函数
        public SendFileProcess(string monitorDirectory)
        {
            _monitorDirectory = monitorDirectory;
        }
        #endregion

        #region 方法
        public void Add(List<string> incrementalFiles)
        {
            if (_sendTask != null && _sendTask.IsCompleted == false)
            {
                _queue.Enqueue(incrementalFiles);
            }
            else
            {
                try
                {
                    _sendTask = Task.Factory.StartNew((obj) =>
                    {
                        List<string> files = obj as List<string>;
                        if (files == null) return;
                        while (files.Count > 0)
                        {
                            Task<FilesRecord>[] tasks = SynchronousSocketManager.Instance.SendFiles(_monitorDirectory, files);
                            if (tasks != null)
                            {
                                Task contiuneTask = SaveOrDeleteFiles(tasks);
                                contiuneTask.Wait();
                            }
                            if (_queue.IsEmpty || !_queue.TryDequeue(out files))
                                files = new List<string>();
                        }
                    }, incrementalFiles);
                }
                catch (Exception e)
                {
                    _logger.Warn(string.Format("{0}下的文件发送线程发生异常！异常为：{1}", _monitorDirectory, e.Message));
                }
            }
        }

        private Task SaveOrDeleteFiles(Task<FilesRecord>[] tasks)
        {
            return Task.Factory.ContinueWhenAll(tasks, (tasksArray) =>
            {
                if (tasksArray == null || tasksArray.Length == 0) return;
                List<FilesRecord> records = new List<FilesRecord>();
                for (int i = 0; i < tasksArray.Length; i++)
                {
                    if (tasksArray[i].Result == null) continue;
                    records.Add(tasksArray[i].Result);
                }
                if (records.Count == 0) return;
                string directory = records.First().MonitorDirectory;
                List<string> incrementFiles = records.First().IncrementFiles;
                List<string> incompleteSendFiles = new List<string>();
                records.ForEach(r => incompleteSendFiles.AddRange(r.IncompleteSendFiles));
                incompleteSendFiles = incompleteSendFiles.Distinct().ToList();
                if (incompleteSendFiles != null && incompleteSendFiles.Count > 0)
                {
                    var savePath = SimpleIoc.Default.GetInstance<MainViewModel>().SendExceptionSavePath;
                    IOHelper.Instance.SaveUnsendedFiles(incompleteSendFiles, savePath);
                }
                if (incrementFiles != null && incrementFiles.Count > 0)
                {
                    foreach (var file in incrementFiles)
                    {
                        IOHelper.Instance.TryDeleteFile(directory, file);
                    }
                }
                IOHelper.Instance.TryDeleteSubdirectories(directory);
            });
        }
        #endregion

    }
}
