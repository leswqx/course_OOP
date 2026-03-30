using MSM.Models.Entities;

namespace MSM.Services.Interfaces;

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
        int? rooms = null,
        string? city = null,
        string? propertyType = null,
        string? searchQuery = null);
    Task<Property> AddAsync(Property property, IEnumerable<(byte[] Data, string FileName, bool IsMain)> images);
    Task UpdateAsync(Property property);
    Task DeleteAsync(int id);
    Task<IEnumerable<Property>> GetRealtorPropertiesAsync(int realtorId);
    Task<IEnumerable<Property>> GetActivePropertiesAsync();
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
    Task ApproveAsync(int id);
    Task RejectAsync(int id);
    Task<double> GetAverageRatingAsync(int? propertyId = null, int? realtorId = null);
}
