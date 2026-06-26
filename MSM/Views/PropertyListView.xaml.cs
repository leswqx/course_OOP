using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MSM.Views;

public partial class PropertyListView : UserControl
{
    private static readonly Regex _decimalRegex = new(@"^[0-9]*\.?[0-9]*$", RegexOptions.Compiled);
    private static readonly Regex _intRegex     = new(@"^[0-9]*$",           RegexOptions.Compiled);

    public PropertyListView()
    {
        InitializeComponent();
    }

    private void NumericBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox tb)
        {
            var proposed = tb.Text[..tb.SelectionStart] + e.Text + tb.Text[(tb.SelectionStart + tb.SelectionLength)..];
            e.Handled = !_decimalRegex.IsMatch(proposed);
        }
    }

    private void NumericBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            if (!_decimalRegex.IsMatch((string)e.DataObject.GetData(typeof(string))!))
                e.CancelCommand();
        }
        else e.CancelCommand();
    }

    private void IntBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox tb)
        {
            var proposed = tb.Text[..tb.SelectionStart] + e.Text + tb.Text[(tb.SelectionStart + tb.SelectionLength)..];
            e.Handled = !_intRegex.IsMatch(proposed);
        }
    }
}
