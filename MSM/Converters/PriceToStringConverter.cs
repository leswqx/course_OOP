using System.Globalization;
using System.Windows.Data;

namespace RealEstateAgency.Wpf.Converters;

/// <summary>
/// Конвертер цены в строку с форматированием
/// </summary>
public class PriceToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal price)
        {
            return price.ToString("N0", culture) + " ₽";
        }
        return "0 ₽";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && decimal.TryParse(str.Replace(" ₽", ""), NumberStyles.Number, culture, out var result))
        {
            return result;
        }
        return 0m;
    }
}
