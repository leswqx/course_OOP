using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MSM.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter?.ToString()?.ToLower() == "invert";
        bool visible;

        if (value is bool b)
            visible = b;
        else if (value is string s)
            visible = !string.IsNullOrEmpty(s);
        else if (value is int i)
            visible = i > 0;
        else
            visible = value != null;

        if (invert) visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}
