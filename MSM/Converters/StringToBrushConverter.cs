using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MSM.Converters;

public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(s);
                return new SolidColorBrush(color);
            }
            catch { }
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
