namespace MSM.Models.Entities;

/// <summary>
/// Пользователь системы (администратор, риелтор, клиент)
/// </summary>
public class User : BaseEntity
{
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // admin, realtor, client
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Навигационные свойства
    public ICollection<Property>? Properties { get; set; }
    public ICollection<Appointment>? ClientAppointments { get; set; }
    public ICollection<Appointment>? RealtorAppointments { get; set; }
    public ICollection<Favorite>? Favorites { get; set; }
    public ICollection<Review>? Reviews { get; set; }
    public ICollection<RealtorSchedule>? Schedules { get; set; }
}
