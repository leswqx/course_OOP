using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

public partial class LandingViewModel : ViewModelBase
{
    private readonly INavigationService _nav;

    [ObservableProperty] private string _selectedPropertyType = "Квартира";
    [ObservableProperty] private string _selectedRooms = "Любое";
    [ObservableProperty] private string _maxPrice = "";

    public List<string> PropertyTypes { get; } = ["Квартира", "Дом", "Коммерческая"];
    public List<string> RoomOptions   { get; } = ["Любое", "1", "2", "3", "4+"];

    public LandingViewModel(INavigationService nav) => _nav = nav;

    partial void OnMaxPriceChanged(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits != value) MaxPrice = digits;
    }

    [RelayCommand]
    private void SetTagSearch(string tag)
    {
        switch (tag)
        {
            case "apartment_2k":
                SelectedPropertyType = "Квартира";
                SelectedRooms = "2";
                break;
            case "apartment_new":
                SelectedPropertyType = "Квартира";
                SelectedRooms = "Любое";
                break;
            case "house":
                SelectedPropertyType = "Дом";
                SelectedRooms = "Любое";
                break;
        }
        MaxPrice = "";
        ShowProperties();
    }

    [RelayCommand]
    private void ShowProperties()
    {
        if (Session.CurrentUser == null)
        {
            _nav.NavigateTo<LoginViewModel>();
            return;
        }

        var typeMap = new Dictionary<string, string>
        {
            ["Квартира"]     = "apartment",
            ["Дом"]          = "house",
            ["Коммерческая"] = "complex"
        };
        typeMap.TryGetValue(SelectedPropertyType, out var mappedType);

        string? minRooms = null, maxRooms = null;
        if (SelectedRooms != "Любое")
        {
            if (SelectedRooms == "4+") minRooms = "4";
            else { minRooms = SelectedRooms; maxRooms = SelectedRooms; }
        }

        _nav.NavigateTo<PropertyListViewModel>(new LandingSearchParams(
            PropertyType: mappedType,
            MaxPrice:     string.IsNullOrWhiteSpace(MaxPrice) ? null : MaxPrice,
            MinRooms:     minRooms,
            MaxRooms:     maxRooms));
    }
}
