using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Settings;

public partial class AppearanceSettingsWindow : Window
{
    private readonly IThemeService _themeService;

    public AppearanceSettingsWindow(IServiceProvider services)
    {
        _themeService = services.GetRequiredService<IThemeService>();
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var current = await _themeService.GetCurrentThemeAsync();
        SelectTheme(current);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var tag = (ThemeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "Light";
        await _themeService.SetThemeAsync(tag);

        StatusText.Text       = "Tema ayarı kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
    }

    private void SelectTheme(string theme)
    {
        foreach (ComboBoxItem item in ThemeCombo.Items)
        {
            if (item.Tag as string == theme)
            {
                ThemeCombo.SelectedItem = item;
                return;
            }
        }
        ThemeCombo.SelectedIndex = 0; // fallback: Light
    }
}
