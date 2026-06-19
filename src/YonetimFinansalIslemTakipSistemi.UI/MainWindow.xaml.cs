using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.DeleteCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Views.AuditLogs;
using YonetimFinansalIslemTakipSistemi.UI.Views.CashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.Views.Permissions;
using YonetimFinansalIslemTakipSistemi.UI.Views.Users;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly CashTransactionListViewModel _listVm;
    private readonly IDialogService _dialogService;

    /// <summary>
    /// true → kullanıcı "Çıkış Yap" seçti; App.xaml.cs döngü yeni login açar.
    /// false → pencere X butonuyla kapatıldı; App.xaml.cs uygulamayı kapatır.
    /// </summary>
    public bool IsLogoutRequested { get; private set; }

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services       = services;
        _listVm         = services.GetRequiredService<CashTransactionListViewModel>();
        _dialogService  = services.GetRequiredService<IDialogService>();
        DataContext     = _listVm;

        // Oturumdaki kullanıcı adını araç çubuğuna yaz; boş ise gösterme
        var userContext = services.GetRequiredService<IUserContext>();
        LoggedInUserText.Text = string.IsNullOrWhiteSpace(userContext.FullName)
            ? string.Empty
            : userContext.FullName;

        Loaded += async (_, _) =>
        {
            await _listVm.LoadAsync();
            RefreshMenuVisibility(userContext);
        };
    }

    /// <summary>
    /// Menü öğelerini oturumun iznine göre gizle/göster.
    /// Gerçek güvenlik handler seviyesindedir; bu yalnızca UI kolaylığıdır.
    /// </summary>
    private void RefreshMenuVisibility(IUserContext userContext)
    {
        var canManage = userContext.HasPermission(PermissionType.CanManageUsers);
        var canAudit  = userContext.HasPermission(PermissionType.CanViewAuditLog);

        MenuItemKullanicilar.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        MenuItemYetkiler.Visibility     = canManage ? Visibility.Visible : Visibility.Collapsed;
        MenuItemDenetim.Visibility      = canAudit  ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void NewTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new CashTransactionFormWindow(_services) { Owner = this };
        if (form.ShowDialog() == true)
            await _listVm.LoadAsync();
    }

    private async void EditTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _listVm.SelectedTransaction;
        if (selected is null) return;

        var form = new CashTransactionFormWindow(_services) { Owner = this };
        form.InitializeForEdit(selected);
        if (form.ShowDialog() == true)
            await _listVm.LoadAsync();
    }

    private async void DeleteTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _listVm.SelectedTransaction;
        if (selected is null) return;

        var label = string.IsNullOrWhiteSpace(selected.Description) ? "seçili işlemi" : $"'{selected.Description}'";
        if (!_dialogService.ShowConfirmation($"{label} silmek istediğinize emin misiniz?", "İşlem Sil"))
            return;

        var handler     = _services.GetRequiredService<DeleteCashTransactionHandler>();
        var userContext = _services.GetRequiredService<IUserContext>();

        var request = new DeleteCashTransactionRequest
        {
            Id              = selected.Id,
            DeletedByUserId = userContext.UserId
        };

        var result = await handler.HandleAsync(request);
        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Beklenmeyen bir hata oluştu.");
            return;
        }

        await _listVm.LoadAsync();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        // Onay reddedilirse hiçbir şey değişmez; pencere açık kalır
        if (!_dialogService.ShowConfirmation("Oturumu kapatmak istediğinize emin misiniz?", "Çıkış Yap"))
            return;

        // Logout: flag set, pencereyi kapat. Session temizleme App.xaml.cs'te scope dispose sonrası yapılır.
        IsLogoutRequested = true;
        Close();
    }

    private void OpenUserManagement_Click(object sender, RoutedEventArgs e)
    {
        var win = new UserManagementWindow(_services) { Owner = this };
        win.ShowDialog();
    }

    private void OpenAuditLog_Click(object sender, RoutedEventArgs e)
    {
        var win = new AuditLogWindow(_services) { Owner = this };
        win.ShowDialog();
    }

    private void OpenPermissions_Click(object sender, RoutedEventArgs e)
    {
        var win = new UserPermissionWindow(_services) { Owner = this };
        win.ShowDialog();

        // Yetkiler değişmiş olabilir — menü görünürlüğünü yenile
        var userContext = _services.GetRequiredService<IUserContext>();
        RefreshMenuVisibility(userContext);
    }
}
