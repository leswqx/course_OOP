namespace MSM.Models.Entities;

public class User : BaseEntity
{
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public byte[]? AvatarPhoto { get; set; }
    public bool IsBlocked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Property>? Properties { get; set; }
    public ICollection<Appointment>? ClientAppointments { get; set; }
    public ICollection<Appointment>? RealtorAppointments { get; set; }
    public ICollection<Favorite>? Favorites { get; set; }
    public ICollection<Review>? Reviews { get; set; }
    public ICollection<RealtorSchedule>? Schedules { get; set; }
}
