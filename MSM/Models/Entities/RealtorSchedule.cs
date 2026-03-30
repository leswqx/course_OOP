namespace MSM.Models.Entities;

/// <summary>
/// График работы риелтора
/// </summary>
public class RealtorSchedule : BaseEntity
{
    public int RealtorId { get; set; }
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public bool IsAvailable { get; set; } = true;

    // Навигационные свойства
    public User? Realtor { get; set; }
}
