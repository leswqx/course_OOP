namespace MSM.Models.Entities;

public class RealtorSchedule : BaseEntity
{
    public int RealtorId { get; set; }
    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }
    public bool IsAvailable { get; set; } = true;

    public User? Realtor { get; set; }
}
