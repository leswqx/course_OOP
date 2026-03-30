using System.Globalization;
using System.Windows.Data;

namespace RealEstateAgency.Wpf.Converters;

/// <summary>
/// Конвертер статуса записи в русский текст
/// </summary>
public class AppointmentStatusToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "new" => "Новая",
            "confirmed" => "Подтверждена",
            "cancelled" => "Отменена",
            "completed" => "Завершена",
            _ => value?.ToString() ?? "Неизвестно"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Новая" => "new",
            "Подтверждена" => "confirmed",
            "Отменена" => "cancelled",
            "Завершена" => "completed",
            _ => value?.ToString() ?? "new"
        };
    }
}
