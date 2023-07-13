using System;
using System.Globalization;
using System.Windows.Data;

namespace PartitionUtility
{
    public class IntToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((int)value == 0)
                {
                    return "#039C23";
                }
                if ((int)value == 1)
                {
                    return "#FFB115";
                }
                if ((int)value == 2)
                {
                    return "#F71B38";
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
