using System.Windows;

namespace MSM.Helpers;

// Переключает язык интерфейса, заменяя языковой ResourceDictionary
public static class LanguageManager
{
    private static bool _isEnglish;
    public static bool IsEnglish => _isEnglish;

    private const string RuUri = "Resources/Languages/lang.ru.xaml";
    private const string EnUri = "Resources/Languages/lang.en.xaml";

    public static void Toggle()
    {
        _isEnglish = !_isEnglish;
        SwapDictionary(_isEnglish ? EnUri : RuUri);
    }

    private static void SwapDictionary(string uri)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var old = dicts.FirstOrDefault(d => d.Source?.OriginalString.Contains("lang.") == true);
        if (old != null) dicts.Remove(old);
        dicts.Add(new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) });
    }
}
