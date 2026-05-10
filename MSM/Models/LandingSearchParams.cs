namespace MSM.Models;

public record LandingSearchParams(
    string? PropertyType,
    string? MaxPrice,
    string? MinRooms,
    string? MaxRooms);
