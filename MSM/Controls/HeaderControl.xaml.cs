using System.ComponentModel;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MSM.ViewModels;

namespace MSM.Controls;

public partial class HeaderControl : UserControl
{
    public HeaderControl()
    {
        InitializeComponent();
        if (!DesignerProperties.GetIsInDesignMode(this))
            DataContext = App.ServiceProvider.GetRequiredService<HeaderViewModel>();
    }
}
