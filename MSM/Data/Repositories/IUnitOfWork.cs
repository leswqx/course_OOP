using MSM.Models.Entities;

namespace MSM.Data.Repositories;

/// <summary>
/// Интерфейс Unit of Work
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Property> Properties { get; }
    IRepository<PropertyImage> PropertyImages { get; }
    IRepository<Appointment> Appointments { get; }
    IRepository<RealtorSchedule> RealtorSchedules { get; }
    IRepository<Favorite> Favorites { get; }
    IRepository<Review> Reviews { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
