using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MSM.Data.Context;
using MSM.Models.Entities;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

public class ReviewDisplayRow
{
    public string Author { get; }
    public string Stars { get; }
    public string FilledStars { get; }
    public string EmptyStars  { get; }
    public string? Comment { get; }
    public string Date { get; }
    public int RatingValue { get; }
    public System.DateTime CreatedAt { get; }

    public ReviewDisplayRow(MSM.Models.Entities.Review r)
    {
        Author      = r.User?.FullName ?? "—";
        RatingValue = r.Rating;
        Stars       = new string('★', r.Rating) + new string('☆', 5 - r.Rating);
        FilledStars = new string('★', r.Rating);
        EmptyStars  = new string('☆', 5 - r.Rating);
        Comment     = r.Comment;
        CreatedAt   = r.CreatedAt;
        Date        = r.CreatedAt.ToString("dd.MM.yyyy");
    }
}

public partial class RealtorProfileViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    private string? _errorMessage;
    public bool HasErrorMessage => ErrorMessage != null;

    [ObservableProperty] private string _realtorName = "";
    [ObservableProperty] private string _realtorPhone = "";
    [ObservableProperty] private string _realtorEmail = "";
    [ObservableProperty] private string _realtorDescription = "";
    [ObservableProperty] private BitmapImage? _avatarImage;
    [ObservableProperty] private bool _hasAvatar;

    [ObservableProperty] private int _statProperties;
    [ObservableProperty] private int _statSold;
    [ObservableProperty] private double _statRating;
    [ObservableProperty] private string _statRatingStars = "☆☆☆☆☆";
    [ObservableProperty] private string _statFilledStars = "";
    [ObservableProperty] private string _statEmptyStars  = "☆☆☆☆☆";

    [ObservableProperty] private ObservableCollection<ReviewDisplayRow> _reviews = new();
    [ObservableProperty] private bool _noReviews;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSortDateDesc), nameof(IsSortDateAsc), nameof(IsSortRatingDesc), nameof(IsSortRatingAsc))]
    private string _sortMode = "date_desc";
    public bool IsSortDateDesc   => SortMode == "date_desc";
    public bool IsSortDateAsc    => SortMode == "date_asc";
    public bool IsSortRatingDesc => SortMode == "rating_desc";
    public bool IsSortRatingAsc  => SortMode == "rating_asc";

    private readonly SemaphoreSlim _sem = new(1, 1);
    private const int ReviewPageSize = 5;
    private int _reviewPage;
    private int _totalReviewCount;
    private int _realtorId;

    [ObservableProperty] private bool _hasMoreReviews;

    public RealtorProfileViewModel(
        AppDbContext context,
        INavigationService navigationService)
    {
        _context = context;
        _navigationService = navigationService;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        _realtorId = parameter is int id ? id : 0;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await _sem.WaitAsync();
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var realtor = await _context.Users.FindAsync(_realtorId);
            if (realtor == null)
            {
                ErrorMessage = "Риелтор не найден.";
                return;
            }

            RealtorName        = realtor.FullName;
            RealtorPhone       = realtor.Phone ?? "—";
            RealtorEmail       = realtor.Email;
            RealtorDescription = string.IsNullOrWhiteSpace(realtor.Description)
                ? "Описание не заполнено."
                : realtor.Description;

            if (realtor.AvatarPhoto?.Length > 0)
            {
                using var ms = new System.IO.MemoryStream(realtor.AvatarPhoto);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                AvatarImage = bmp;
                HasAvatar = true;
            }
            else
            {
                HasAvatar = false;
            }

            StatProperties = await _context.Properties.CountAsync(p => p.RealtorId == _realtorId);
            StatSold       = await _context.Properties.CountAsync(p => p.RealtorId == _realtorId && p.Status == "sold");

            StatRating = await _context.Reviews
                .Where(r => r.RealtorId == _realtorId && r.IsApproved)
                .Select(r => (double?)r.Rating)
                .AverageAsync() ?? 0;
            var rounded = (int)Math.Round(StatRating);
            StatRatingStars = new string('★', rounded) + new string('☆', 5 - rounded);
            StatFilledStars = new string('★', rounded);
            StatEmptyStars  = new string('☆', 5 - rounded);

            await LoadReviewsPageAsync(false);
        }
        finally { IsLoading = false; _sem.Release(); }
    }

    private IQueryable<Review> GetSortedReviewQuery()
    {
        var q = _context.Reviews.Include(r => r.User)
            .Where(r => r.RealtorId == _realtorId && r.IsApproved);
        return SortMode switch
        {
            "date_asc"    => q.OrderBy(r => r.CreatedAt),
            "rating_desc" => q.OrderByDescending(r => r.Rating),
            "rating_asc"  => q.OrderBy(r => r.Rating),
            _             => q.OrderByDescending(r => r.CreatedAt)
        };
    }

    private async Task LoadReviewsPageAsync(bool append)
    {
        if (!append)
        {
            _totalReviewCount = await _context.Reviews.CountAsync(r => r.RealtorId == _realtorId && r.IsApproved);
            _reviewPage = 0;
            Reviews.Clear();
        }
        var items = await GetSortedReviewQuery()
            .Skip(_reviewPage * ReviewPageSize)
            .Take(ReviewPageSize)
            .ToListAsync();
        foreach (var r in items) Reviews.Add(new ReviewDisplayRow(r));
        _reviewPage++;
        HasMoreReviews = Reviews.Count < _totalReviewCount;
        NoReviews = _totalReviewCount == 0;
    }

    [RelayCommand]
    private async Task ShowMoreReviewsAsync()
    {
        await _sem.WaitAsync();
        try { await LoadReviewsPageAsync(true); }
        finally { _sem.Release(); }
    }

    [RelayCommand]
    private async Task SortDateDescAsync()
    {
        await _sem.WaitAsync();
        try { SortMode = "date_desc"; await LoadReviewsPageAsync(false); }
        finally { _sem.Release(); }
    }

    [RelayCommand]
    private async Task SortDateAscAsync()
    {
        await _sem.WaitAsync();
        try { SortMode = "date_asc"; await LoadReviewsPageAsync(false); }
        finally { _sem.Release(); }
    }

    [RelayCommand]
    private async Task SortRatingDescAsync()
    {
        await _sem.WaitAsync();
        try { SortMode = "rating_desc"; await LoadReviewsPageAsync(false); }
        finally { _sem.Release(); }
    }

    [RelayCommand]
    private async Task SortRatingAscAsync()
    {
        await _sem.WaitAsync();
        try { SortMode = "rating_asc"; await LoadReviewsPageAsync(false); }
        finally { _sem.Release(); }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.GoBack();
}
