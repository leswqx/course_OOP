using System.Globalization;
using System.Windows.Data;

namespace MSM.Converters;

/// <summary>
/// Конвертер цены в строку с форматированием
/// </summary>
public class PriceToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal price)
        {
            return price.ToString("N0", culture) + " BYN";
        }
        return "0 BYN";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && decimal.TryParse(str.Replace(" BYN", ""), NumberStyles.Number, culture, out var result))
        {
            return result;
        }
        return 0m;
    }
}
