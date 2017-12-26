using System;
using System.Windows.Data;
using System.Windows.Media;

namespace FileTransfer.Converters
{
    public class ConnectBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return Brushes.LightGreen;
            else
                return Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
