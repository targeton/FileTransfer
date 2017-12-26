using System;
using System.Windows;
using System.Windows.Data;

namespace FileTransfer.Converters
{
    class VisibilityToNullableBooleanConverter : IValueConverter
    {
        #region IValueConverter 成员

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Visibility)
            {
                return ((Visibility)value) == Visibility.Visible;
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool?)
            {
                return ((bool?)value) == true ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (value is bool)
            {
                return ((bool)value) == true ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        #endregion
    }
}
