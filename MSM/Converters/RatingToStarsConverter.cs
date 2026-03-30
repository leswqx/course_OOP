using System.Globalization;
using System.Windows.Data;

namespace RealEstateAgency.Wpf.Converters;

/// <summary>
/// Конвертер рейтинга в строку со звездами
/// </summary>
public class RatingToStarsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int rating)
        {
            return new string('★', rating) + new string('☆', 5 - rating);
        }
        if (value is double doubleRating)
        {
            var rounded = (int)Math.Round(doubleRating);
            return new string('★', rounded) + new string('☆', 5 - rounded);
        }
        return "☆☆☆☆☆";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str.Count(c => c == '★');
        }
        return 0;
    }
}
