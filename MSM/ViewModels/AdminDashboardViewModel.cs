using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using MSM.Data.Context;
using MSM.Models;
using MSM.Models.Entities;
using MSM.Services.Interfaces;
using SkiaSharp;

namespace MSM.ViewModels;

public class UserRowViewModel
{
    public int Id { get; }
    public string FullName { get; }
    public string Login { get; }
    public string Email { get; }
    public string RoleDisplay { get; }
    public string Role { get; }
    public string Phone { get; }
    public string CreatedAt { get; }
    public bool IsBlocked { get; }
    public bool CanPromote => Role == "client" && !IsBlocked;
    public bool CanDemote  => Role == "realtor" && !IsBlocked;
    public string BlockLabel => IsBlocked ? "🔓 Разблокировать" : "🔒 Заблокировать";
    public string BlockedBadge => IsBlocked ? "ЗАБЛОКИРОВАН" : "";
    public bool ShowBlockedBadge => IsBlocked;

    public UserRowViewModel(User u)
    {
        Id = u.Id; FullName = u.FullName; Login = u.Login; Email = u.Email;
        Role = u.Role; Phone = u.Phone ?? "—";
        IsBlocked = u.IsBlocked;
        RoleDisplay = u.Role switch { "admin" => "Администратор", "realtor" => "Риелтор", _ => "Клиент" };
        CreatedAt = u.CreatedAt.ToString("dd.MM.yyyy");
    }
}

public class ReviewRowViewModel
{
    public int Id { get; }
    public string Author { get; }
    public string Rating { get; }
    public string? Comment { get; }
    public string Date { get; }

    public ReviewRowViewModel(Review r)
    {
        Id = r.Id; Author = r.User?.FullName ?? "—";
        Rating = new string('★', r.Rating) + new string('☆', 5 - r.Rating);
        Comment = r.Comment; Date = r.CreatedAt.ToString("dd.MM.yyyy");
    }
}

// Панель администратора: пользователи · модерация · статистика
public partial class AdminDashboardViewModel : ViewModelBase
{
    private readonly IReviewService _reviewService;
    private readonly INavigationService _navigationService;
    private readonly AppDbContext _context;

    // ===== Вкладки =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTab0), nameof(ShowTab1), nameof(ShowTab2), nameof(ShowTab3), nameof(ShowTab4))]
    private int _selectedTab;
    public bool ShowTab0 => SelectedTab == 0;
    public bool ShowTab1 => SelectedTab == 1;
    public bool ShowTab2 => SelectedTab == 2;
    public bool ShowTab3 => SelectedTab == 3;
    public bool ShowTab4 => SelectedTab == 4;

    // ===== Профиль =====
    [ObservableProperty] private string _profileFullName = "";
    [ObservableProperty] private string _profileEmail = "";
    [ObservableProperty] private string _profilePhone = "";
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

    // ===== Пользователи =====
    [ObservableProperty] private ObservableCollection<UserRowViewModel> _users = new();
    [ObservableProperty] private bool _isUsersLoading;
    [ObservableProperty] private string? _userActionMessage;
    [ObservableProperty] private string _userSearch = "";
    [ObservableProperty] private string _userRoleFilter = "all"; // all, realtor, client, blocked

    private List<UserRowViewModel> _allUsers = new();

    // ===== Отзывы =====
    [ObservableProperty] private ObservableCollection<ReviewRowViewModel> _pendingReviews = new();
    [ObservableProperty] private bool _isReviewsLoading;
    [ObservableProperty] private bool _noReviews;

