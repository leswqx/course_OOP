using BCrypt.Net;
Console.WriteLine("admin123: " + BCrypt.HashPassword("admin123"));
Console.WriteLine("realtor123: " + BCrypt.HashPassword("realtor123"));
Console.WriteLine("client123: " + BCrypt.HashPassword("client123"));
