using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IconSwapperGui.Utilities
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// If true, null becomes Visible and non-null becomes Collapsed.
        /// Default: false (null => Collapsed, non-null => Visible)
        /// </summary>
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isNull = value == null;
            var visible = !isNull;

            if (Invert)
                visible = !visible;

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
