using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Models.Entities;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

// ViewModel страницы детального просмотра объекта недвижимости.
// Управляет каруселью изображений, избранным и формой записи на просмотр.
public partial class PropertyDetailViewModel : ViewModelBase
{
    private readonly IPropertyService _propertyService;
    private readonly IFavoriteService _favoriteService;
    private readonly IAppointmentService _appointmentService;
    private readonly INavigationService _navigationService;

    private List<BitmapImage> _images = new();

    [ObservableProperty] private Property? _property;
    [ObservableProperty] private BitmapImage? _currentImage;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    private bool _isFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageCounter))]
    private int _currentImageIndex;

    // ===== Форма записи на просмотр =====
    [ObservableProperty] private bool _isScheduleFormVisible;
    [ObservableProperty] private DateTime _scheduleDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private string _scheduleTimeSlot = "10:00";
    [ObservableProperty] private string _scheduleComment = "";
    [ObservableProperty] private bool _scheduleSuccess;       // → DynamicResource Schedule.Success
    [ObservableProperty] private bool _scheduleSlotTaken;    // → DynamicResource Schedule.SlotTaken
    [ObservableProperty] private string? _scheduleErrorText; // произвольная ошибка сервиса
    [ObservableProperty] private bool _isSubmittingSchedule;

    // Доступные временные слоты (09:00–18:00)
    public List<string> TimeSlots { get; } =
        Enumerable.Range(9, 10).Select(h => $"{h:D2}:00").ToList();

    // --- Вычисляемые свойства ---

    public string ImageCounter => _images.Count > 0
        ? $"{CurrentImageIndex + 1} / {_images.Count}" : "";

    public bool HasMultipleImages => _images.Count > 1;
    public bool HasImages         => _images.Count > 0;
    public bool IsClient          => Session.IsClient;
    public bool IsBookable        => Session.IsClient && Property?.Status == "active";

    public string FavoriteButtonText => IsFavorite ? "★  В избранном" : "☆  В избранное";

    public string PropertyTypeDisplay => Property?.PropertyType switch
    {
        "apartment" => "Квартира",
        "house"     => "Дом",
        "complex"   => "Комплекс",
        _           => Property?.PropertyType ?? ""
    };

    public string StatusDisplay => Property?.Status switch
    {
        "active" => "Активен",
        "sold"   => "Продан",
        "hidden" => "Скрыт",
        _        => ""
    };

    public string FloorInfo
    {
        get
        {
            if (Property == null) return "";
            if (Property.Floor.HasValue && Property.TotalFloors.HasValue)
                return $"{Property.Floor} / {Property.TotalFloors}";
            return Property.Floor.HasValue ? Property.Floor.ToString()! : "—";
        }
    }

    public PropertyDetailViewModel(
        IPropertyService propertyService,
        IFavoriteService favoriteService,
        IAppointmentService appointmentService,
        INavigationService navigationService)
    {
        _propertyService = propertyService;
        _favoriteService = favoriteService;
        _appointmentService = appointmentService;
        _navigationService = navigationService;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        if (parameter is int id)
            _ = LoadAsync(id);
    }

    private async Task LoadAsync(int id)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Property = await _propertyService.GetByIdAsync(id);
            if (Property == null) { ErrorMessage = "Объект не найден"; return; }

            _images = (Property.Images?
                .OrderByDescending(i => i.IsMain)
                .ThenBy(i => i.SortOrder)
                .Select(i => ToImage(i.ImageData))
                .OfType<BitmapImage>()
                .ToList()) ?? new List<BitmapImage>();

            CurrentImageIndex = 0;
            CurrentImage = _images.FirstOrDefault();
            OnPropertyChanged(nameof(ImageCounter));
            OnPropertyChanged(nameof(HasMultipleImages));
            OnPropertyChanged(nameof(HasImages));
            OnPropertyChanged(nameof(PropertyTypeDisplay));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(FloorInfo));
            OnPropertyChanged(nameof(IsBookable));

            if (Session.IsClient && Session.CurrentUser != null)
                IsFavorite = await _favoriteService.IsFavoriteAsync(Session.CurrentUser.Id, id);
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void NextImage()
    {
        if (_images.Count == 0) return;
        CurrentImageIndex = (CurrentImageIndex + 1) % _images.Count;
        CurrentImage = _images[CurrentImageIndex];
    }

    [RelayCommand]
    private void PreviousImage()
    {
        if (_images.Count == 0) return;
        CurrentImageIndex = (CurrentImageIndex - 1 + _images.Count) % _images.Count;
        CurrentImage = _images[CurrentImageIndex];
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Session.CurrentUser == null || Property == null) return;
        try
        {
            if (IsFavorite)
                await _favoriteService.RemoveFromFavoritesAsync(Session.CurrentUser.Id, Property.Id);
            else
                await _favoriteService.AddToFavoritesAsync(Session.CurrentUser.Id, Property.Id);
            IsFavorite = !IsFavorite;
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
    }

    // Показывает / скрывает форму записи, сбрасывает состояние
    [RelayCommand]
    private void ToggleScheduleForm()
    {
        IsScheduleFormVisible = !IsScheduleFormVisible;
        ScheduleSuccess = ScheduleSlotTaken = false;
        ScheduleErrorText = null;
        ScheduleComment = "";
    }

    // Создаёт запись на просмотр в БД
    [RelayCommand]
    private async Task SubmitAppointmentAsync()
    {
        if (Property == null || Session.CurrentUser == null) return;
        if (!TimeSpan.TryParse(ScheduleTimeSlot, out var time)) return;

        var slotStart = ScheduleDate.Date + time;
        var slotEnd   = slotStart.AddHours(1);

        IsSubmittingSchedule = true;
        ScheduleSuccess = ScheduleSlotTaken = false;
        ScheduleErrorText = null;

        try
        {
            var available = await _appointmentService.IsSlotAvailableAsync(
                Property.RealtorId, slotStart, slotEnd);

            if (!available)
            {
                ScheduleSlotTaken = true; // View покажет {DynamicResource Schedule.SlotTaken}
                return;
            }

            await _appointmentService.CreateAsync(
                Property.Id,
                Session.CurrentUser.Id,
                Property.RealtorId,
                slotStart,
                slotEnd,
                string.IsNullOrWhiteSpace(ScheduleComment) ? null : ScheduleComment);

            ScheduleSuccess = true; // View покажет {DynamicResource Schedule.Success}
            ScheduleComment = "";
        }
        catch (Exception ex)
        {
            ScheduleErrorText = $"Ошибка: {ex.Message}";
        }
        finally { IsSubmittingSchedule = false; }
    }

    [RelayCommand]
    private void GoToRealtorProfile()
    {
        if (Property == null) return;
        _navigationService.NavigateTo<RealtorProfileViewModel>(Property.RealtorId);
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();

    private static BitmapImage? ToImage(byte[] data)
    {
        if (data.Length == 0) return null;
        try
        {
            var img = new BitmapImage();
            using var ms = new MemoryStream(data);
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch { return null; }
    }
}
