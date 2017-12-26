using FileTransfer.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace FileTransfer.Converters
{
    public class SubscribeNetworkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ObservableCollection<SubscribeInfoModel> infos = value as ObservableCollection<SubscribeInfoModel>;
            if (infos == null || infos.Count == 0) return @"当前无订阅";
            return string.Format("当前可连接{0}个订阅端，不可连接{1}个", infos.Where(i => i.CanConnect == true).Count(), infos.Where(i => i.CanConnect == false).Count());
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
