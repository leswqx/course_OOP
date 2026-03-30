using MSM.Data.Context;
using MSM.Models.Entities;

namespace MSM.Data;

/// <summary>
/// Инициализация базы данных тестовыми данными
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // Если уже есть пользователи, ничего не делаем
        if (context.Users.Any())
            return;

        // Создаем администратора
        var admin = new User
        {
            Login = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Email = "admin@realestate.com",
            Role = "admin",
            FullName = "Администратор Системы",
            Phone = "+7 (999) 000-00-00",
            CreatedAt = DateTime.Now
        };

        // Создаем риелтора
        var realtor = new User
        {
            Login = "realtor",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("realtor123"),
            Email = "realtor@realestate.com",
            Role = "realtor",
            FullName = "Иванов Иван Иванович",
            Phone = "+7 (999) 111-22-33",
            CreatedAt = DateTime.Now
        };

        // Создаем клиента
        var client = new User
        {
            Login = "client",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("client123"),
            Email = "client@example.com",
            Role = "client",
            FullName = "Петров Петр Петрович",
            Phone = "+7 (999) 444-55-66",
            CreatedAt = DateTime.Now
        };

        context.Users.AddRange(admin, realtor, client);
        await context.SaveChangesAsync();
    }
}
