using System;
using System.Windows.Data;

namespace FileTransfer.Converters
{
    class MonitorFlagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return @"启动监控";
            else
                return @"关闭监控";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
