using System.Windows;

namespace PartitionUtility
{
    public class ColumnHelper
    {
        public static string GetSummaryText(DependencyObject obj)
        {
            return (string)obj.GetValue(SummaryTextProperty);
        }
        public static void SetSummaryText(DependencyObject obj, string value)
        {
            obj.SetValue(SummaryTextProperty, value);
        }
        public static readonly DependencyProperty SummaryTextProperty = DependencyProperty.RegisterAttached("SummaryText", typeof(string), typeof(ColumnHelper), new PropertyMetadata(null));

        public static Thickness GetSummaryTextMargin(DependencyObject obj)
        {
            return (Thickness)obj.GetValue(SummaryTextMarginProperty);
        }
        public static void SetSummaryTextMargin(DependencyObject obj, string value)
        {
            var converter = new ThicknessConverter();
            obj.SetValue(SummaryTextMarginProperty, converter.ConvertFrom(value));
        }
        public static readonly DependencyProperty SummaryTextMarginProperty = DependencyProperty.RegisterAttached("SummaryTextMargin", typeof(Thickness), typeof(ColumnHelper), new PropertyMetadata(null));
    }
}
