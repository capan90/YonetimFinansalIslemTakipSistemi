using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Features.Settings.UserPreferences;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Settings;

/// <summary>
/// Ayarlar → Harf Duyarlılığı. Kullanıcı bazlı tercih; kaydedilince oturuma anında uygulanır.
/// </summary>
public partial class TextCaseSettingsWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IDialogService _dialogService;

    public TextCaseSettingsWindow(IServiceProvider services)
    {
        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var handler = _services.GetRequiredService<GetUserPreferenceHandler>();
        var current = await handler.HandleAsync();
        SelectPreference(current);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        // Çift tıklama koruması: işlem sürerken buton devre dışı
        SaveButton.IsEnabled = false;

        var tag = (PreferenceCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "Preserve";
        var preference = Enum.TryParse<TextCasePreference>(tag, out var parsed)
            ? parsed
            : TextCasePreference.Preserve;

        var handler = _services.GetRequiredService<SaveUserPreferenceHandler>();
        var result  = await handler.HandleAsync(preference);

        if (!result.Success)
        {
            // Başarısız kayıt: pencere açık kalır, kullanıcı tekrar deneyebilir
            SaveButton.IsEnabled = true;
            _dialogService.ShowError(result.ErrorMessage ?? "Harf duyarlılığı ayarı kaydedilemedi.");
            return;
        }

        // Başarı mesajı kısaca gösterilir, ardından pencere otomatik kapanır
        // (CargoNotificationPreviewWindow ile aynı desen)
        StatusText.Text       = "Harf duyarlılığı ayarı kaydedildi.";
        StatusText.Visibility = Visibility.Visible;
        await Task.Delay(800);
        DialogResult = true;
        Close();
    }

    private void SelectPreference(TextCasePreference preference)
    {
        var tag = preference.ToString();
        foreach (ComboBoxItem item in PreferenceCombo.Items)
        {
            if (item.Tag as string == tag)
            {
                PreferenceCombo.SelectedItem = item;
                return;
            }
        }
        PreferenceCombo.SelectedIndex = 0; // fallback: Olduğu Gibi
    }
}
