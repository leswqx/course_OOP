namespace MSM.Models.Entities;

public class PriceHistory : BaseEntity
{
    public int PropertyId { get; set; }
    public decimal Price { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    public Property? Property { get; set; }
}
