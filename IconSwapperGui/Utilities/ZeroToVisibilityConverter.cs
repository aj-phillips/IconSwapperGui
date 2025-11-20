using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace IconSwapperGui.Utilities
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class ZeroToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// When true, inversion is applied (zero => Visible, non-zero => Collapsed).
        /// </summary>
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isZero = IsZero(value);
            var visible = isZero;

            if (Invert)
                visible = !visible;

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool IsZero(object value)
        {
            if (value == null) return true;

            return value switch
            {
                int i => i == 0,
                long l => l == 0L,
                double d => Math.Abs(d) < double.Epsilon,
                float f => Math.Abs(f) < float.Epsilon,
                decimal m => m == 0m,
                string s => s.Length == 0,
                ICollection col => col.Count == 0,
                IEnumerable en => !en.Cast<object>().Any(),
                _ => false,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
