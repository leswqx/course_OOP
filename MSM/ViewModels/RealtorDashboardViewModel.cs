using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using MSM.Data.Context;
using MSM.Models;
using MSM.Models.Entities;
using MSM.Services.Interfaces;
using SkiaSharp;

namespace MSM.ViewModels;

public class RealtorAppointmentRow
{
    public int Id { get; }
    public string ClientName { get; }
    public string PropertyTitle { get; }
    public string DateTime { get; }
    public string Status { get; }
    public string StatusColor { get; }
    public bool CanConfirm { get; }
    public bool CanCancel { get; }
    public bool CanComplete { get; }

    public RealtorAppointmentRow(Appointment a)
    {
        Id = a.Id;
        ClientName = a.Client?.FullName ?? "—";
        PropertyTitle = a.Property?.Title ?? "—";
        DateTime = $"{a.SlotStart:dd.MM.yyyy  HH:mm}–{a.SlotEnd:HH:mm}";
        CanConfirm  = a.Status == "new";
        CanCancel   = a.Status is "new" or "confirmed";
        CanComplete = a.Status == "confirmed";
        (Status, StatusColor) = a.Status switch
        {
            "new"       => ("Новая",        "#7A7A7A"),
            "confirmed" => ("Подтверждена", "#7CB342"),
            "cancelled" => ("Отменена",     "#EF5350"),
            "completed" => ("Завершена ✓",  "#D4A5A5"),
            _           => (a.Status,       "#7A7A7A")
        };
    }
}

public class ImagePreviewVm
{
    public string FileName { get; init; } = "";
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public bool IsMain { get; set; }
}

// Панель риелтора: мои объекты · записи клиентов · статистика
public partial class RealtorDashboardViewModel : ViewModelBase
{
    private readonly IPropertyService _propertyService;
    private readonly IAppointmentService _appointmentService;
    private readonly IReviewService _reviewService;
    private readonly INavigationService _navigationService;
    private readonly AppDbContext _context;

    private readonly List<(byte[] Data, string FileName, bool IsMain)> _pendingImages = new();

    // ===== Вкладки =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTab0), nameof(ShowTab1), nameof(ShowTab2), nameof(ShowTab3))]
    private int _selectedTab;
    public bool ShowTab0 => SelectedTab == 0;
    public bool ShowTab1 => SelectedTab == 1;
    public bool ShowTab2 => SelectedTab == 2;
    public bool ShowTab3 => SelectedTab == 3;

    // ===== Профиль риелтора =====
    [ObservableProperty] private string _realtorName = "";
    [ObservableProperty] private string _realtorPhone = "";
    [ObservableProperty] private string _realtorEmail = "";
    [ObservableProperty] private string _realtorDescription = "";

    // ===== Мои объекты =====
    [ObservableProperty] private ObservableCollection<PropertyCardViewModel> _myProperties = new();
    [ObservableProperty] private bool _isPropsLoading;

    // ===== Форма добавления объекта =====
    [ObservableProperty] private bool _isFormVisible;
    [ObservableProperty] private string _formTitle = "";
    [ObservableProperty] private string _formDescription = "";
    [ObservableProperty] private string _formPrice = "";
    [ObservableProperty] private string _formArea = "";
    [ObservableProperty] private string _formRooms = "";
    [ObservableProperty] private string _formFloor = "";
    [ObservableProperty] private string _formTotalFloors = "";
    [ObservableProperty] private string _formYearBuilt = "";
    [ObservableProperty] private string _formCity = "";
    [ObservableProperty] private string _formAddress = "";
    [ObservableProperty] private string _formType = "apartment";
    [ObservableProperty] private bool _formHasRepair;
    [ObservableProperty] private bool _formMortgage;
    [ObservableProperty] private string? _formError;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private ObservableCollection<ImagePreviewVm> _formImages = new();

    // ===== Форма редактирования объекта =====
    [ObservableProperty] private bool _isEditFormVisible;
    [ObservableProperty] private int _editPropertyId;
    [ObservableProperty] private string _editTitle = "";
    [ObservableProperty] private string _editDescription = "";
    [ObservableProperty] private string _editPrice = "";
    [ObservableProperty] private string _editArea = "";
    [ObservableProperty] private string _editRooms = "";
    [ObservableProperty] private string _editFloor = "";
    [ObservableProperty] private string _editTotalFloors = "";
    [ObservableProperty] private string _editYearBuilt = "";
    [ObservableProperty] private string _editCity = "";
    [ObservableProperty] private string _editAddress = "";
    [ObservableProperty] private string _editType = "apartment";
    [ObservableProperty] private bool _editHasRepair;
    [ObservableProperty] private bool _editMortgage;
    [ObservableProperty] private string? _editError;
    [ObservableProperty] private bool _isEditSaving;

