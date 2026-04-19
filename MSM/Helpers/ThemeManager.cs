using System.Windows;

namespace MSM.Helpers;

// Переключает тему оформления, заменяя ResourceDictionary в MergedDictionaries
public static class ThemeManager
{
    private static bool _isDark;
    public static bool IsDark => _isDark;

    private const string LightUri = "Resources/Themes/LightTheme.xaml";
    private const string DarkUri  = "Resources/Themes/DarkTheme.xaml";

    public static void Toggle()
    {
        _isDark = !_isDark;
        Apply(_isDark);
    }

    public static void Apply(bool dark)
    {
        _isDark = dark;
        SwapDictionary(dark ? DarkUri : LightUri, "Theme");
    }

    private static void SwapDictionary(string uri, string marker)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var old = dicts.FirstOrDefault(d => d.Source?.OriginalString.Contains(marker) == true);
        if (old != null) dicts.Remove(old);
        dicts.Add(new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) });
    }
}
