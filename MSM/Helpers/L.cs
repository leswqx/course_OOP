using System.Windows;

namespace MSM.Helpers;

public static class L
{
    public static string Get(string key, string fallback = "")
    {
        if (Application.Current?.TryFindResource(key) is string s)
            return s;
        return string.IsNullOrEmpty(fallback) ? key : fallback;
    }
}
