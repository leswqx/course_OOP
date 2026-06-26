using MSM.Models.Entities;

namespace MSM.Models;

public static class Session
{
    public static User? CurrentUser { get; set; }

    public static bool IsLoggedIn => CurrentUser != null;
    public static bool IsAdmin => CurrentUser?.Role == "admin";
    public static bool IsRealtor => CurrentUser?.Role == "realtor";
    public static bool IsClient => CurrentUser?.Role == "client";

    public static void Logout()
    {
        CurrentUser = null;
    }
}
