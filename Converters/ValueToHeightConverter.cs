using System;
using System.Globalization;
using System.Windows.Data;

namespace MP3Pro
{
    public class ValueToHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is float peak && values[1] is double actualHeight)
            {
                return peak * (actualHeight * 0.85); // %85 doluluk oranı
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}