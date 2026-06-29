using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class CargoShipmentEditWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly CargoShipmentEditViewModel _vm;

    public CargoShipmentEditWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services   = services;
        _vm         = services.GetRequiredService<CargoShipmentEditViewModel>();
        DataContext = _vm;
        _vm.SaveCompleted += () => { DialogResult = true; Close(); };

        // UI gizlemesi; asıl koruma handler seviyesindedir
        var userContext = services.GetRequiredService<IUserContext>();
        AddCargoCompanyButton.Visibility = userContext.HasPermission(PermissionType.CanManageCargoCompanies)
            ? Visibility.Visible : Visibility.Collapsed;
        AddDirectoryButton.Visibility = userContext.HasPermission(PermissionType.CanManageCompanyDirectory)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Yeni kayıt için: yön belirle ve lookup'ları yükle.</summary>
    public async Task PrepareNewAsync(CargoShipmentDirection direction)
    {
        _vm.SetDirection(direction);
        await _vm.LoadLookupsAsync();
    }

    /// <summary>Düzenleme için: mevcut kaydı ViewModel'e aktar.</summary>
    public async Task PrepareEditAsync(CargoShipmentDto dto)
    {
        await _vm.InitializeAsync(dto);
    }

    /// <summary>Kopyalama için: kaynak kayıttan operasyonel alanları aktar, ID/audit/takip bilgilerini sıfırla.</summary>
    public async Task PrepareCopyAsync(CargoShipmentDto source)
    {
        await _vm.InitializeForCopyAsync(source);
    }

    private async void AddCargoCompanyButton_Click(object sender, RoutedEventArgs e)
    {
        var oldIds = _vm.CargoCompanies.Select(c => c.Id).ToHashSet();
        var dialog = new CargoCompanyEditWindow(_services) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            await _vm.LoadLookupsAsync();
            var newItem = _vm.CargoCompanies.FirstOrDefault(c => !oldIds.Contains(c.Id));
            if (newItem is not null)
                _vm.SelectedCargoCompany = newItem;
        }
    }

    private async void AddDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        var oldIds = _vm.CompanyDirectories.Select(d => d.Id).ToHashSet();
        var dialog = new CompanyDirectoryEditWindow(_services) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            await _vm.LoadLookupsAsync();
            var newItem = _vm.CompanyDirectories.FirstOrDefault(d => !oldIds.Contains(d.Id));
            if (newItem is not null)
                _vm.SelectedCompanyDirectory = newItem;
        }
    }

    private async void AddAttentionContactButton_Click(object sender, RoutedEventArgs e)
    {
        var dialogService = _services.GetRequiredService<IDialogService>();
        var name = _vm.AttentionContactInput?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            dialogService.ShowWarning("Eklenecek kişi adı boş olamaz.", "Dikkatine Ekle");
            return;
        }
        if (_vm.SelectedCompanyDirectory is null)
        {
            dialogService.ShowWarning("Dikkatine kişisi eklemek için önce bir firma seçin.", "Dikkatine Ekle");
            return;
        }
        await _vm.AddAttentionContactAsync(name);
        dialogService.ShowInfo($"'{name}' dikkatine listesine eklendi.", "Dikkatine Ekle");
    }

    private void RefreshSnapshotButton_Click(object sender, RoutedEventArgs e)
    {
        var dialogService = _services.GetRequiredService<IDialogService>();

        // Onay al: snapshot mevcut kargonun alıcı bilgilerini değiştirir
        var confirmed = dialogService.ShowConfirmation(
            "Bu işlem mevcut kargonun alıcı firma, adres, telefon ve e-posta snapshot bilgilerini\n" +
            "firma rehberindeki güncel verilerle değiştirecek.\n\n" +
            "Devam edilsin mi?",
            "Firma Bilgilerini Yenile");

        if (!confirmed) return;

        _vm.RefreshSnapshotFromDirectory();

        dialogService.ShowSuccess(
            "Firma snapshot bilgileri güncellendi.\n" +
            "Kaydet butonuna basarak değişiklikleri kalıcı hale getirin.",
            "Yenilendi");
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
