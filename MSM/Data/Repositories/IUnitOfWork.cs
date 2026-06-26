using MSM.Models.Entities;

namespace MSM.Data.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Property> Properties { get; }
    IRepository<PropertyImage> PropertyImages { get; }
    IRepository<Appointment> Appointments { get; }
    IRepository<RealtorSchedule> RealtorSchedules { get; }
    IRepository<Favorite> Favorites { get; }
    IRepository<Review> Reviews { get; }
    IRepository<PriceHistory> PriceHistories { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel);
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
