namespace MSM.Models.Entities;

/// <summary>
/// Запись на просмотр объекта недвижимости
/// </summary>
public class Appointment : BaseEntity
{
    public int PropertyId { get; set; }
    public int ClientId { get; set; }
    public int RealtorId { get; set; }
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.New;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Навигационные свойства
    public Property? Property { get; set; }
    public User? Client { get; set; }
    public User? Realtor { get; set; }
}
