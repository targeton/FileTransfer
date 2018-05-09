using System;
using System.Windows.Data;

namespace FileTransfer.Converters
{
    public class ConnectInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return @"正常连接中";
            else
                return @"无法建立连接";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
