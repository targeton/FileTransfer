using FileTransfer.IO;
using FileTransfer.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using System.Linq;
using System.Windows.Forms;

namespace FileTransfer.ViewModels
{
    public class AddMonitorViewModel : ViewModelBase
    {
        #region 变量

        #endregion

        #region 属性
        private string _monitorAlias;

        public string MonitorAlias
        {
            get { return _monitorAlias; }
            set
            {
                _monitorAlias = value;
                RaisePropertyChanged("MonitorAlias");
            }
        }

        private string _monitorDirectory;

        public string MonitorDirectory
        {
            get { return _monitorDirectory; }
            set
            {
                _monitorDirectory = value;
                RaisePropertyChanged("MonitorDirectory");
            }
        }


        #endregion

        #region 命令
        public RelayCommand SelectMonitorPathCommand { get; set; }
        public RelayCommand ConfirmCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
        #endregion

        #region 构造函数
        public AddMonitorViewModel()
        {
            InitialCommands();
        }
        #endregion

        #region 方法
        private void InitialCommands()
        {
            SelectMonitorPathCommand = new RelayCommand(ExecuteSelectMonitorPathCommand);
            ConfirmCommand = new RelayCommand(ExecuteConfirmCommand, CanExecuteConfirmCommand);
            CancelCommand = new RelayCommand(ExecuteCancelCommand);
        }

        private void ExecuteSelectMonitorPathCommand()
        {
            MonitorDirectory = IOHelper.Instance.SelectFloder(@"请选择监控文件夹目录");
        }

        private void ExecuteConfirmCommand()
        {
            string exceptionSavePath = SimpleIoc.Default.GetInstance<MainViewModel>().ExceptionSavePath;
            if (IOHelper.Instance.IsConflict(_monitorDirectory, exceptionSavePath))
            {
                MessageBox.Show("所选文件夹与发送异常转存路径冲突！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.FirstOrDefault(m => m.MonitorDirectory == _monitorDirectory) == null
                && SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.FirstOrDefault(m => m.MonitorAlias == _monitorAlias) == null)
            {
                SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.Add(new MonitorModel()
                {
                    MonitorAlias = _monitorAlias,
                    MonitorDirectory = _monitorDirectory,
                    DeleteFiles = true
                });
            }
            else
            {
                MessageBox.Show("所设置的监控与已有项冲突！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Messenger.Default.Send<string>("CloseAddMonitorView");
        }

        private bool CanExecuteConfirmCommand()
        {
            return !string.IsNullOrEmpty(MonitorAlias) && !string.IsNullOrEmpty(MonitorDirectory);
        }

        private void ExecuteCancelCommand()
        {
            Messenger.Default.Send<string>("CloseAddMonitorView");
        }
        #endregion

    }
}
