namespace MSM.Models.Entities;

/// <summary>
/// Изображение объекта недвижимости
/// </summary>
public class PropertyImage : BaseEntity
{
    public int PropertyId { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }

    // Навигационные свойства
    public Property? Property { get; set; }
}
