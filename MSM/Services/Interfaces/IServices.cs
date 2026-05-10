using MSM.Models.Entities;

namespace MSM.Services.Interfaces;

/// <summary>
/// Сервис email-уведомлений
/// </summary>
public interface INotificationService
{
    /// <summary>Отправить письмо одному адресату</summary>
    Task SendEmailAsync(string toEmail, string toName, string subject, string body);

    /// <summary>Приветственное письмо при регистрации</summary>
    Task SendWelcomeEmailAsync(User user);

    /// <summary>Оповещение о пополнении каталога всем клиентам</summary>
    Task SendNewListingsNotificationAsync(IEnumerable<User> clients, int newCount);

    /// <summary>Ручная рассылка из админки</summary>
    Task SendBulkEmailAsync(IEnumerable<User> recipients, string subject, string body);

    /// <summary>Уведомление клиенту об изменении статуса записи</summary>
    Task SendAppointmentStatusChangedAsync(User client, string propertyTitle, string realtorName, string newStatus, DateTime slotStart);
}

/// <summary>
/// Интерфейс сервиса аутентификации
/// </summary>
public interface IAuthService
{
    Task<User?> LoginAsync(string login, string password);
    Task<User?> RegisterAsync(string login, string password, string email, string fullName, string? phone = null);
    Task<bool> ChangePasswordAsync(User user, string oldPassword, string newPassword);
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByLoginAsync(string login);
    Task UpdateProfileAsync(User user);
    Task<string?> GetLoginErrorAsync(string login, string password);
    Task SetBlockedAsync(int userId, bool isBlocked);
}

/// <summary>
/// Интерфейс сервиса недвижимости
/// </summary>
public interface IPropertyService
{
    Task<IEnumerable<Property>> GetAllAsync();
    Task<Property?> GetByIdAsync(int id);
    Task<IEnumerable<Property>> GetFilteredAsync(
        decimal? minPrice = null,
        decimal? maxPrice = null,
        double? minArea = null,
        double? maxArea = null,
        int? minRooms = null,
        int? maxRooms = null,
        int? minBathrooms = null,
        int? maxBathrooms = null,
        string? city = null,
        string? district = null,
        string? propertyType = null,
        string? searchQuery = null,
        bool? hasMortgage = null,
        bool? hasRenovation = null,
        string? sortBy = null);
    Task<IEnumerable<string>> GetDistinctCitiesAsync();
    Task<IEnumerable<string>> GetDistinctDistrictsAsync();
    Task<Property> AddAsync(Property property, IEnumerable<(byte[] Data, string FileName, bool IsMain)> images, string? district = null);
    Task UpdateAsync(Property property);
    Task DeleteAsync(int id);
    Task<IEnumerable<Property>> GetRealtorPropertiesAsync(int realtorId);
    Task<IEnumerable<Property>> GetActivePropertiesAsync();
    Task UpdateStatusAsync(int id, string status);
    Task UpdatePropertyDetailsAsync(int id, string title, string description, decimal price, double area, int rooms,
        int? bathrooms, int? floor, int? totalFloors, int? yearBuilt, string city, string? district, string address,
        string propertyType, bool hasRepair, bool mortgageAvailable);
    Task<IEnumerable<PriceHistory>> GetPriceHistoryAsync(int propertyId);
}

/// <summary>
/// Интерфейс сервиса записей
/// </summary>
public interface IAppointmentService
{
    Task<Appointment> CreateAsync(int propertyId, int clientId, int realtorId, DateTime slotStart, DateTime slotEnd, string? comment = null);
    Task<Appointment?> GetByIdAsync(int id);
    Task<IEnumerable<Appointment>> GetByClientIdAsync(int clientId);
    Task<IEnumerable<Appointment>> GetByRealtorIdAsync(int realtorId);
    Task UpdateStatusAsync(int id, string status, string? comment = null);
    Task<bool> IsSlotAvailableAsync(int realtorId, DateTime slotStart, DateTime slotEnd, int? excludeAppointmentId = null);
    Task<IEnumerable<RealtorSchedule>> GetBlockedSchedulesAsync(int realtorId);
}

/// <summary>
/// Интерфейс сервиса избранного
/// </summary>
public interface IFavoriteService
{
    Task<IEnumerable<Property>> GetUserFavoritesAsync(int userId);
    Task<bool> IsFavoriteAsync(int userId, int propertyId);
    Task AddToFavoritesAsync(int userId, int propertyId);
    Task RemoveFromFavoritesAsync(int userId, int propertyId);
}

/// <summary>
/// Интерфейс сервиса отзывов
/// </summary>
public interface IReviewService
{
    Task<Review> CreateAsync(int userId, int? propertyId, int? realtorId, int rating, string? comment = null);
    Task<IEnumerable<Review>> GetPropertyReviewsAsync(int propertyId);
    Task<IEnumerable<Review>> GetRealtorReviewsAsync(int realtorId);
    Task<IEnumerable<Review>> GetUserReviewsAsync(int userId);
    Task<bool> HasReviewAsync(int userId, int realtorId);
    Task ApproveAsync(int id);
    Task RejectAsync(int id);
    Task<double> GetAverageRatingAsync(int? propertyId = null, int? realtorId = null);
}
