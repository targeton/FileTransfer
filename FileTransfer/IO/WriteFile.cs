using FileTransfer.DbHelper.Entitys;
using FileTransfer.LogToDb;
using FileTransfer.Utils;
using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.IO
{
    //TODO:完善文件流写逻辑（写入到订阅文件夹内）
    public class WriteFile : ProducerConsumerLite<WriteDataBuffer>
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(WriteFile));
        private string _directory = string.Empty;
        private string _monitorIP = string.Empty;
        private string _monitorAlias = string.Empty;
        private FileStream _writeStream = null;
        private long _currentFileSize = 0;
        private long _currentStreamIndex = 0;
        private string _currentFileName = string.Empty;
        #endregion

        #region 属性
        public bool IsException { get; set; }

        public string Directory
        {
            get { return _directory; }
        }
        #endregion

        #region 构造方法
        public WriteFile(string directory, string monitorIP, string monitorAlias)
        {
            _directory = directory;
            _monitorIP = monitorIP;
            _monitorAlias = monitorAlias;
        }
        #endregion

        #region 方法
        protected override void Consume(IEnumerable<WriteDataBuffer> items)
        {
            try
            {
                foreach (var item in items)
                {
                    switch (item.DataType)
                    {
                        case WriteDataType.FileSize:
                            _currentStreamIndex = 0;
                            _currentFileSize = BitConverter.ToInt64(item.DataBuffer, 0);
                            break;
                        case WriteDataType.FileName:
                            string receiveFileName = Encoding.Unicode.GetString(item.DataBuffer, 0, item.DataBuffer.Length);
                            //TODO:根据发送的文件名转换成接收的文件名
                            _currentFileName = Path.Combine(_directory, receiveFileName);
                            IOHelper.Instance.CheckAndCreateDirectory(_currentFileName);
                            LogHelper.Instance.ReceiveLogger.Add(new ReceiveLogEntity(DateTime.Now, _currentFileName, _monitorIP, _monitorAlias, @"开始接收"));
                            SimpleIoc.Default.GetInstance<MainViewModel>().ShowAcceptProgress(_monitorIP, _monitorAlias, _directory, _currentFileName, 0.0);
                            _writeStream = new FileStream(_currentFileName, FileMode.Create, FileAccess.Write);
                            break;
                        case WriteDataType.FileContent:
                            if (_writeStream == null)
                                break;
                            if (_currentFileSize <= 0)
                            {
                                SimpleIoc.Default.GetInstance<MainViewModel>().ShowAcceptProgress(_monitorIP, _monitorAlias, _directory, _currentFileName, 1.0);
                                _writeStream.Close();
                                LogHelper.Instance.ReceiveLogger.Add(new ReceiveLogEntity(DateTime.Now, _currentFileName, _monitorIP, _monitorAlias, @"完成接收"));
                                break;
                            }
                            _writeStream.Seek(_currentStreamIndex, SeekOrigin.Begin);
                            _writeStream.Write(item.DataBuffer, 0, item.DataBuffer.Length);
                            _currentStreamIndex += item.DataBuffer.Length;
                            SimpleIoc.Default.GetInstance<MainViewModel>().ShowAcceptProgress(_monitorIP, _monitorAlias, _directory, _currentFileName, _currentStreamIndex * 1.0 / _currentFileSize);
                            if (_currentStreamIndex >= _currentFileSize)
                            {
                                _writeStream.Close();
                                LogHelper.Instance.ReceiveLogger.Add(new ReceiveLogEntity(DateTime.Now, _currentFileName, _monitorIP, _monitorAlias, @"完成接收"));
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                string logMsg = string.Format("向{0}内写入文件{1}时发生异常{2}！", _directory, _currentFileName, e.Message);
                _logger.Error(logMsg);
                LogHelper.Instance.ErrorLogger.Add(new DbHelper.Entitys.ErrorLogEntity(DateTime.Now, "ERROR", logMsg));
            }

        }
        #endregion
    }
}
