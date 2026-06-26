namespace MSM.Models.Entities;

public class PropertyImage : BaseEntity
{
    public int PropertyId { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }

    public Property? Property { get; set; }
}
