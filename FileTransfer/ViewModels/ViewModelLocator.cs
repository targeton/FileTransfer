using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        #endregion

    }
}
