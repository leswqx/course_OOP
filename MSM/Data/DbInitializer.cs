using Microsoft.EntityFrameworkCore;
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
        await SeedReviewsAsync(context);
        await SeedFavoritesAsync(context);
        await SeedPriceHistoryAsync(context);
    }

    // ──────────────────────────────────────────────────────────────
    // Пользователи
    // ──────────────────────────────────────────────────────────────
    private static async Task SeedUsersAsync(AppDbContext context)
    {
        if (context.Users.Any()) return;

        var now = DateTime.Now;

        var admin = new User
        {
            Login        = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Email        = "admin@belagency.by",
            Role         = "admin",
            FullName     = "Краснова Ирина Викторовна",
            Phone        = "+375 (17) 200-00-01",
            Description  = "Главный администратор агентства недвижимости «БелАгентство».",
            CreatedAt    = now.AddMonths(-18)
        };

        var realtor1 = new User
        {
            Login        = "realtor1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("realtor123"),
            Email        = "kuznetsov@belagency.by",
            Role         = "realtor",
            FullName     = "Кузнецов Алексей Игоревич",
            Phone        = "+375 (29) 645-12-33",
            Description  = "Специализируюсь на квартирах Минска. Опыт — 8 лет. " +
                           "Помогаю клиентам с оформлением ипотеки через Беларусбанк и АСБ. " +
                           "Более 120 успешных сделок за карьеру. Работаю без выходных.",
            CreatedAt    = now.AddMonths(-14)
        };

        var realtor2 = new User
        {
            Login        = "realtor2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("realtor123"),
            Email        = "likhacheva@belagency.by",
            Role         = "realtor",
            FullName     = "Лихачёва Наталья Сергеевна",
            Phone        = "+375 (44) 712-88-50",
            Description  = "Эксперт по загородной недвижимости: коттеджи, дома, таунхаусы " +
                           "в Минском районе (Дроздово, Боровляны, Колодищи, Масюковщина). " +
                           "10 лет в сфере недвижимости. Юридическое сопровождение сделок.",
            CreatedAt    = now.AddMonths(-10)
        };

        var realtor3 = new User
        {
            Login        = "realtor3",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("realtor123"),
            Email        = "borisevich@belagency.by",
            Role         = "realtor",
            FullName     = "Борисевич Дмитрий Андреевич",
            Phone        = "+375 (33) 560-40-17",
            Description  = "Работаю с недвижимостью в регионах Беларуси: Гомель, Брест, Гродно, " +
                           "Витебск, Могилёв. Знаю рынок региональных городов изнутри. " +
                           "Помогу оформить документы и провести сделку под ключ.",
            CreatedAt    = now.AddMonths(-6)
        };

        var client1 = new User
        {
            Login        = "client1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("client123"),
            Email        = "petrovichiv@mail.ru",
            Role         = "client",
            FullName     = "Петрович Иван Валентинович",
            Phone        = "+375 (29) 311-22-45",
            CreatedAt    = now.AddMonths(-8)
        };

        var client2 = new User
        {
            Login        = "client2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("client123"),
            Email        = "zaitseva.kate@gmail.com",
            Role         = "client",
            FullName     = "Зайцева Екатерина Михайловна",
            Phone        = "+375 (44) 890-34-56",
            CreatedAt    = now.AddMonths(-5)
        };

        var client3 = new User
        {
            Login        = "client3",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("client123"),
            Email        = "romanov.sn@yandex.by",
            Role         = "client",
            FullName     = "Романов Сергей Николаевич",
            Phone        = "+375 (33) 455-67-89",
            CreatedAt    = now.AddMonths(-3)
        };

        var client4 = new User
        {
            Login        = "client4",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("client123"),
            Email        = "shevchenko.anna@tut.by",
            Role         = "client",
            FullName     = "Шевченко Анна Петровна",
            Phone        = "+375 (25) 777-00-12",
            CreatedAt    = now.AddMonths(-2)
        };

        var client5 = new User
        {
            Login        = "client5",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("client123"),
            Email        = "klimovich.ad@mail.ru",
            Role         = "client",
            FullName     = "Климович Андрей Дмитриевич",
            Phone        = "+375 (29) 612-53-91",
            CreatedAt    = now.AddDays(-20)
        };

        context.Users.AddRange(admin, realtor1, realtor2, realtor3, client1, client2, client3, client4, client5);
        await context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Объекты недвижимости
    // ──────────────────────────────────────────────────────────────
    private static async Task SeedPropertiesAsync(AppDbContext context)
    {
        if (context.Properties.Any()) return;

        var realtors = await context.Users.Where(u => u.Role == "realtor").ToListAsync();
        if (realtors.Count < 3) return;

        var r1 = realtors.First(r => r.Login == "realtor1");
        var r2 = realtors.First(r => r.Login == "realtor2");
        var r3 = realtors.First(r => r.Login == "realtor3");

        var now = DateTime.Now;

        var properties = new List<Property>
        {
            // ── Риелтор 1 (Кузнецов — квартиры Минска) ─────────────────
            new()
            {
                Title       = "3-комнатная квартира на проспекте Независимости",
                Description = "Просторная квартира в кирпичном доме 2018 года постройки. Евроремонт, " +
                              "встроенная кухня, два санузла. 9-й этаж с панорамным видом на центр Минска. " +
                              "Развитая инфраструктура: рядом станция метро «Площадь Победы», парк Горького, " +
                              "ТЦ «Галерея», школы и детские сады.",
                PropertyType     = "apartment", Price = 185_000, Area = 82.5, Rooms = 3,
                Bathrooms        = 2, Floor = 9, TotalFloors = 16, YearBuilt = 2018,
                City             = "Минск", District = "Советский",
                Address          = "пр. Независимости, д. 168, кв. 91",
                HasRepair        = true, MortgageAvailable = true, Status = "active",
                RealtorId        = r1.Id, CreatedAt = now.AddDays(-22)
            },
            new()
            {
                Title       = "2-комнатная квартира в мкр. Малиновка",
                Description = "Уютная двушка в жилом микрорайоне Малиновка. Хорошая планировка, " +
                              "раздельные комнаты, большая лоджия 6 м². Свежий косметический ремонт, " +
                              "новая сантехника. В шаговой доступности школа №133, детский сад и магазины.",
                PropertyType     = "apartment", Price = 112_000, Area = 56.0, Rooms = 2,
                Bathrooms        = 1, Floor = 4, TotalFloors = 9, YearBuilt = 2004,
                City             = "Минск", District = "Московский",
                Address          = "ул. Лобанка, д. 74, кв. 22",
                HasRepair        = true, MortgageAvailable = true, Status = "active",
                RealtorId        = r1.Id, CreatedAt = now.AddDays(-14)
            },
            new()
            {
                Title       = "1-комнатная квартира у метро Пушкинская",
                Description = "Компактная квартира в 5 минутах пешком от метро «Пушкинская». " +
                              "Отличный вариант для молодой семьи или под сдачу в аренду. " +
                              "Качественный ремонт 2022 года: ламинат, новые окна, встроенный шкаф-купе.",
                PropertyType     = "apartment", Price = 73_500, Area = 35.2, Rooms = 1,
                Bathrooms        = 1, Floor = 7, TotalFloors = 10, YearBuilt = 2001,
                City             = "Минск", District = "Первомайский",
                Address          = "ул. Притыцкого, д. 46, кв. 138",
                HasRepair        = true, MortgageAvailable = true, Status = "active",
                RealtorId        = r1.Id, CreatedAt = now.AddDays(-9)
            },
            new()
            {
                Title       = "Студия в центре Минска, ул. Немига",
                Description = "Современная студия с авторским дизайном в историческом центре города. " +
                              "Высокие потолки 3 м, панорамное остекление. Всё новое: кухня, техника, " +
                              "мебель. Идеально для деловых людей — рядом бизнес-центры и рестораны.",
                PropertyType     = "apartment", Price = 61_000, Area = 27.8, Rooms = 1,
                Bathrooms        = 1, Floor = 3, TotalFloors = 7, YearBuilt = 2019,
                City             = "Минск", District = "Центральный",
                Address          = "ул. Немига, д. 5, кв. 12",
                HasRepair        = true, MortgageAvailable = false, Status = "sold",
                RealtorId        = r1.Id, CreatedAt = now.AddDays(-45), UpdatedAt = now.AddMonths(-4)
            },
            new()
            {
                Title       = "4-комнатная квартира бизнес-класса, пр. Победителей",
                Description = "Представительские апартаменты в новом доме на проспекте Победителей. " +
                              "Закрытая охраняемая территория, подземный паркинг, консьерж. Высококачественная " +
                              "отделка: мрамор, паркет дуб, немецкая кухня. Вид на Минск-Арену и парк.",
                PropertyType     = "complex", Price = 245_000, Area = 118.0, Rooms = 4,
                Bathrooms        = 2, Floor = 12, TotalFloors = 24, YearBuilt = 2022,
                City             = "Минск", District = "Фрунзенский",
                Address          = "пр. Победителей, д. 115, кв. 243",
                HasRepair        = true, MortgageAvailable = true, Status = "active",
                RealtorId        = r1.Id, CreatedAt = now.AddDays(-3)
            },
            new()
            {
                Title       = "2-комнатная квартира, ул. Заводская — без ремонта",
                Description = "Двухкомнатная квартира с хорошей планировкой. Раздельные комнаты, " +
                              "кухня 11 м², большие окна. Требует косметического ремонта — отличная " +
                              "возможность сделать всё под себя. Тихий двор, рядом остановка.",
                PropertyType     = "apartment", Price = 92_000, Area = 52.3, Rooms = 2,
                Bathrooms        = 1, Floor = 2, TotalFloors = 9, YearBuilt = 1996,
                City             = "Минск", District = "Заводской",
                Address          = "ул. Заводская, д. 18, кв. 7",
                HasRepair        = false, MortgageAvailable = true, Status = "hidden",
                RealtorId        = r1.Id, CreatedAt = now.AddDays(-30)
            },

            // ── Риелтор 2 (Лихачёва — загородная недвижимость) ─────────
            new()
            {
                Title       = "Коттедж в посёлке Дроздово, Минский район",
                Description = "Просторный кирпичный коттедж в элитном посёлке Дроздово. 15 км от МКАД. " +
                              "Участок 12 соток с ландшафтным дизайном, гараж на 2 авто, баня, беседка. " +
                              "Все коммуникации: газ, скважина, автономная канализация. " +
                              "Охрана периметра, видеонаблюдение. Идеально для большой семьи.",
                PropertyType     = "house", Price = 325_000, Area = 210.0, Rooms = 5,
                Bathrooms        = 3, Floor = 2, TotalFloors = 2, YearBuilt = 2017,
                City             = "Минский район", District = "Минский",
                Address          = "п. Дроздово, ул. Берёзовая, д. 14",
                HasRepair        = true, MortgageAvailable = false, Status = "active",
                RealtorId        = r2.Id, CreatedAt = now.AddDays(-18)
            },
            new()
            {
                Title       = "Дом в Колодищах с участком 10 соток",
                Description = "Добротный дом в Колодищах — 12 км от Минска. Подключены все городские " +
                              "коммуникации (централизованный газ, вода, канализация). Тёплый гараж, " +
                              "хозблок. Участок 10 соток. Пригородный транспорт — каждые 20 минут. " +
                              "Рядом лесной массив и пруд.",
                PropertyType     = "house", Price = 188_000, Area = 148.0, Rooms = 4,
                Bathrooms        = 2, Floor = 2, TotalFloors = 2, YearBuilt = 2009,
                City             = "Минский район", District = "Минский",
                Address          = "аг. Колодищи, ул. Лесная, д. 22",
                HasRepair        = true, MortgageAvailable = true, Status = "active",
                RealtorId        = r2.Id, CreatedAt = now.AddDays(-11)
            },
            new()
            {
                Title       = "Таунхаус в Боровлянах, 3 уровня",
                Description = "Современный таунхаус в закрытом коттеджном посёлке Боровляны. " +
                              "3 уровня, 5 комнат, 2 санузла. Терраса, небольшой приусадебный участок 3 сотки. " +
                              "До МКАД 8 км, прямой автобус до центра Минска. Ухоженная закрытая территория.",
                PropertyType     = "house", Price = 222_000, Area = 164.0, Rooms = 5,
                Bathrooms        = 2, Floor = 3, TotalFloors = 3, YearBuilt = 2020,
                City             = "Минский район", District = "Минский",
                Address          = "аг. Боровляны, ул. Солнечная, д. 8",
                HasRepair        = true, MortgageAvailable = true, Status = "active",
                RealtorId        = r2.Id, CreatedAt = now.AddDays(-6)
            },
            new()
            {
                Title       = "Дача с домом в СТ «Озёрный», Минская область",
                Description = "Дача на живописном участке 15 соток. Кирпичный дом 2 этажа, " +
                              "веранда, летняя кухня, колодец. Электричество, септик. " +
                              "Рядом озеро. Отличный вариант для отдыха или постоянного проживания летом.",
                PropertyType     = "house", Price = 65_000, Area = 72.0, Rooms = 3,
                Floor            = 2, TotalFloors = 2, YearBuilt = 2003,
                City             = "Минская область", District = "Минский",
                Address          = "СТ «Озёрный», участок 47",
                HasRepair        = false, MortgageAvailable = false, Status = "sold",
                RealtorId        = r2.Id, CreatedAt = now.AddDays(-50), UpdatedAt = now.AddMonths(-2)
            },
            new()
            {
                Title       = "Участок 20 соток с недостроенным домом, д. Масюковщина",
                Description = "Участок ИЖС в деревне Масюковщина, 6 км от МКАД (Логойский тракт). " +
                              "На участке залит фундамент и возведены стены 1-го этажа. " +
                              "Газ и электричество у границы. Тихое место, лес рядом. Документы готовы.",
                PropertyType     = "house", Price = 98_000, Area = 160.0, Rooms = 4,
                Floor            = 1, TotalFloors = 2, YearBuilt = 2021,
                City             = "Минский район", District = "Минский",
                Address          = "д. Масюковщина, ул. Центральная, д. 3А",
                HasRepair        = false, MortgageAvailable = false, Status = "active",
                RealtorId        = r2.Id, CreatedAt = now.AddDays(-25)
            },

            // ── Риелтор 3 (Борисевич — региональная недвижимость) ───────
            new()
            {
                Title       = "2-комнатная квартира на пр. Ленина, Гомель",
                Description = "Отличная квартира в центре Гомеля на проспекте Ленина. " +
                              "Сделан хороший ремонт 2021 года, встроенная кухня, паркет. " +
                              "Рядом парк им. Луначарского, цирк и торговые центры. " +
                              "Дом кирпичный 1987 г. п., высокие потолки 2.8 м.",
                PropertyType     = "apartment", Price = 68_500, Area = 54.0, Rooms = 2,
                Bathrooms        = 1, Floor = 5, TotalFloors = 9, YearBuilt = 1987,
                City             = "Гомель", District = "Центральный",
                Address          = "пр. Ленина, д. 38, кв. 71",
                HasRepair        = true, MortgageAvailable = true, Status = "active",
                RealtorId        = r3.Id, CreatedAt = now.AddDays(-15)
            },
            new()
            {
                Title       = "3-комнатная квартира в новостройке, Брест",
                Description = "Просторная трёшка в новом доме 2023 года в Бресте. " +
                              "Чистовая отделка застройщика, готова к заселению. Лоджия 5 м², " +
                              "гардеробная. Тёплый двор, паркинг. В 10 минутах — крепость-герой Брест. " +
                              "Ипотека от 6% в Беларусбанке.",
                PropertyType     = "apartment", Price = 87_000, Area = 76.5, Rooms = 3,
                Bathrooms        = 1, Floor = 8, TotalFloors = 14, YearBuilt = 2023,
                City             = "Брест", District = "Московский",
                Address          = "ул. Московская, д. 262, кв. 154",
                HasRepair        = true, MortgageAvailable = true, Status = "active",
                RealtorId        = r3.Id, CreatedAt = now.AddDays(-8)
            },
            new()
            {
                Title       = "1-комнатная квартира, Витебск, пр. Фрунзе",
                Description = "Светлая однокомнатная квартира на одной из центральных улиц Витебска. " +
                              "Ремонт 2020 года, новая сантехника и электрика. 3-й этаж 5-этажного " +
                              "кирпичного дома. Рядом рынок «Марковщина», поликлиника, остановки. " +
                              "Хорошая транспортная доступность.",
                PropertyType     = "apartment", Price = 44_000, Area = 32.0, Rooms = 1,
                Bathrooms        = 1, Floor = 3, TotalFloors = 5, YearBuilt = 1979,
                City             = "Витебск", District = "Железнодорожный",
                Address          = "пр. Фрунзе, д. 47, кв. 11",
                HasRepair        = true, MortgageAvailable = true, Status = "active",
                RealtorId        = r3.Id, CreatedAt = now.AddDays(-20)
            },
            new()
            {
                Title       = "Дом с участком 8 соток, Гродно",
                Description = "Добротный дом в черте Гродно, ул. Кирова. Участок 8 соток, гараж, погреб. " +
                              "Газовое отопление, централизованный водопровод и канализация. Свежий ремонт. " +
                              "До центра 15 минут на автобусе, рядом школа и детский сад.",
                PropertyType     = "house", Price = 122_000, Area = 112.0, Rooms = 4,
                Bathrooms        = 1, Floor = 2, TotalFloors = 2, YearBuilt = 2005,
                City             = "Гродно", District = "Ленинский",
                Address          = "ул. Кирова, д. 84А",
                HasRepair        = true, MortgageAvailable = false, Status = "active",
                RealtorId        = r3.Id, CreatedAt = now.AddDays(-12)
            },
            new()
            {
                Title       = "2-комнатная квартира, Могилёв, ул. Первомайская",
                Description = "Двушка в тихом районе Могилёва. Окна во двор, 5-й этаж. " +
                              "Ремонт делался в 2019 году. Встроенный шкаф, остеклённый балкон. " +
                              "Рядом парк «Подниколье», ТЦ «Атлант», транспортная остановка.",
                PropertyType     = "apartment", Price = 59_000, Area = 50.0, Rooms = 2,
                Bathrooms        = 1, Floor = 5, TotalFloors = 9, YearBuilt = 1992,
                City             = "Могилёв", District = "Октябрьский",
                Address          = "ул. Первомайская, д. 54, кв. 87",
                HasRepair        = true, MortgageAvailable = true, Status = "active",
                RealtorId        = r3.Id, CreatedAt = now.AddDays(-17)
            },
            new()
            {
                Title       = "3-комнатная квартира, ул. Сурганова, Минск — продана",
                Description = "Трёшка в зелёном районе Минска. Дом 1995 г. п., кирпичный. " +
                              "Качественный ремонт с элементами дизайна. Просторная кухня-столовая 17 м². " +
                              "Продана — снята с продажи.",
                PropertyType     = "apartment", Price = 155_000, Area = 72.0, Rooms = 3,
                Bathrooms        = 1, Floor = 6, TotalFloors = 10, YearBuilt = 1995,
                City             = "Минск", District = "Советский",
                Address          = "ул. Сурганова, д. 29, кв. 63",
                HasRepair        = true, MortgageAvailable = false, Status = "sold",
                RealtorId        = r3.Id, CreatedAt = now.AddDays(-60), UpdatedAt = now.AddMonths(-1)
            },
            new()
            {
                Title       = "1-комнатная квартира, Барановичи, ул. Стрелецкая",
                Description = "Недорогая однокомнатная квартира в Барановичах. Подойдёт для первой покупки " +
                              "или инвестиций. Ремонт советского периода, но ухоженная. Есть балкон. " +
                              "Рядом ж/д вокзал и все необходимые магазины.",
                PropertyType     = "apartment", Price = 38_500, Area = 31.0, Rooms = 1,
                Bathrooms        = 1, Floor = 2, TotalFloors = 5, YearBuilt = 1984,
                City             = "Барановичи", District = "Северный",
                Address          = "ул. Стрелецкая, д. 17, кв. 4",
                HasRepair        = false, MortgageAvailable = true, Status = "active",
                RealtorId        = r3.Id, CreatedAt = now.AddDays(-28)
            },
            new()
            {
                Title       = "Офисное помещение 85 м², пр. Победителей, Минск",
                Description = "Офисное помещение на 1-м этаже жилого дома на одной из ключевых магистралей " +
                              "Минска. Открытая планировка, 4 кабинета, переговорная, санузел. " +
                              "Отдельный вход с улицы, парковка перед зданием. " +
                              "Подходит для офиса, медицинского кабинета, салона красоты.",
                PropertyType     = "complex", Price = 195_000, Area = 85.0, Rooms = 4,
                Bathrooms        = 1, Floor = 1, TotalFloors = 12, YearBuilt = 2016,
                City             = "Минск", District = "Фрунзенский",
                Address          = "пр. Победителей, д. 88",
                HasRepair        = true, MortgageAvailable = false, Status = "active",
                RealtorId        = r3.Id, CreatedAt = now.AddDays(-5)
            }
        };

        context.Properties.AddRange(properties);
        await context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Записи на просмотр
    // ──────────────────────────────────────────────────────────────
    private static async Task SeedAppointmentsAsync(AppDbContext context)
    {
        if (context.Appointments.Any()) return;

        var realtors = await context.Users.Where(u => u.Role == "realtor").ToListAsync();
        var clients  = await context.Users.Where(u => u.Role == "client").ToListAsync();
        var props    = await context.Properties.ToListAsync();

        if (!realtors.Any() || !clients.Any() || !props.Any()) return;

        var r1 = realtors.First(r => r.Login == "realtor1");
        var r2 = realtors.First(r => r.Login == "realtor2");
        var r3 = realtors.First(r => r.Login == "realtor3");

        var c1 = clients.First(c => c.Login == "client1");
        var c2 = clients.First(c => c.Login == "client2");
        var c3 = clients.First(c => c.Login == "client3");
        var c4 = clients.First(c => c.Login == "client4");
        var c5 = clients.First(c => c.Login == "client5");

        Property Prop(string title) => props.First(p => p.Title.Contains(title));

        var now = DateTime.Now;

        // Вспомогательная функция: дата в N месяцев назад + смещение дней и часов
        DateTime Ago(int months, int days, int hour) =>
            new DateTime(now.AddMonths(-months).Year, now.AddMonths(-months).Month, 1)
                .AddDays(days - 1).AddHours(hour);

        var appointments = new List<Appointment>
        {
            // ── 5 месяцев назад ────────────────────────────────────────
            new()
            {
                ClientId = c1.Id, RealtorId = r1.Id, PropertyId = Prop("пр. Независимости").Id,
                SlotStart = Ago(5, 5, 10), SlotEnd = Ago(5, 5, 11),
                Status = "completed", Comment = "Хочу посмотреть квартиру — интересует планировка",
                CreatedAt = Ago(5, 3, 9)
            },
            new()
            {
                ClientId = c3.Id, RealtorId = r2.Id, PropertyId = Prop("Дроздово").Id,
                SlotStart = Ago(5, 10, 14), SlotEnd = Ago(5, 10, 15),
                Status = "completed", Comment = "Важны коммуникации и состояние участка",
                CreatedAt = Ago(5, 8, 10)
            },
            new()
            {
                ClientId = c4.Id, RealtorId = r3.Id, PropertyId = Prop("пр. Ленина, Гомель").Id,
                SlotStart = Ago(5, 15, 11), SlotEnd = Ago(5, 15, 12),
                Status = "completed", Comment = null,
                CreatedAt = Ago(5, 13, 9)
            },

            // ── 4 месяца назад ─────────────────────────────────────────
            new()
            {
                ClientId = c2.Id, RealtorId = r1.Id, PropertyId = Prop("Малиновка").Id,
                SlotStart = Ago(4, 7, 15), SlotEnd = Ago(4, 7, 16),
                Status = "completed", Comment = "Интересует состояние санузла и окон",
                CreatedAt = Ago(4, 5, 12)
            },
            new()
            {
                ClientId = c5.Id, RealtorId = r2.Id, PropertyId = Prop("Боровлянах").Id,
                SlotStart = Ago(4, 12, 10), SlotEnd = Ago(4, 12, 11),
                Status = "completed", Comment = "Рассматриваем для постоянного проживания",
                CreatedAt = Ago(4, 10, 9)
            },
            new()
            {
                ClientId = c1.Id, RealtorId = r3.Id, PropertyId = Prop("Брест").Id,
                SlotStart = Ago(4, 20, 14), SlotEnd = Ago(4, 20, 15),
                Status = "completed", Comment = "Интересует ипотека через Беларусбанк",
                CreatedAt = Ago(4, 18, 11)
            },
            new()
            {
                ClientId = c3.Id, RealtorId = r1.Id, PropertyId = Prop("метро Пушкинская").Id,
                SlotStart = Ago(4, 25, 11), SlotEnd = Ago(4, 25, 12),
                Status = "completed", Comment = "Вариант под аренду — смотрел состояние",
                CreatedAt = Ago(4, 23, 10)
            },

            // ── 3 месяца назад ─────────────────────────────────────────
            new()
            {
                ClientId = c2.Id, RealtorId = r2.Id, PropertyId = Prop("Колодищах").Id,
                SlotStart = Ago(3, 5, 10), SlotEnd = Ago(3, 5, 11),
                Status = "completed", Comment = "Нужно оценить состояние крыши и подвала",
                CreatedAt = Ago(3, 3, 9)
            },
            new()
            {
                ClientId = c4.Id, RealtorId = r1.Id, PropertyId = Prop("бизнес-класса, пр. Победителей").Id,
                SlotStart = Ago(3, 14, 16), SlotEnd = Ago(3, 14, 17),
                Status = "completed", Comment = "Приехал с дизайнером — оцениваем перепланировку",
                CreatedAt = Ago(3, 12, 14)
            },
            new()
            {
                ClientId = c5.Id, RealtorId = r3.Id, PropertyId = Prop("Гродно").Id,
                SlotStart = Ago(3, 18, 13), SlotEnd = Ago(3, 18, 14),
                Status = "completed", Comment = "Хочу посмотреть участок и гараж",
                CreatedAt = Ago(3, 16, 10)
            },
            new()
            {
                ClientId = c1.Id, RealtorId = r2.Id, PropertyId = Prop("Масюковщина").Id,
                SlotStart = Ago(3, 22, 11), SlotEnd = Ago(3, 22, 12),
                Status = "cancelled", Comment = "Изменились планы",
                CreatedAt = Ago(3, 20, 9)
            },

            // ── 2 месяца назад ─────────────────────────────────────────
            new()
            {
                ClientId = c3.Id, RealtorId = r3.Id, PropertyId = Prop("Витебск").Id,
                SlotStart = Ago(2, 6, 14), SlotEnd = Ago(2, 6, 15),
                Status = "completed", Comment = null,
                CreatedAt = Ago(2, 4, 10)
            },
            new()
            {
                ClientId = c4.Id, RealtorId = r2.Id, PropertyId = Prop("Дроздово").Id,
                SlotStart = Ago(2, 10, 10), SlotEnd = Ago(2, 10, 11),
                Status = "completed", Comment = "Второй осмотр с нотариусом",
                CreatedAt = Ago(2, 8, 9)
            },
            new()
            {
                ClientId = c2.Id, RealtorId = r1.Id, PropertyId = Prop("пр. Независимости").Id,
                SlotStart = Ago(2, 16, 15), SlotEnd = Ago(2, 16, 16),
                Status = "completed", Comment = "Сравниваю с другим вариантом",
                CreatedAt = Ago(2, 14, 12)
            },
            new()
            {
                ClientId = c5.Id, RealtorId = r1.Id, PropertyId = Prop("Малиновка").Id,
                SlotStart = Ago(2, 22, 11), SlotEnd = Ago(2, 22, 12),
                Status = "completed", Comment = "Рассматриваю для родителей",
                CreatedAt = Ago(2, 20, 9)
            },
            new()
            {
                ClientId = c1.Id, RealtorId = r3.Id, PropertyId = Prop("Могилёв").Id,
                SlotStart = Ago(2, 25, 13), SlotEnd = Ago(2, 25, 14),
                Status = "cancelled", Comment = "Слишком далеко от работы",
                CreatedAt = Ago(2, 23, 10)
            },

            // ── 1 месяц назад ──────────────────────────────────────────
            new()
            {
                ClientId = c3.Id, RealtorId = r1.Id, PropertyId = Prop("метро Пушкинская").Id,
                SlotStart = Ago(1, 8, 10), SlotEnd = Ago(1, 8, 11),
                Status = "completed", Comment = "Первичный осмотр — интересует высота потолков",
                CreatedAt = Ago(1, 6, 9)
            },
            new()
            {
                ClientId = c5.Id, RealtorId = r2.Id, PropertyId = Prop("Боровлянах").Id,
                SlotStart = Ago(1, 12, 14), SlotEnd = Ago(1, 12, 15),
                Status = "completed", Comment = "Готов к сделке — финальный осмотр",
                CreatedAt = Ago(1, 10, 11)
            },
            new()
            {
                ClientId = c2.Id, RealtorId = r3.Id, PropertyId = Prop("Барановичи").Id,
                SlotStart = Ago(1, 17, 11), SlotEnd = Ago(1, 17, 12),
                Status = "completed", Comment = "Инвестиционная покупка",
                CreatedAt = Ago(1, 15, 10)
            },
            new()
            {
                ClientId = c4.Id, RealtorId = r2.Id, PropertyId = Prop("Масюковщина").Id,
                SlotStart = Ago(1, 20, 16), SlotEnd = Ago(1, 20, 17),
                Status = "completed", Comment = "Проверяем фундамент с инженером",
                CreatedAt = Ago(1, 18, 14)
            },
            new()
            {
                ClientId = c1.Id, RealtorId = r1.Id, PropertyId = Prop("бизнес-класса, пр. Победителей").Id,
                SlotStart = Ago(1, 25, 10), SlotEnd = Ago(1, 25, 11),
                Status = "completed", Comment = "Удобно в обед. Приеду с супругой.",
                CreatedAt = Ago(1, 23, 9)
            },

            // ── Текущий месяц / ближайшее время ────────────────────────
            new()
            {
                ClientId = c2.Id, RealtorId = r1.Id, PropertyId = Prop("бизнес-класса, пр. Победителей").Id,
                SlotStart = now.AddDays(2).AddHours(13), SlotEnd = now.AddDays(2).AddHours(14),
                Status = "confirmed", Comment = "Удобно в обед. Приеду с супругой.",
                CreatedAt = now.AddDays(-3)
            },
            new()
            {
                ClientId = c5.Id, RealtorId = r2.Id, PropertyId = Prop("Боровлянах").Id,
                SlotStart = now.AddDays(4).AddHours(11), SlotEnd = now.AddDays(4).AddHours(12),
                Status = "confirmed", Comment = "Рассматриваем для постоянного проживания",
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                ClientId = c3.Id, RealtorId = r3.Id, PropertyId = Prop("Брест").Id,
                SlotStart = now.AddDays(6).AddHours(15), SlotEnd = now.AddDays(6).AddHours(16),
                Status = "confirmed", Comment = "Интересует ипотечное оформление через Беларусбанк",
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                ClientId = c4.Id, RealtorId = r1.Id, PropertyId = Prop("метро Пушкинская").Id,
                SlotStart = now.AddDays(7).AddHours(10), SlotEnd = now.AddDays(7).AddHours(11),
                Status = "new", Comment = "Первичный осмотр",
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                ClientId = c5.Id, RealtorId = r3.Id, PropertyId = Prop("Гродно").Id,
                SlotStart = now.AddDays(10).AddHours(14), SlotEnd = now.AddDays(10).AddHours(15),
                Status = "new", Comment = "Хочу посмотреть участок и гараж",
                CreatedAt = now
            },
            new()
            {
                ClientId = c2.Id, RealtorId = r3.Id, PropertyId = Prop("Витебск").Id,
                SlotStart = now.AddDays(-3).AddHours(12), SlotEnd = now.AddDays(-3).AddHours(13),
                Status = "cancelled", Comment = "Изменились планы, рассмотрю другие варианты",
                CreatedAt = now.AddDays(-6)
            }
        };

        context.Appointments.AddRange(appointments);
        await context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Отзывы
    // ──────────────────────────────────────────────────────────────
    private static async Task SeedReviewsAsync(AppDbContext context)
    {
        if (context.Reviews.Any()) return;

        var realtors = await context.Users.Where(u => u.Role == "realtor").ToListAsync();
        var clients  = await context.Users.Where(u => u.Role == "client").ToListAsync();
        if (!realtors.Any() || !clients.Any()) return;

        var r1 = realtors.First(r => r.Login == "realtor1");
        var r2 = realtors.First(r => r.Login == "realtor2");
        var r3 = realtors.First(r => r.Login == "realtor3");

        var c1 = clients.First(c => c.Login == "client1");
        var c2 = clients.First(c => c.Login == "client2");
        var c3 = clients.First(c => c.Login == "client3");
        var c4 = clients.First(c => c.Login == "client4");
        var c5 = clients.First(c => c.Login == "client5");

        var now = DateTime.Now;

        var reviews = new List<Review>
        {
            // ── Одобренные (видны в профиле риелтора) ──────────────────
            new()
            {
                UserId     = c1.Id, RealtorId = r1.Id, Rating = 5,
                Comment    = "Алексей — отличный специалист! Помог подобрать квартиру точно под наш бюджет. " +
                             "Провёл несколько показов, терпеливо отвечал на все вопросы. " +
                             "Сделка прошла быстро и без проблем. Рекомендую!",
                IsApproved = true, CreatedAt = now.AddDays(-18)
            },
            new()
            {
                UserId     = c2.Id, RealtorId = r1.Id, Rating = 4,
                Comment    = "Хороший риелтор, знает рынок Минска. Показал несколько вариантов, " +
                             "помог сравнить. Единственный минус — не всегда отвечал сразу. " +
                             "В целом доволен результатом.",
                IsApproved = true, CreatedAt = now.AddDays(-13)
            },
            new()
            {
                UserId     = c3.Id, RealtorId = r2.Id, Rating = 5,
                Comment    = "Наталья — профессионал высокого уровня. Нашли коттедж в Дроздово именно такой, " +
                             "как хотели. Она знает каждый посёлок в Минском районе. " +
                             "Помогла с проверкой юридической чистоты сделки. Огромное спасибо!",
                IsApproved = true, CreatedAt = now.AddDays(-8)
            },
            new()
            {
                UserId     = c4.Id, RealtorId = r3.Id, Rating = 4,
                Comment    = "Дмитрий помог купить квартиру в Гомеле на расстоянии — я был в Минске, " +
                             "он провёл видеопоказ и всё оформил. Всё прошло нормально, " +
                             "документы подготовил быстро.",
                IsApproved = true, CreatedAt = now.AddDays(-6)
            },
            new()
            {
                UserId     = c1.Id, RealtorId = r2.Id, Rating = 5,
                Comment    = "Уже второй раз обращаемся к Наталье. Теперь купили дом в Колодищах — " +
                             "всё как мы хотели: участок, гараж, лес рядом. " +
                             "Очень благодарны за терпение и профессионализм!",
                IsApproved = true, CreatedAt = now.AddDays(-3)
            },

            // ── Ожидают модерации (видны только администратору) ────────
            new()
            {
                UserId     = c5.Id, RealtorId = r1.Id, Rating = 3,
                Comment    = "Риелтор неплохой, но немного торопил с принятием решения. " +
                             "Чувствовалось давление — хотел закрыть сделку быстрее. " +
                             "Квартира в итоге подошла, но осадок остался.",
                IsApproved = false, CreatedAt = now.AddDays(-2)
            },
            new()
            {
                UserId     = c3.Id, RealtorId = r3.Id, Rating = 5,
                Comment    = "Борисевич — настоящий знаток регионального рынка. " +
                             "Подобрал квартиру в Бресте дистанционно, всё рассказал про район. " +
                             "Сделка прошла чисто. Рекомендую всем, кто ищет в регионах.",
                IsApproved = false, CreatedAt = now.AddDays(-1)
            },
            new()
            {
                UserId     = c2.Id, RealtorId = r2.Id, Rating = 2,
                Comment    = "К сожалению, дом оказался не таким как на фото. " +
                             "Риелтор не предупредила о проблемах с фундаментом. " +
                             "Пришлось отказаться от покупки после осмотра с экспертом.",
                IsApproved = false, CreatedAt = now
            }
        };

        context.Reviews.AddRange(reviews);
        await context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Избранное
    // ──────────────────────────────────────────────────────────────
    private static async Task SeedFavoritesAsync(AppDbContext context)
    {
        if (context.Favorites.Any()) return;

        var clients = await context.Users.Where(u => u.Role == "client").ToListAsync();
        var props   = await context.Properties.Where(p => p.Status == "active").ToListAsync();
        if (!clients.Any() || !props.Any()) return;

        var c1 = clients.First(c => c.Login == "client1");
        var c2 = clients.First(c => c.Login == "client2");
        var c3 = clients.First(c => c.Login == "client3");
        var c4 = clients.First(c => c.Login == "client4");
        var c5 = clients.First(c => c.Login == "client5");

        Property Prop(string title) => props.First(p => p.Title.Contains(title));

        var now = DateTime.Now;

        var favorites = new List<Favorite>
        {
            new() { UserId = c1.Id, PropertyId = Prop("бизнес-класса, пр. Победителей").Id, AddedAt = now.AddDays(-5) },
            new() { UserId = c1.Id, PropertyId = Prop("Боровлянах").Id,                       AddedAt = now.AddDays(-4) },
            new() { UserId = c2.Id, PropertyId = Prop("пр. Независимости").Id,                AddedAt = now.AddDays(-7) },
            new() { UserId = c2.Id, PropertyId = Prop("Дроздово").Id,                         AddedAt = now.AddDays(-6) },
            new() { UserId = c3.Id, PropertyId = Prop("Брест").Id,                            AddedAt = now.AddDays(-3) },
            new() { UserId = c3.Id, PropertyId = Prop("Гродно").Id,                           AddedAt = now.AddDays(-2) },
            new() { UserId = c4.Id, PropertyId = Prop("метро Пушкинская").Id,                 AddedAt = now.AddDays(-8) },
            new() { UserId = c5.Id, PropertyId = Prop("Могилёв").Id,                          AddedAt = now.AddDays(-1) },
        };

        context.Favorites.AddRange(favorites);
        await context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // История цен
    // ──────────────────────────────────────────────────────────────
    private static async Task SeedPriceHistoryAsync(AppDbContext context)
    {
        var seededIds = await context.PriceHistories
            .Select(ph => ph.PropertyId).Distinct().ToListAsync();

        var seededSet = seededIds.ToHashSet();
        var allProps  = await context.Properties.ToListAsync();
        var unSeeded  = allProps.Where(p => !seededSet.Contains(p.Id)).ToList();

        if (unSeeded.Count == 0) return;

        var now = DateTime.Now;

        // Объекты, для которых добавим несколько записей в истории цен
        var priceChanges = new Dictionary<string, (decimal[] Prices, int[] DaysAgo)>
        {
            ["пр. Независимости"] = (new[] { 195_000m, 190_000m, 185_000m }, new[] { 90, 45, 22 }),
            ["Малиновка"]         = (new[] { 118_000m, 115_000m, 112_000m }, new[] { 60, 30, 14 }),
            ["Дроздово"]          = (new[] { 340_000m, 330_000m, 325_000m }, new[] { 80, 40, 18 }),
            ["Боровлянах"]        = (new[] { 230_000m, 225_000m, 222_000m }, new[] { 55, 25, 6  }),
            ["пр. Ленина, Гомель"]= (new[] { 72_000m,  70_000m,  68_500m  }, new[] { 50, 25, 15 }),
        };

        foreach (var p in unSeeded)
        {
            var matchKey = priceChanges.Keys.FirstOrDefault(k => p.Title.Contains(k));
            if (matchKey != null)
            {
                var (prices, daysAgo) = priceChanges[matchKey];
                for (int i = 0; i < prices.Length; i++)
                {
                    context.PriceHistories.Add(new PriceHistory
                    {
                        PropertyId = p.Id,
                        Price      = prices[i],
                        ChangedAt  = now.AddDays(-daysAgo[i])
                    });
                }
            }
            else
            {
                context.PriceHistories.Add(new PriceHistory
                {
                    PropertyId = p.Id,
                    Price      = p.Price,
                    ChangedAt  = p.CreatedAt
                });
            }
        }
        await context.SaveChangesAsync();
    }
}
