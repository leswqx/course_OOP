using MSM.Data.Context;
using MSM.Models.Entities;

namespace MSM.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        await SeedUsersAsync(context);
        await SeedPropertiesAsync(context);
        await SeedAppointmentsAsync(context);
    }

    private static async Task SeedUsersAsync(AppDbContext context)
    {
        if (context.Users.Any()) return;

        var admin = new User
        {
            Login = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Email = "admin@realestate.com",
            Role = "admin",
            FullName = "Администратор Системы",
            Phone = "+7 (999) 000-00-00",
            Description = "Системный администратор агентства недвижимости.",
            CreatedAt = DateTime.Now
        };

        var realtor = new User
        {
            Login = "realtor",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("realtor123"),
            Email = "realtor@realestate.com",
            Role = "realtor",
            FullName = "Иванов Иван Иванович",
            Phone = "+7 (999) 111-22-33",
            Description = "Опытный риелтор с 10-летним стажем работы. Специализируюсь на жилой недвижимости Москвы и Подмосковья. Помогу найти идеальный вариант под любой бюджет.",
            CreatedAt = DateTime.Now
        };

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

    private static async Task SeedPropertiesAsync(AppDbContext context)
    {
        if (context.Properties.Any()) return;

        var realtor = context.Users.FirstOrDefault(u => u.Role == "realtor");
        if (realtor == null) return;

        var properties = new List<Property>
        {
            new()
            {
                Title = "Уютная 3-комнатная квартира в центре",
                Description = "Светлая просторная квартира с современным ремонтом в самом сердце города. " +
                              "Высокие потолки, панорамные окна, встроенная кухня. " +
                              "Рядом метро, школы, парк и торговые центры.",
                PropertyType = "apartment", Price = 8_500_000, Area = 78.5, Rooms = 3,
                Floor = 5, TotalFloors = 10, YearBuilt = 2015,
                City = "Москва", Address = "ул. Тверская, д. 12, кв. 45",
                HasRepair = true, MortgageAvailable = true, Status = "active",
                RealtorId = realtor.Id, CreatedAt = DateTime.Now.AddDays(-12)
            },
            new()
            {
                Title = "Загородный дом с участком",
                Description = "Добротный кирпичный дом в тихом месте. Участок 12 соток, гараж, баня. " +
                              "Все коммуникации подключены. Хорошая транспортная доступность — 20 минут до МКАД.",
                PropertyType = "house", Price = 14_200_000, Area = 145.0, Rooms = 5,
                Floor = 2, TotalFloors = 2, YearBuilt = 2010,
                City = "Подмосковье", Address = "п. Зеленоградский, ул. Садовая, д. 7",
                HasRepair = true, MortgageAvailable = false, Status = "active",
                RealtorId = realtor.Id, CreatedAt = DateTime.Now.AddDays(-8)
            },
            new()
            {
                Title = "Студия рядом с метро",
                Description = "Компактная и функциональная студия с качественным ремонтом. " +
                              "Идеально для молодых специалистов или как инвестиция под аренду. До метро 5 минут пешком.",
                PropertyType = "apartment", Price = 4_350_000, Area = 28.0, Rooms = 1,
                Floor = 8, TotalFloors = 17, YearBuilt = 2020,
                City = "Москва", Address = "ул. Профсоюзная, д. 88, кв. 312",
                HasRepair = true, MortgageAvailable = true, Status = "active",
                RealtorId = realtor.Id, CreatedAt = DateTime.Now.AddDays(-5)
            },
            new()
            {
                Title = "2-комнатная квартира без ремонта",
                Description = "Хорошая планировка, раздельные комнаты, большая кухня 12 кв.м. " +
                              "Требует косметического ремонта — отличный вариант для тех, кто хочет всё сделать под себя.",
                PropertyType = "apartment", Price = 6_100_000, Area = 54.3, Rooms = 2,
                Floor = 3, TotalFloors = 9, YearBuilt = 1998,
                City = "Санкт-Петербург", Address = "пр. Невский, д. 120, кв. 18",
                HasRepair = false, MortgageAvailable = true, Status = "sold",
                RealtorId = realtor.Id, CreatedAt = DateTime.Now.AddDays(-30)
            },
            new()
            {
                Title = "Апартаменты в жилом комплексе бизнес-класса",
                Description = "Готовые апартаменты в новом ЖК с закрытой территорией, подземным паркингом " +
                              "и консьерж-сервисом. Чистовая отделка, готово к заселению.",
                PropertyType = "complex", Price = 22_000_000, Area = 110.0, Rooms = 4,
                Floor = 15, TotalFloors = 25, YearBuilt = 2023,
                City = "Москва", Address = "Пресненская наб., д. 6, кв. 1502",
                HasRepair = true, MortgageAvailable = true, Status = "active",
                RealtorId = realtor.Id, CreatedAt = DateTime.Now.AddDays(-1)
            },
            new()
            {
                Title = "Дом в Сочи у моря",
                Description = "Двухэтажный дом в 400 метрах от пляжа. Терраса с видом на море, " +
                              "бассейн, ухоженный сад. Круглогодичное проживание или сдача в аренду туристам.",
                PropertyType = "house", Price = 31_500_000, Area = 180.0, Rooms = 6,
                Floor = 2, TotalFloors = 2, YearBuilt = 2018,
                City = "Сочи", Address = "ул. Морская, д. 3",
                HasRepair = true, MortgageAvailable = false, Status = "active",
                RealtorId = realtor.Id, CreatedAt = DateTime.Now
            }
        };

        context.Properties.AddRange(properties);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAppointmentsAsync(AppDbContext context)
    {
        if (context.Appointments.Any()) return;

        var realtor = context.Users.FirstOrDefault(u => u.Role == "realtor");
        var client  = context.Users.FirstOrDefault(u => u.Role == "client");
        var props   = context.Properties.Take(3).ToList();
        if (realtor == null || client == null || props.Count == 0) return;

        var appointments = new List<Appointment>
        {
            // завершённая — клиент может оставить отзыв
            new()
            {
                ClientId = client.Id, RealtorId = realtor.Id, PropertyId = props[0].Id,
                SlotStart = DateTime.Now.AddDays(-14).AddHours(10),
                SlotEnd   = DateTime.Now.AddDays(-14).AddHours(11),
                Status = "completed", Comment = "Хочу посмотреть квартиру подробнее"
            },
            // ещё одна завершённая
            new()
            {
                ClientId = client.Id, RealtorId = realtor.Id, PropertyId = props[1].Id,
                SlotStart = DateTime.Now.AddDays(-7).AddHours(14),
                SlotEnd   = DateTime.Now.AddDays(-7).AddHours(15),
                Status = "completed", Comment = null
            },
            // подтверждённая — будущая
            new()
            {
                ClientId = client.Id, RealtorId = realtor.Id, PropertyId = props[2].Id,
                SlotStart = DateTime.Now.AddDays(3).AddHours(12),
                SlotEnd   = DateTime.Now.AddDays(3).AddHours(13),
                Status = "confirmed", Comment = "Удобно в обед"
            },
        };

        context.Appointments.AddRange(appointments);
        await context.SaveChangesAsync();
    }
}
