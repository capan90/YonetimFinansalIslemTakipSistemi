using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.DeleteWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.WhatsApp;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.WhatsApp;

/// <summary>
/// Ortak WhatsApp rehberi yönetim ekranı. Rehber kullanıcı bazlı değildir;
/// oturum açan kullanıcılar görüntüleyip yönetebilir (ayrı permission yoktur).
/// </summary>
public partial class WhatsAppContactListScreen : UserControl
{

    /// <summary>
    /// Alt diyalogların sahibi AĞAÇTAN bulunur. Aynı ekran hem ince
    /// barındırıcı pencerede hem kabuk sekmesinde durabiliyor; sabit bir
    /// pencereye bağlanırsa diğerinde sahipsiz diyalog açardı.
    /// </summary>
    private Window? HostWindow => Window.GetWindow(this);
    private readonly IServiceProvider _services;
    private readonly WhatsAppContactListViewModel _vm;
    private readonly IDialogService _dialogService;

    public WhatsAppContactListScreen(IServiceProvider services)
    {
        InitializeComponent();
        _services      = services;
        _vm            = services.GetRequiredService<WhatsAppContactListViewModel>();
        _dialogService = services.GetRequiredService<IDialogService>();
        DataContext    = _vm;

        // Yazma butonları (Yeni/Düzenle/Sil/İçe Aktar) yalnızca rehber yazma yetkisi olanlara gösterilir.
        // Handler'lar zaten guard'lı; buton gizleme UX tutarlılığı içindir (CargoCompany/CompanyDirectory
        // liste ekranlarıyla aynı desen — yetkisiz kullanıcıya "yapamazsın" butonu gösterilmez).
        var userContext = services.GetRequiredService<Application.Interfaces.Services.IUserContext>();
        var manageVisibility =
            Application.Features.WhatsAppContacts.WhatsAppContactPermissions.CanModify(userContext)
                ? Visibility.Visible : Visibility.Collapsed;
        NewButton.Visibility    = manageVisibility;
        EditButton.Visibility   = manageVisibility;
        DeleteButton.Visibility = manageVisibility;
        ImportButton.Visibility = manageVisibility;

        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private async void NewButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new WhatsAppContactEditWindow(_services) { Owner = HostWindow };
        if (form.ShowDialog() == true)
            await _vm.LoadAsync();
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new WhatsAppImportWindow(_services) { Owner = HostWindow };
        wizard.ShowDialog();
        // X ile kapatılsa bile içe aktarma yapıldıysa liste yenilenir
        if (wizard.ImportCompleted) await _vm.LoadAsync();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
        => await EditSelectedAsync();

    private async void MainGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => await EditSelectedAsync();

    private async Task EditSelectedAsync()
    {
        if (_vm.Selected is null) return;
        var form = new WhatsAppContactEditWindow(_services) { Owner = HostWindow };
        form.InitializeForEdit(_vm.Selected);
        if (form.ShowDialog() == true)
            await _vm.LoadAsync();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        if (!_dialogService.ShowConfirmation(
                $"'{_vm.Selected.FullName}' kaydını rehberden silmek istediğinize emin misiniz?",
                "Rehber Kişisi Sil"))
            return;

        var handler = _services.GetRequiredService<DeleteWhatsAppContactHandler>();
        var result  = await handler.HandleAsync(_vm.Selected.Id);

        if (!result.Success)
            _dialogService.ShowError(result.ErrorMessage ?? "Beklenmeyen bir hata oluştu.");
        else
            await _vm.LoadAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await _vm.LoadAsync();
    }
}
