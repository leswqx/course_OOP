namespace MSM.Models.Entities;

public class Favorite : BaseEntity
{
    public int UserId { get; set; }
    public int PropertyId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;

    public User? User { get; set; }
    public Property? Property { get; set; }
}
