using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

// Строка пользователя в таблице
public class UserRowViewModel
{
    public int Id { get; }
    public string FullName { get; }
    public string Login { get; }
    public string Email { get; }
    public string Role { get; }
    public string Phone { get; }
    public string CreatedAt { get; }

    public UserRowViewModel(User u)
    {
        Id = u.Id;
        FullName = u.FullName;
        Login = u.Login;
        Email = u.Email;
        Phone = u.Phone ?? "—";
        Role = u.Role switch { "admin" => "Администратор", "realtor" => "Риелтор", _ => "Клиент" };
        CreatedAt = u.CreatedAt.ToString("dd.MM.yyyy");
    }
}

// Строка отзыва на модерации
public class ReviewRowViewModel
{
    public int Id { get; }
    public string Author { get; }
    public string Rating { get; }
    public string? Comment { get; }
    public string Date { get; }

    public ReviewRowViewModel(Review r)
    {
        Id = r.Id;
        Author = r.User?.FullName ?? "—";
        Rating = new string('★', r.Rating) + new string('☆', 5 - r.Rating);
        Comment = r.Comment;
        Date = r.CreatedAt.ToString("dd.MM.yyyy");
    }
}

// Панель администратора: пользователи, модерация отзывов, статистика (LiveCharts)
public partial class AdminDashboardViewModel : ViewModelBase
{
    private readonly IReviewService _reviewService;
    private readonly INavigationService _navigationService;
    private readonly AppDbContext _context;

    [ObservableProperty] private int _selectedTab; // 0=users 1=reviews 2=stats

    // --- Пользователи ---
    [ObservableProperty] private ObservableCollection<UserRowViewModel> _users = new();
    [ObservableProperty] private bool _isUsersLoading;

    // --- Отзывы ---
    [ObservableProperty] private ObservableCollection<ReviewRowViewModel> _pendingReviews = new();
    [ObservableProperty] private bool _isReviewsLoading;
    [ObservableProperty] private bool _noReviews;

    // --- Статистика ---
    [ObservableProperty] private int _statTotalProps;
    [ObservableProperty] private int _statActiveProps;
    [ObservableProperty] private int _statSoldProps;
    [ObservableProperty] private int _statTotalUsers;
    [ObservableProperty] private ISeries[] _propStatusSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] _realtorSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _realtorXAxes = Array.Empty<Axis>();

    public AdminDashboardViewModel(
        IReviewService reviewService,
        INavigationService navigationService,
        AppDbContext context)
    {
        _reviewService = reviewService;
        _navigationService = navigationService;
        _context = context;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        _ = LoadUsersAsync();
        _ = LoadReviewsAsync();
        _ = LoadStatsAsync();
    }

    private async Task LoadUsersAsync()
    {
        IsUsersLoading = true;
        try
        {
            var users = await _context.Users.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();
            Users.Clear();
            foreach (var u in users) Users.Add(new UserRowViewModel(u));
        }
        finally { IsUsersLoading = false; }
    }

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

    private async Task LoadStatsAsync()
    {
        var props   = await _context.Properties.ToListAsync();
        var users   = await _context.Users.ToListAsync();
        var realtors = await _context.Users.Where(u => u.Role == "realtor").ToListAsync();

        StatTotalProps  = props.Count;
        StatActiveProps = props.Count(p => p.Status == "active");
        StatSoldProps   = props.Count(p => p.Status == "sold");
        StatTotalUsers  = users.Count;

        // Pie: активные / проданные / скрытые
        PropStatusSeries = new ISeries[]
        {
            new PieSeries<int> { Values = new[]{ StatActiveProps }, Name = "Активные",
                Fill = new SolidColorPaint(SKColor.Parse("#D4A5A5")) },
            new PieSeries<int> { Values = new[]{ StatSoldProps },   Name = "Проданные",
                Fill = new SolidColorPaint(SKColor.Parse("#7CB342")) },
            new PieSeries<int> { Values = new[]{ props.Count(p=>p.Status=="hidden") }, Name = "Скрытые",
                Fill = new SolidColorPaint(SKColor.Parse("#AAAAAA")) },
        };

        // Bar: объекты по риелторам
        var realtorCounts = realtors.Select(r => new
        {
            Name = r.FullName.Split(' ').First(),
            Count = props.Count(p => p.RealtorId == r.Id)
        }).ToList();

        RealtorSeries = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Values = realtorCounts.Select(x => x.Count).ToArray(),
                Name = "Объектов",
                Fill = new SolidColorPaint(SKColor.Parse("#D4A5A5"))
            }
        };
        RealtorXAxes = new Axis[]
        {
            new Axis { Labels = realtorCounts.Select(x => x.Name).ToArray() }
        };
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

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();
}
