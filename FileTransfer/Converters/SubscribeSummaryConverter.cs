using FileTransfer.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace FileTransfer.Converters
{
    public class SubscribeSummaryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ObservableCollection<SubscribeInfoModel> infos = value as ObservableCollection<SubscribeInfoModel>;
            if (infos == null || infos.Count == 0) return @"当前无订阅";
            return string.Format("当前有{0}个订阅", infos.Count);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
