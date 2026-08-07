using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.DeleteMailContact;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Mail;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Mail;

/// <summary>
/// Ortak mail rehberi yönetim ekranı — WhatsApp rehberiyle aynı kurallar:
/// rehber kullanıcı bazlı değildir, oturum açan kullanıcılar görüntüleyebilir;
/// yazma işlemleri kargo/firma rehberi yönetim izinlerinden birini gerektirir.
/// </summary>
public partial class MailContactListScreen : UserControl
{

    /// <summary>
    /// Alt diyalogların sahibi AĞAÇTAN bulunur. Aynı ekran hem ince
    /// barındırıcı pencerede hem kabuk sekmesinde durabiliyor; sabit bir
    /// pencereye bağlanırsa diğerinde sahipsiz diyalog açardı.
    /// </summary>
    private Window? HostWindow => Window.GetWindow(this);
    private readonly IServiceProvider _services;
    private readonly MailContactListViewModel _vm;
    private readonly IDialogService _dialogService;

    public MailContactListScreen(IServiceProvider services)
    {
        InitializeComponent();
        _services      = services;
        _vm            = services.GetRequiredService<MailContactListViewModel>();
        _dialogService = services.GetRequiredService<IDialogService>();
        DataContext    = _vm;

        // Handler'lar zaten guard'lı; buton gizleme UX tutarlılığı içindir
        var userContext = services.GetRequiredService<Application.Interfaces.Services.IUserContext>();
        var manageVisibility = MailContactPermissions.CanModify(userContext)
            ? Visibility.Visible : Visibility.Collapsed;
        NewButton.Visibility    = manageVisibility;
        EditButton.Visibility   = manageVisibility;
        DeleteButton.Visibility = manageVisibility;

        ScreenData.Bind(this, () => _vm.LoadAsync());
    }

    private async void NewButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new MailContactEditWindow(_services) { Owner = HostWindow };
        if (form.ShowDialog() == true)
            await _vm.LoadAsync();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
        => await EditSelectedAsync();

    private async void MainGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => await EditSelectedAsync();

    private async Task EditSelectedAsync()
    {
        if (_vm.Selected is null) return;
        var form = new MailContactEditWindow(_services) { Owner = HostWindow };
        form.InitializeForEdit(_vm.Selected);
        if (form.ShowDialog() == true)
            await _vm.LoadAsync();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        if (!_dialogService.ShowConfirmation(
                $"'{_vm.Selected.FullName}' kaydını mail rehberinden silmek istediğinize emin misiniz?",
                "Mail Kişisi Sil"))
            return;

        var handler = _services.GetRequiredService<DeleteMailContactHandler>();
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
