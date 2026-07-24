using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.DeleteWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.WhatsApp;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.WhatsApp;

/// <summary>
/// Ortak WhatsApp rehberi yönetim ekranı. Rehber kullanıcı bazlı değildir;
/// oturum açan kullanıcılar görüntüleyip yönetebilir (ayrı permission yoktur).
/// </summary>
public partial class WhatsAppContactListWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly WhatsAppContactListViewModel _vm;
    private readonly IDialogService _dialogService;

    public WhatsAppContactListWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services      = services;
        _vm            = services.GetRequiredService<WhatsAppContactListViewModel>();
        _dialogService = services.GetRequiredService<IDialogService>();
        DataContext    = _vm;

        // İçe aktarma toplu yazma işlemidir — Sprint 17 rehber yazma guard'ıyla aynı kural
        var userContext = services.GetRequiredService<Application.Interfaces.Services.IUserContext>();
        ImportButton.Visibility =
            Application.Features.WhatsAppContacts.WhatsAppContactPermissions.CanModify(userContext)
                ? Visibility.Visible : Visibility.Collapsed;

        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private async void NewButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new WhatsAppContactEditWindow(_services) { Owner = this };
        if (form.ShowDialog() == true)
            await _vm.LoadAsync();
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new WhatsAppImportWindow(_services) { Owner = this };
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
        var form = new WhatsAppContactEditWindow(_services) { Owner = this };
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
