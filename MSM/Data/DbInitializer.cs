using Microsoft.EntityFrameworkCore;
using MSM.Data.Context;
using MSM.Models.Entities;

namespace MSM.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        if (!await context.Users.AnyAsync(u => u.Role == "admin"))
        {
            context.Users.Add(new User
            {
                Login        = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Email        = "admin@agency.by",
                Role         = "admin",
                FullName     = "Администратор",
                Phone        = null,
                CreatedAt    = DateTime.Now
            });
            await context.SaveChangesAsync();
        }
    }
}
