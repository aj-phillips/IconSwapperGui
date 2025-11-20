using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IconSwapperGui.Utilities
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// When true, the conversion is inverted (true => Collapsed, false => Visible).
        /// Default is false.
        /// </summary>
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isTrue = false;

            if (value is bool b)
                isTrue = b;
            else if (value is bool?)
                isTrue = ((bool?)value) == true;

            if (Invert)
                isTrue = !isTrue;

            return isTrue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
