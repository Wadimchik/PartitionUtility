using System;
using System.Globalization;
using System.Windows.Data;

namespace PartitionUtility
{
    public class BoolCalcAdditionalToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null) return (bool)value ? @"\Resources\Images\set_additional.png" : @"\Resources\Images\calculate_additional.png";
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