    // ===== Статистика =====
    [ObservableProperty] private int _statTotalProps;
    [ObservableProperty] private int _statActiveProps;
    [ObservableProperty] private int _statSoldProps;
    [ObservableProperty] private int _statTotalUsers;
    [ObservableProperty] private int _statRealtors;
    [ObservableProperty] private ISeries[] _propStatusSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] _realtorPropSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _realtorXAxes = Array.Empty<Axis>();
    [ObservableProperty] private ISeries[] _realtorRatingSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _ratingXAxes = Array.Empty<Axis>();

    private readonly INotificationService _notificationService;

    // ===== Рассылка =====
    [ObservableProperty] private string _mailSubject = "";
    [ObservableProperty] private string _mailBody = "";
    [ObservableProperty] private string _mailRecipients = "clients"; // all, clients, realtors
    [ObservableProperty] private string? _mailResult;
    [ObservableProperty] private bool _mailSuccess;
    [ObservableProperty] private bool _isSendingMail;

    public AdminDashboardViewModel(
        IReviewService reviewService,
        INavigationService navigationService,
        INotificationService notificationService,
        AppDbContext context)
    {
        _reviewService = reviewService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _context = context;
    }

    public override async void OnNavigatedTo(object? parameter)
    {
        await Task.Yield();
        LoadProfileTab();
        await LoadUsersAsync();
        await LoadReviewsAsync();
        await LoadStatsAsync();
    }

    // ===== Вкладки =====
    [RelayCommand] private void SetTab0() => SelectedTab = 0;
    [RelayCommand] private void SetTab1() => SelectedTab = 1;
    [RelayCommand] private void SetTab2() => SelectedTab = 2;
    [RelayCommand] private void SetTab3() => SelectedTab = 3;
    [RelayCommand] private void SetTab4() => SelectedTab = 4;

    // ===== Пользователи =====
    private async Task LoadUsersAsync()
    {
        IsUsersLoading = true;
        try
        {
            var users = await _context.Users
                .OrderBy(u => u.IsBlocked).ThenBy(u => u.Role).ThenBy(u => u.FullName)
                .ToListAsync();
            _allUsers = users.Select(u => new UserRowViewModel(u)).ToList();
            ApplyUserFilter();
        }
        finally { IsUsersLoading = false; }
    }

    private void ApplyUserFilter()
    {
        var filtered = _allUsers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(UserSearch))
        {
            var q = UserSearch.Trim().ToLower();
            filtered = filtered.Where(u =>
                u.FullName.ToLower().Contains(q) ||
                u.Login.ToLower().Contains(q) ||
                u.Email.ToLower().Contains(q));
        }

        filtered = UserRoleFilter switch
        {
            "realtor"  => filtered.Where(u => u.Role == "realtor"),
            "client"   => filtered.Where(u => u.Role == "client"),
            "blocked"  => filtered.Where(u => u.IsBlocked),
            _          => filtered
        };

        Users.Clear();
        foreach (var u in filtered) Users.Add(u);
    }

    [RelayCommand]
    private void SearchUsers() => ApplyUserFilter();

    [RelayCommand]
    private void FilterAll()     { UserRoleFilter = "all";     ApplyUserFilter(); }
    [RelayCommand]
    private void FilterRealtor() { UserRoleFilter = "realtor"; ApplyUserFilter(); }
    [RelayCommand]
    private void FilterClient()  { UserRoleFilter = "client";  ApplyUserFilter(); }
    [RelayCommand]
    private void FilterBlocked() { UserRoleFilter = "blocked"; ApplyUserFilter(); }

    [RelayCommand]
    private async Task PromoteToRealtorAsync(int userId) => await ChangeRoleAsync(userId, "realtor");

    [RelayCommand]
    private async Task DemoteToClientAsync(int userId) => await ChangeRoleAsync(userId, "client");

    private async Task ChangeRoleAsync(int userId, string newRole)
    {
        UserActionMessage = null;
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;
        user.Role = newRole;
        await _context.SaveChangesAsync();
        UserActionMessage = $"Роль {user.FullName} изменена на «{(newRole == "realtor" ? "Риелтор" : "Клиент")}»";
        await LoadUsersAsync();
        await LoadStatsAsync();
    }

    [RelayCommand]
    private async Task ToggleBlockAsync(int userId)
    {
        UserActionMessage = null;
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;
        user.IsBlocked = !user.IsBlocked;
        await _context.SaveChangesAsync();
        UserActionMessage = user.IsBlocked
            ? $"Пользователь {user.FullName} заблокирован."
            : $"Пользователь {user.FullName} разблокирован.";
        await LoadUsersAsync();
    }

    // ===== Отзывы =====
    private async Task LoadReviewsAsync()
    {
        IsReviewsLoading = true;
        try
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            PendingReviews.Clear();
            foreach (var r in reviews) PendingReviews.Add(new ReviewRowViewModel(r));
            NoReviews = PendingReviews.Count == 0;
        }
        finally { IsReviewsLoading = false; }
    }

    [RelayCommand]
    private async Task ApproveReviewAsync(int id)
    {
        await _reviewService.ApproveAsync(id);
        await LoadReviewsAsync();
    }

    [RelayCommand]
    private async Task RejectReviewAsync(int id)
    {
        await _reviewService.RejectAsync(id);
        await LoadReviewsAsync();
    }

    // ===== Статистика =====
    private async Task LoadStatsAsync()
    {
        try
        {
            var props   = await _context.Properties.ToListAsync();
            var users   = await _context.Users.ToListAsync();
            var realtors = users.Where(u => u.Role == "realtor").ToList();

            StatTotalProps  = props.Count;
            StatActiveProps = props.Count(p => p.Status == "active");
            StatSoldProps   = props.Count(p => p.Status == "sold");
            StatTotalUsers  = users.Count;
            StatRealtors    = realtors.Count;

            // Pie: статусы объектов
            PropStatusSeries = new ISeries[]
            {
                new PieSeries<int> { Values = new[]{ StatActiveProps }, Name = "Активные",
                    Fill = new SolidColorPaint(SKColor.Parse("#D4A5A5")) },
                new PieSeries<int> { Values = new[]{ StatSoldProps },   Name = "Проданные",
                    Fill = new SolidColorPaint(SKColor.Parse("#7CB342")) },
                new PieSeries<int> { Values = new[]{ props.Count(p=>p.Status=="hidden") }, Name = "Скрытые",
                    Fill = new SolidColorPaint(SKColor.Parse("#AAAAAA")) },
            };

            // Bar: объектов по риелторам
            var realtorData = realtors.Select(r => new
            {
                Name  = r.FullName.Split(' ').FirstOrDefault() ?? r.Login,
                Count = props.Count(p => p.RealtorId == r.Id)
            }).ToList();

            RealtorPropSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = realtorData.Select(x => x.Count).ToArray(),
                    Name = "Объектов",
                    Fill = new SolidColorPaint(SKColor.Parse("#D4A5A5"))
                }
            };
            RealtorXAxes = new Axis[]
            {
                new Axis { Labels = realtorData.Select(x => x.Name).ToArray() }
            };

            // Bar: средний рейтинг по риелторами (последовательно — один DbContext)
            var ratings = new List<(string Name, double Rating)>();
            foreach (var r in realtors)
            {
                var avg = await _reviewService.GetAverageRatingAsync(realtorId: r.Id);
                ratings.Add((r.FullName.Split(' ').FirstOrDefault() ?? r.Login, avg));
            }

            RealtorRatingSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = ratings.Select(x => Math.Round(x.Rating, 1)).ToArray(),
                    Name = "Рейтинг",
                    Fill = new SolidColorPaint(SKColor.Parse("#C49595"))
                }
            };
            RatingXAxes = new Axis[]
            {
                new Axis { Labels = ratings.Select(x => x.Name).ToArray(), MinStep = 1 }
            };
        }
        catch { /* некритично */ }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();

    // ===== Рассылка =====
    [RelayCommand] private void MailToAll()      { MailRecipients = "all";      }
    [RelayCommand] private void MailToClients()  { MailRecipients = "clients";  }
    [RelayCommand] private void MailToRealtors() { MailRecipients = "realtors"; }

    [RelayCommand]
    private async Task SendMailAsync()
    {
        if (string.IsNullOrWhiteSpace(MailSubject) || string.IsNullOrWhiteSpace(MailBody))
        { MailResult = "Заполните тему и текст письма."; MailSuccess = false; return; }

        IsSendingMail = true;
        MailResult = null;
        try
        {
            var query = _context.Users.Where(u => !u.IsBlocked && u.Email != null);
            query = MailRecipients switch
            {
                "clients"  => query.Where(u => u.Role == "client"),
                "realtors" => query.Where(u => u.Role == "realtor"),
                _          => query
            };
            var recipients = await query.ToListAsync();

            if (!recipients.Any())
            { MailResult = "Нет получателей."; MailSuccess = false; return; }

            await _notificationService.SendBulkEmailAsync(recipients, MailSubject, MailBody);
            MailResult  = $"Письмо отправлено {recipients.Count} получателям.";
            MailSuccess = true;
            MailSubject = MailBody = "";
        }
        catch (Exception ex) { MailResult = $"Ошибка отправки: {ex.Message}"; MailSuccess = false; }
        finally { IsSendingMail = false; }
    }

    // ===== Профиль =====
    private void LoadProfileTab()
    {
        var u = Session.CurrentUser;
        if (u == null) return;
        ProfileFullName    = u.FullName;
        ProfileEmail       = u.Email;
        ProfilePhone       = u.Phone ?? "";
        ProfileAvatar      = u.AvatarPhoto;
        ProfileResult      = null;
        PasswordResult     = null;
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
            user.AvatarPhoto = ProfileAvatar;
            await authService.UpdateProfileAsync(user);
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
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp" };
        if (dlg.ShowDialog() == true) ProfileAvatar = System.IO.File.ReadAllBytes(dlg.FileName);
    }

    [RelayCommand]
    private void RemoveProfileAvatar() => ProfileAvatar = null;
}
