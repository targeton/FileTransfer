using GalaSoft.MvvmLight.Ioc;

namespace FileTransfer.ViewModels
{
    public class ViewModelLocator
    {
        #region 构造函数
        public ViewModelLocator()
        {
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<SubscribeViewModel>();
            SimpleIoc.Default.Register<LogsQueryViewModel>();
            SimpleIoc.Default.Register<AddMonitorViewModel>();
        }
        #endregion

        #region 属性
        public MainViewModel MainViewModel
        {
            get
            {
                return SimpleIoc.Default.GetInstance<MainViewModel>();
            }
        }

        public SubscribeViewModel SubscribeViewModel
        {
            get
            {
                return SimpleIoc.Default.GetInstance<SubscribeViewModel>();
            }
        }

        public LogsQueryViewModel LogsQueryViewModel
        {
            get
            {
                return SimpleIoc.Default.GetInstance<LogsQueryViewModel>();
            }
        }

        public AddMonitorViewModel AddMonitorViewModel
        {
            get 
            {
                return SimpleIoc.Default.GetInstance<AddMonitorViewModel>();
            }
        }
        #endregion

    }
}
