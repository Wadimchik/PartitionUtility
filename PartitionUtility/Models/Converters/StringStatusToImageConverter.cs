using System;
using System.Globalization;
using System.Windows.Data;

namespace PartitionUtility
{
    public class StringStatusToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((string)value == "Ok" || (string)value == "Ок") 
                {
                    return @"\Resources\Images\ok.png";
                }
                if ((string)value == "Info") 
                {
                    return @"\Resources\Images\info.png";
                }
                if ((string)value == "Warning" || (string)value == "Предупреждение")
                {
                    return @"\Resources\Images\warning.png";
                }
                if ((string)value == "Error" || (string)value == "Ошибка") 
                {
                    return @"\Resources\Images\error_36.png";
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
