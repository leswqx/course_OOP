namespace MSM.Models.Entities;

/// <summary>
/// Избранные объекты
/// </summary>
public class Favorite : BaseEntity
{
    public int UserId { get; set; }
    public int PropertyId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;

    // Навигационные свойства
    public User? User { get; set; }
    public Property? Property { get; set; }
}
