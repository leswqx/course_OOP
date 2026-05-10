namespace MSM.Models.Entities;

/// <summary>
/// Объект недвижимости
/// </summary>
public class Property : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double Area { get; set; }
    public int Rooms { get; set; }
    public int? Bathrooms { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string Address { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty; // house, apartment, complex
    public int? Floor { get; set; }
    public int? TotalFloors { get; set; }
    public int? YearBuilt { get; set; }
    public bool HasRepair { get; set; }
    public bool MortgageAvailable { get; set; }
    public string Status { get; set; } = "active"; // active, sold, hidden
    public int RealtorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Навигационные свойства
    public User? Realtor { get; set; }
    public ICollection<PropertyImage>? Images { get; set; }
    public ICollection<Appointment>? Appointments { get; set; }
    public ICollection<Favorite>? Favorites { get; set; }
    public ICollection<Review>? Reviews { get; set; }
}
