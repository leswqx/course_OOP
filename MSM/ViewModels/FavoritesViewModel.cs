using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

public partial class FavoritesViewModel : ViewModelBase
{
    private const int PageSize = 9;

    private readonly IFavoriteService _favoriteService;
    private readonly INavigationService _navigationService;

    private int _loadedCount;
    private int _totalCount;

    [ObservableProperty] private ObservableCollection<PropertyCardViewModel> _properties = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsContentVisible))]
    private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsContentVisible))]
    private bool _isEmpty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    [NotifyPropertyChangedFor(nameof(IsContentVisible))]
    private string? _errorMessage;

    public bool HasErrorMessage => ErrorMessage != null;
    public bool IsContentVisible => !IsLoading && !IsEmpty && !HasErrorMessage;
    public bool CanShowMore   => _totalCount > Properties.Count;
    public int  ShowMoreCount => Math.Max(0, _totalCount - Properties.Count);

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
        _loadedCount = 0;
        try
        {
            _totalCount = await _favoriteService.GetUserFavoritesCountAsync(Session.CurrentUser.Id);
            var items = await _favoriteService.GetUserFavoritesPagedAsync(Session.CurrentUser.Id, 0, PageSize);
            Properties.Clear();
            foreach (var p in items) Properties.Add(new PropertyCardViewModel(p));
            _loadedCount = Properties.Count;
            IsEmpty = Properties.Count == 0;
            OnPropertyChanged(nameof(CanShowMore));
            OnPropertyChanged(nameof(ShowMoreCount));
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ShowMoreAsync()
    {
        if (Session.CurrentUser == null || !CanShowMore || IsLoading) return;
        IsLoading = true;
        try
        {
            var items = await _favoriteService.GetUserFavoritesPagedAsync(Session.CurrentUser.Id, _loadedCount, PageSize);
            foreach (var p in items) Properties.Add(new PropertyCardViewModel(p));
            _loadedCount = Properties.Count;
            OnPropertyChanged(nameof(CanShowMore));
            OnPropertyChanged(nameof(ShowMoreCount));
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void OpenDetail(PropertyCardViewModel card)
    {
        if (card.IsHidden) return;
        _navigationService.NavigateTo<PropertyDetailViewModel>(card.Id);
    }

    [RelayCommand]
    private async Task RemoveFromFavoritesAsync(PropertyCardViewModel card)
    {
        if (Session.CurrentUser == null) return;
        try
        {
            await _favoriteService.RemoveFromFavoritesAsync(Session.CurrentUser.Id, card.Id);
            Properties.Remove(card);
            _loadedCount = Properties.Count;
            _totalCount  = Math.Max(0, _totalCount - 1);
            IsEmpty = Properties.Count == 0;
            OnPropertyChanged(nameof(CanShowMore));
            OnPropertyChanged(nameof(ShowMoreCount));
        }
        catch (Exception ex) { ErrorMessage = $"Не удалось убрать из избранного: {ex.Message}"; }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();
}
