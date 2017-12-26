using log4net;
using System.Windows;

namespace FileTransfer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(App));
        #endregion

        #region 事件
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.Error(string.Format("应用程序FileTransfer发生了未知的异常！异常为：{0}", e.Exception.Message));
            MessageBox.Show("应用程序遇到异常问题！请检查！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            this.Shutdown();
        }
        #endregion

    }
}