    // ===== Записи клиентов =====
    [ObservableProperty] private ObservableCollection<RealtorAppointmentRow> _appointments = new();
    [ObservableProperty] private bool _isApptLoading;
    [ObservableProperty] private bool _noAppointments;

    // ===== Статистика =====
    [ObservableProperty] private int _statTotal;
    [ObservableProperty] private int _statActive;
    [ObservableProperty] private int _statSold;
    [ObservableProperty] private double _statRating;
    [ObservableProperty] private int _statCompletedAppt;
    [ObservableProperty] private ISeries[] _statusSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] _ratingSeriesData = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _ratingXAxes = Array.Empty<Axis>();

    public RealtorDashboardViewModel(
        IPropertyService propertyService,
        IAppointmentService appointmentService,
        IReviewService reviewService,
        INavigationService navigationService,
        AppDbContext context)
    {
        _propertyService = propertyService;
        _appointmentService = appointmentService;
        _reviewService = reviewService;
        _navigationService = navigationService;
        _context = context;
    }

    public override async void OnNavigatedTo(object? parameter)
    {
        await Task.Yield();
        LoadProfile();
        LoadProfileTab();
        await LoadPropertiesAsync();
        await LoadAppointmentsAsync();
        await LoadStatsAsync();
    }

    private void LoadProfile()
    {
        var u = Session.CurrentUser;
        if (u == null) return;
        RealtorName        = u.FullName;
        RealtorPhone       = u.Phone ?? "—";
        RealtorEmail       = u.Email;
        RealtorDescription = string.IsNullOrWhiteSpace(u.Description) ? "Описание не заполнено." : u.Description;
    }

    // ===== Вкладки =====
    [RelayCommand] private void SetTab0() => SelectedTab = 0;
    [RelayCommand] private void SetTab1() => SelectedTab = 1;
    [RelayCommand] private void SetTab2() => SelectedTab = 2;
    [RelayCommand] private void SetTab3() => SelectedTab = 3;

    // ===== Объекты =====
    private async Task LoadPropertiesAsync()
    {
        if (Session.CurrentUser == null) return;
        IsPropsLoading = true;
        try
        {
            var items = await _propertyService.GetRealtorPropertiesAsync(Session.CurrentUser.Id);
            MyProperties.Clear();
            foreach (var p in items) MyProperties.Add(new PropertyCardViewModel(p));
        }
        finally { IsPropsLoading = false; }
    }

    [RelayCommand]
    private void OpenDetail(PropertyCardViewModel card) =>
        _navigationService.NavigateTo<PropertyDetailViewModel>(card.Id);

    [RelayCommand]
    private async Task DeletePropertyAsync(PropertyCardViewModel card)
    {
        await _propertyService.DeleteAsync(card.Id);
        await LoadPropertiesAsync();
        await LoadStatsAsync();
    }

    [RelayCommand]
    private async Task OpenEditFormAsync(PropertyCardViewModel card)
    {
        var property = await _propertyService.GetByIdAsync(card.Id);
        if (property == null) return;

        EditPropertyId   = property.Id;
        EditTitle        = property.Title;
        EditDescription  = property.Description;
        EditPrice        = property.Price.ToString(CultureInfo.InvariantCulture);
        EditArea         = property.Area.ToString(CultureInfo.InvariantCulture);
        EditRooms        = property.Rooms.ToString();
        EditFloor        = property.Floor?.ToString() ?? "";
        EditTotalFloors  = property.TotalFloors?.ToString() ?? "";
        EditYearBuilt    = property.YearBuilt?.ToString() ?? "";
        EditCity         = property.City;
        EditAddress      = property.Address;
        EditType         = property.PropertyType;
        EditHasRepair    = property.HasRepair;
        EditMortgage     = property.MortgageAvailable;
        EditError        = null;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    private void CloseEditForm() => IsEditFormVisible = false;

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        EditError = null;
        if (string.IsNullOrWhiteSpace(EditTitle) || string.IsNullOrWhiteSpace(EditDescription)
            || string.IsNullOrWhiteSpace(EditPrice) || string.IsNullOrWhiteSpace(EditArea)
            || string.IsNullOrWhiteSpace(EditRooms) || string.IsNullOrWhiteSpace(EditCity)
            || string.IsNullOrWhiteSpace(EditAddress))
        { EditError = "Заполните все обязательные поля."; return; }

        var priceStr = EditPrice.Replace(',', '.').Replace(" ", "");
        if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
        { EditError = "Укажите корректную цену."; return; }

        var areaStr = EditArea.Replace(',', '.').Replace(" ", "");
        if (!double.TryParse(areaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var area) || area <= 0)
        { EditError = "Укажите корректную площадь."; return; }

        if (!int.TryParse(EditRooms.Trim(), out var rooms) || rooms <= 0)
        { EditError = "Укажите количество комнат."; return; }

        IsEditSaving = true;
        try
        {
            await _propertyService.UpdatePropertyDetailsAsync(
                EditPropertyId,
                EditTitle.Trim(), EditDescription.Trim(), price, area, rooms,
                int.TryParse(EditFloor.Trim(), out var f) ? f : null,
                int.TryParse(EditTotalFloors.Trim(), out var tf) ? tf : null,
                int.TryParse(EditYearBuilt.Trim(), out var yb) ? yb : null,
                EditCity.Trim(), EditAddress.Trim(),
                EditType, EditHasRepair, EditMortgage);

            IsEditFormVisible = false;
            await LoadPropertiesAsync();
            await LoadStatsAsync();
        }
        catch (Exception ex) { EditError = $"Ошибка: {ex.Message}"; }
        finally { IsEditSaving = false; }
    }

    [RelayCommand]
    private async Task SetSoldAsync(PropertyCardViewModel card)
    {
        await _propertyService.UpdateStatusAsync(card.Id, "sold");
        await LoadPropertiesAsync();
        await LoadStatsAsync();
    }

    [RelayCommand]
    private async Task ToggleHiddenAsync(PropertyCardViewModel card)
    {
        var newStatus = card.Status == "hidden" ? "active" : "hidden";
        await _propertyService.UpdateStatusAsync(card.Id, newStatus);
        await LoadPropertiesAsync();
        await LoadStatsAsync();
    }

    [RelayCommand]
    private void ToggleForm()
    {
        IsFormVisible = !IsFormVisible;
        FormError = null;
    }

    // Загрузить изображение через диалог
    [RelayCommand]
    private void AddImage()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Выберите изображение",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;

        foreach (var file in dlg.FileNames)
        {
            try
            {
                var bytes = File.ReadAllBytes(file);
                var name = Path.GetFileName(file);
                bool isMain = _pendingImages.Count == 0;
                _pendingImages.Add((Data: bytes, FileName: name, IsMain: isMain));
                FormImages.Add(new ImagePreviewVm { FileName = name, Data = bytes, IsMain = isMain });
            }
            catch { /* пропустить повреждённый файл */ }
        }
    }

    [RelayCommand]
    private void RemoveImage(ImagePreviewVm img)
    {
        FormImages.Remove(img);
        _pendingImages.RemoveAll(x => x.FileName == img.FileName);
        // пересчитать IsMain
        if (_pendingImages.Count > 0 && !_pendingImages.Any(x => x.IsMain))
        {
            var first = _pendingImages[0];
            _pendingImages[0] = (first.Data, first.FileName, true);
            if (FormImages.Count > 0) FormImages[0].IsMain = true;
        }
    }

    [RelayCommand]
    private async Task SavePropertyAsync()
    {
        FormError = null;
        if (string.IsNullOrWhiteSpace(FormTitle) || string.IsNullOrWhiteSpace(FormDescription)
            || string.IsNullOrWhiteSpace(FormPrice) || string.IsNullOrWhiteSpace(FormArea)
            || string.IsNullOrWhiteSpace(FormRooms) || string.IsNullOrWhiteSpace(FormCity)
            || string.IsNullOrWhiteSpace(FormAddress))
        { FormError = "Заполните все обязательные поля."; return; }

        // принимаем и запятую, и точку как разделитель дробной части
        var priceStr = FormPrice.Replace(',', '.').Replace(" ", "");
        if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
        { FormError = "Укажите корректную цену (например: 4500000)."; return; }

        var areaStr = FormArea.Replace(',', '.').Replace(" ", "");
        if (!double.TryParse(areaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var area) || area <= 0)
        { FormError = "Укажите корректную площадь (например: 78.5 или 78,5)."; return; }

        if (!int.TryParse(FormRooms.Trim(), out var rooms) || rooms <= 0)
        { FormError = "Укажите количество комнат (целое число)."; return; }

        IsSaving = true;
        try
        {
            var property = new Property
            {
                Title = FormTitle.Trim(), Description = FormDescription.Trim(),
                Price = price, Area = area, Rooms = rooms,
                Floor       = int.TryParse(FormFloor.Trim(),       out var f)  ? f  : null,
                TotalFloors = int.TryParse(FormTotalFloors.Trim(),  out var tf) ? tf : null,
                YearBuilt   = int.TryParse(FormYearBuilt.Trim(),   out var yb) ? yb : null,
                City = FormCity.Trim(), Address = FormAddress.Trim(),
                PropertyType = FormType, HasRepair = FormHasRepair,
                MortgageAvailable = FormMortgage,
                RealtorId = Session.CurrentUser!.Id,
            };
            await _propertyService.AddAsync(property, _pendingImages);
            IsFormVisible = false;
            ClearForm();
            await LoadPropertiesAsync();
            await LoadStatsAsync();
        }
        catch (Exception ex) { FormError = $"Ошибка: {ex.Message}"; }
        finally { IsSaving = false; }
    }

    // ===== Записи =====
    private async Task LoadAppointmentsAsync()
    {
        if (Session.CurrentUser == null) return;
        IsApptLoading = true;
        try
        {
            var items = await _appointmentService.GetByRealtorIdAsync(Session.CurrentUser.Id);
            Appointments.Clear();
            foreach (var a in items) Appointments.Add(new RealtorAppointmentRow(a));
            NoAppointments = Appointments.Count == 0;
        }
        finally { IsApptLoading = false; }
    }

    [RelayCommand]
    private async Task ConfirmAppointmentAsync(int id)
    {
        await _appointmentService.UpdateStatusAsync(id, "confirmed");
        await LoadAppointmentsAsync();
    }

    [RelayCommand]
    private async Task CancelAppointmentAsync(int id)
    {
        await _appointmentService.UpdateStatusAsync(id, "cancelled");
        await LoadAppointmentsAsync();
    }

    [RelayCommand]
    private async Task CompleteAppointmentAsync(int id)
    {
        await _appointmentService.UpdateStatusAsync(id, "completed");
        await LoadAppointmentsAsync();
    }

    // ===== Статистика =====
    private async Task LoadStatsAsync()
    {
        if (Session.CurrentUser == null) return;
        try
        {
            var props = await _context.Properties
                .Where(p => p.RealtorId == Session.CurrentUser.Id)
                .ToListAsync();

            StatTotal  = props.Count;
            StatActive = props.Count(p => p.Status == "active");
            StatSold   = props.Count(p => p.Status == "sold");

            var appts = await _context.Appointments
                .Where(a => a.RealtorId == Session.CurrentUser.Id)
                .ToListAsync();
            StatCompletedAppt = appts.Count(a => a.Status == "completed");

            StatRating = await _reviewService.GetAverageRatingAsync(realtorId: Session.CurrentUser.Id);

            StatusSeries = new ISeries[]
            {
                new PieSeries<int> { Values = new[]{ StatActive }, Name = "Активные",
                    Fill = new SolidColorPaint(SKColor.Parse("#D4A5A5")) },
                new PieSeries<int> { Values = new[]{ StatSold },   Name = "Проданные",
                    Fill = new SolidColorPaint(SKColor.Parse("#7CB342")) },
                new PieSeries<int> { Values = new[]{ props.Count(p=>p.Status=="hidden") }, Name = "Скрытые",
                    Fill = new SolidColorPaint(SKColor.Parse("#AAAAAA")) },
            };

            var months = Enumerable.Range(0, 6)
                .Select(i => System.DateTime.Today.AddMonths(-5 + i))
                .ToList();
            var counts = months.Select(m =>
                appts.Count(a => a.SlotStart.Year == m.Year && a.SlotStart.Month == m.Month)).ToArray();

            RatingSeriesData = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = counts, Name = "Записи",
                    Fill = new SolidColorPaint(SKColor.Parse("#D4A5A5"))
                }
            };
            RatingXAxes = new Axis[]
            {
                new Axis { Labels = months.Select(m => m.ToString("MMM")).ToArray() }
            };
        }
        catch { /* статистика некритична */ }
    }

    // ===== Профиль =====
    [ObservableProperty] private string _profileFullName = "";
    [ObservableProperty] private string _profileEmail = "";
    [ObservableProperty] private string _profilePhone = "";
    [ObservableProperty] private string _profileDescription = "";
    [ObservableProperty] private byte[]? _profileAvatar;
    [ObservableProperty] private string _profileOldPassword = "";
    [ObservableProperty] private string _profileNewPassword = "";
    [ObservableProperty] private string _profileConfirmPassword = "";
    [ObservableProperty] private string? _profileResult;
    [ObservableProperty] private bool _profileSuccess;
    [ObservableProperty] private string? _passwordResult;
    [ObservableProperty] private bool _passwordSuccess;
    [ObservableProperty] private bool _isProfileSaving;

    public string ProfileLogin => Session.CurrentUser?.Login ?? "";

    private void LoadProfileTab()
    {
        var u = Session.CurrentUser;
        if (u == null) return;
        ProfileFullName     = u.FullName;
        ProfileEmail        = u.Email;
        ProfilePhone        = u.Phone ?? "";
        ProfileDescription  = u.Description ?? "";
        ProfileAvatar       = u.AvatarPhoto;
        ProfileResult       = null;
        PasswordResult      = null;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        var user = Session.CurrentUser;
        if (user == null) return;
        if (string.IsNullOrWhiteSpace(ProfileFullName))
        { ProfileResult = "Имя не может быть пустым."; ProfileSuccess = false; return; }

        IsProfileSaving = true;
        ProfileResult = null;
        try
        {
            var authService = MSM.App.ServiceProvider.GetRequiredService<IAuthService>();
            user.FullName    = ProfileFullName.Trim();
            user.Email       = ProfileEmail.Trim();
            user.Phone       = string.IsNullOrWhiteSpace(ProfilePhone) ? null : ProfilePhone.Trim();
            user.Description = string.IsNullOrWhiteSpace(ProfileDescription) ? null : ProfileDescription.Trim();
            user.AvatarPhoto = ProfileAvatar;
            await authService.UpdateProfileAsync(user);
            // обновить шапку профиля
            RealtorName        = user.FullName;
            RealtorEmail       = user.Email;
            RealtorPhone       = user.Phone ?? "—";
            RealtorDescription = user.Description ?? "Описание не заполнено.";
            ProfileResult  = "Профиль сохранён!";
            ProfileSuccess = true;
        }
        catch (Exception ex) { ProfileResult = $"Ошибка: {ex.Message}"; ProfileSuccess = false; }
        finally { IsProfileSaving = false; }
    }

    [RelayCommand]
    private async Task ChangeProfilePasswordAsync()
    {
        var user = Session.CurrentUser;
        if (user == null) return;
        if (string.IsNullOrWhiteSpace(ProfileOldPassword) || string.IsNullOrWhiteSpace(ProfileNewPassword))
        { PasswordResult = "Заполните все поля."; PasswordSuccess = false; return; }
        if (ProfileNewPassword != ProfileConfirmPassword)
        { PasswordResult = "Пароли не совпадают."; PasswordSuccess = false; return; }
        if (ProfileNewPassword.Length < 6)
        { PasswordResult = "Минимум 6 символов."; PasswordSuccess = false; return; }

        IsProfileSaving = true;
        PasswordResult = null;
        try
        {
            var authService = MSM.App.ServiceProvider.GetRequiredService<IAuthService>();
            var ok = await authService.ChangePasswordAsync(user, ProfileOldPassword, ProfileNewPassword);
            if (ok) { PasswordResult = "Пароль изменён!"; PasswordSuccess = true; ProfileOldPassword = ProfileNewPassword = ProfileConfirmPassword = ""; }
            else    { PasswordResult = "Неверный текущий пароль."; PasswordSuccess = false; }
        }
        catch (Exception ex) { PasswordResult = $"Ошибка: {ex.Message}"; PasswordSuccess = false; }
        finally { IsProfileSaving = false; }
    }

    [RelayCommand]
    private void UploadProfileAvatar()
    {
        var dlg = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp", Title = "Фото профиля" };
        if (dlg.ShowDialog() == true) ProfileAvatar = System.IO.File.ReadAllBytes(dlg.FileName);
    }

    [RelayCommand]
    private void RemoveProfileAvatar() => ProfileAvatar = null;

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();

    private void ClearForm()
    {
        FormTitle = FormDescription = FormPrice = FormArea = FormRooms = "";
        FormFloor = FormTotalFloors = FormYearBuilt = FormCity = FormAddress = "";
        FormType = "apartment"; FormHasRepair = FormMortgage = false;
        FormImages.Clear();
        _pendingImages.Clear();
    }
}
