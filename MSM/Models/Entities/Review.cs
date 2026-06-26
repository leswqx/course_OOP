namespace MSM.Models.Entities;

public class Review : BaseEntity
{
    public int UserId { get; set; }
    public int? PropertyId { get; set; }
    public int? RealtorId { get; set; }
    public int? AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public User? User { get; set; }
    public Property? Property { get; set; }
    public User? Realtor { get; set; }
    public Appointment? Appointment { get; set; }
}
