using System;
using System.Windows.Data;

namespace FileTransfer.Converters
{
    public class BoolInverseConverter : IValueConverter
    {
        #region IValueConverter 成员

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return false;
            else
                return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return false;
            else
                return true;
        }

        #endregion
    }
}
