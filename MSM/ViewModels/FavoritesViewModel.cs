using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

// Список избранных объектов клиента
public partial class FavoritesViewModel : ViewModelBase
{
    private readonly IFavoriteService _favoriteService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private ObservableCollection<PropertyCardViewModel> _properties = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsContentVisible))]
    private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsContentVisible))]
    private bool _isEmpty;
    [ObservableProperty] private string? _errorMessage;

    public bool IsContentVisible => !IsLoading && !IsEmpty;

    public FavoritesViewModel(IFavoriteService favoriteService, INavigationService navigationService)
    {
        _favoriteService = favoriteService;
        _navigationService = navigationService;
    }

    public override void OnNavigatedTo(object? parameter) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        if (Session.CurrentUser == null) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var items = await _favoriteService.GetUserFavoritesAsync(Session.CurrentUser.Id);
            Properties.Clear();
            foreach (var p in items) Properties.Add(new PropertyCardViewModel(p));
            IsEmpty = Properties.Count == 0;
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void OpenDetail(PropertyCardViewModel card) =>
        _navigationService.NavigateTo<PropertyDetailViewModel>(card.Id);

    [RelayCommand]
    private async Task RemoveFromFavoritesAsync(PropertyCardViewModel card)
    {
        if (Session.CurrentUser == null) return;
        try
        {
            await _favoriteService.RemoveFromFavoritesAsync(Session.CurrentUser.Id, card.Id);
            Properties.Remove(card);
            IsEmpty = Properties.Count == 0;
        }
        catch { /* некритично */ }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();
}
