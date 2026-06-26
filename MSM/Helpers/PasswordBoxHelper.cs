using System.Windows;
using System.Windows.Controls;

namespace MSM.Helpers;

public static class PasswordBoxHelper
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxHelper),
            new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached("BindPassword", typeof(bool), typeof(PasswordBoxHelper),
            new PropertyMetadata(false, OnBindPasswordChanged));

    private static bool _isUpdating;

    public static string GetBoundPassword(DependencyObject d) => (string)d.GetValue(BoundPasswordProperty);
    public static void SetBoundPassword(DependencyObject d, string value) => d.SetValue(BoundPasswordProperty, value);

    public static bool GetBindPassword(DependencyObject d) => (bool)d.GetValue(BindPasswordProperty);
    public static void SetBindPassword(DependencyObject d, bool value) => d.SetValue(BindPasswordProperty, value);

    private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box) return;
        if ((bool)e.OldValue) box.PasswordChanged -= HandlePasswordChanged;
        if ((bool)e.NewValue) box.PasswordChanged += HandlePasswordChanged;
    }

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box || _isUpdating) return;
        box.Password = (string)e.NewValue;
    }

    private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox box) return;
        _isUpdating = true;
        SetBoundPassword(box, box.Password);
        _isUpdating = false;
    }
}
