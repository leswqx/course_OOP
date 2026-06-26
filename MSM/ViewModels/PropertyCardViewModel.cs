using System.IO;
using System.Windows.Media.Imaging;
using MSM.Models.Entities;

namespace MSM.ViewModels;

public class PropertyCardViewModel
{
    public int Id { get; }
    public string Title { get; }
    public decimal Price { get; }
    public double Area { get; }
    public int Rooms { get; }
    public int? Bathrooms { get; }
    public string City { get; }
    public string PropertyType { get; }
    public string RealtorName { get; }
    public BitmapImage? MainImage { get; }

    public string Status { get; }
    public bool IsFavorite { get; init; }
    public bool HasImage => MainImage != null;
    public bool IsSoldable => Status == "active";
    public bool IsSold => Status == "sold";
    public bool IsHidden => Status == "hidden";
    public bool IsRestorable => Status == "sold";
    public bool IsToggleHideVisible => Status != "sold";
    public string ToggleHideLabel => Status == "hidden" ? "👁 Показать" : "🙈 Скрыть";
    public string AreaRoomsInfo => $"{Area:F0} м²  •  {Rooms} комн.";
    public string PropertyTypeDisplay => PropertyType switch
    {
        "apartment" => "Квартира",
        "house" => "Дом",
        "complex" => "Комплекс",
        _ => PropertyType
    };

    public PropertyCardViewModel(Property property)
    {
        Id = property.Id;
        Title = property.Title;
        Status = property.Status;
        Price = property.Price;
        Area = property.Area;
        Rooms = property.Rooms;
        Bathrooms = property.Bathrooms;
        City = property.City;
        PropertyType = property.PropertyType;
        RealtorName = property.Realtor?.FullName ?? "";

        var imageData = property.Images?.FirstOrDefault(i => i.IsMain)?.ImageData
                     ?? property.Images?.FirstOrDefault()?.ImageData;

        if (imageData is { Length: > 0 })
        {
            try
            {
                var img = new BitmapImage();
                using var ms = new MemoryStream(imageData);
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = ms;
                img.EndInit();
                img.Freeze();
                MainImage = img;
            }
            catch {  }
        }
    }
}
