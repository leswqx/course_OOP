using System.Globalization;
using System.Windows.Data;
using MSM.Models.Entities;

namespace MSM.Converters;

public class AppointmentStatusToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is AppointmentStatus s ? s switch
        {
            AppointmentStatus.New       => "Новая",
            AppointmentStatus.Confirmed => "Подтверждена",
            AppointmentStatus.Cancelled => "Отменена",
            AppointmentStatus.Completed => "Завершена",
            _                           => value.ToString() ?? ""
        } : value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Новая"        => AppointmentStatus.New,
            "Подтверждена" => AppointmentStatus.Confirmed,
            "Отменена"     => AppointmentStatus.Cancelled,
            "Завершена"    => AppointmentStatus.Completed,
            _              => AppointmentStatus.New
        };
    }
}
