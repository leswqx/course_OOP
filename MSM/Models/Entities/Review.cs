namespace MSM.Models.Entities;

/// <summary>
/// Отзыв
/// </summary>
public class Review : BaseEntity
{
    public int UserId { get; set; }
    public int? PropertyId { get; set; }
    public int? RealtorId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Навигационные свойства
    public User? User { get; set; }
    public Property? Property { get; set; }
    public User? Realtor { get; set; }
}
