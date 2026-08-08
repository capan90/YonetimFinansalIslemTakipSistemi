using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.DeleteCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.QuickUpdateCargoStatus;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

/// <summary>
/// Gelen/Giden kargo listesi ekranı (Faz D6).
///
/// Aynı sınıf iki ekranı da karşılar; hangisi olduğu kurucudaki yön
/// parametresinden gelir — kayıt tablosunda iki ayrı satır olarak durur.
/// </summary>
public partial class CargoShipmentListScreen : UserControl, IShellCloseSource, IShellNavigationAware
{
    /// <summary>
    /// Alt diyalogların sahibi AĞAÇTAN bulunur. Aynı ekran hem ince
    /// barındırıcı pencerede hem kabuk sekmesinde durabiliyor; sabit bir
    /// pencereye bağlanırsa diğerinde sahipsiz diyalog açardı.
    /// </summary>
    private Window? HostWindow => Window.GetWindow(this);

    /// <summary>Kapanma isteği — pencerede pencereyi, kabukta sekmeyi kapatır.</summary>
    public event Action? CloseRequested;

    /// <summary>Kabuk sekme oluştururken atar; ince barındırıcıda null kalır.</summary>
    public IShellNavigator? Navigator { get; set; }

    /// <summary>Ekran başlığı — sekme başlığı ve içerideki başlık aynı kaynaktan.</summary>
    public string ScreenTitle { get; }

    private readonly IServiceProvider _services;
    private readonly CargoShipmentListViewModel _vm;
    private readonly IDialogService _dialogService;

    public CargoShipmentListScreen(IServiceProvider services, CargoShipmentDirection direction)
    {
        InitializeComponent();
        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();

        var listHandler        = services.GetRequiredService<GetCargoShipmentListHandler>();
        var quickStatusHandler = services.GetRequiredService<QuickUpdateCargoStatusHandler>();
        _vm = new CargoShipmentListViewModel(listHandler, quickStatusHandler, direction);
        DataContext = _vm;

        ScreenTitle     = direction == CargoShipmentDirection.Incoming ? "Gelen Kargolar" : "Giden Kargolar";
        TitleBlock.Text = ScreenTitle;

        // UI gizlemesi; asıl koruma handler seviyesindedir
        var userContext = services.GetRequiredService<IUserContext>();
        var managePermission = direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;
        var manageVisibility = userContext.HasPermission(managePermission)
            ? Visibility.Visible : Visibility.Collapsed;
        NewButton.Visibility    = manageVisibility;
        CopyButton.Visibility   = manageVisibility;
        EditButton.Visibility   = manageVisibility;
        DeleteButton.Visibility = manageVisibility;
        ImportButton.Visibility = manageVisibility;

        ScreenData.Bind(this, () => _vm.LoadAsync());

        // Sekmeye dönüldüğünde YALNIZCA GEREKİYORSA tazele (Faz F5).
        //
        // Operasyon merkezi AYRI BİR SEKMEDE açılıyor ve modal değil; orada
        // değişen kargo durumu bu listede eski görünürdü. Faz E1'de bu,
        // "sekmeye her dönüşte yeniden sorgula" ile çözülmüştü — doğruydu
        // ama pahalıydı: kullanıcı sekmeler arasında gezindikçe hiçbir şey
        // değişmese de sorgu gidiyordu.
        //
        // Artık soru açık: açtığımız operasyon merkezi kaydı DEĞİŞTİRDİ Mİ?
        // Değiştirmediyse sorgu yok.
        //
        // İlk gösterim ScreenData'nın işi; buradaki ilk geçiş tüketilir,
        // yoksa açılışta iki sorgu giderdi.
        var ilkGosterim = true;
        IsVisibleChanged += async (_, e) =>
        {
            if (e.NewValue is not true) return;

            if (ilkGosterim)
            {
                ilkGosterim = false;
                return;
            }

            if (!ConsumeOperationCenterChange()) return;

            await ReloadAsync();
        };
    }

    // ── Klavye Kısayolları ────────────────────────────────────────────────────
    // Sarmalayıcılar: RoutedUICommand imzası Click handler'ınkinden farklı.
    // Handler'lar değiştirilmedi; yetki kapıları buton görünürlüğünde olduğu için
    // kısayol da aynı kapıya tabi tutulur.

    private void Command_New(object sender, ExecutedRoutedEventArgs e)
    {
        if (NewButton.Visibility == Visibility.Visible)
            NewButton_Click(sender, new RoutedEventArgs());
    }

    private void Command_Duplicate(object sender, ExecutedRoutedEventArgs e)
    {
        if (CopyButton.Visibility == Visibility.Visible && _vm.HasSelected)
            CopyButton_Click(sender, new RoutedEventArgs());
    }

    private void Command_Delete(object sender, ExecutedRoutedEventArgs e)
    {
        // Onay diyaloğu handler'ın içinde — kısayol onu atlamaz
        if (DeleteButton.Visibility == Visibility.Visible && _vm.HasSelected)
            DeleteButton_Click(sender, new RoutedEventArgs());
    }

    private void Command_Edit(object sender, ExecutedRoutedEventArgs e)
    {
        if (EditButton.Visibility == Visibility.Visible && _vm.HasSelected)
            EditButton_Click(sender, new RoutedEventArgs());
    }

    private void Command_Refresh(object sender, ExecutedRoutedEventArgs e)
        => RefreshButton_Click(sender, new RoutedEventArgs());

    private void Command_FocusSearch(object sender, ExecutedRoutedEventArgs e)
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    // Esc: ekran kendini kapatmaz, İSTER. Pencerede pencere kapanır,
    // kabukta sekme kapanır — karar barındıranın.
    private void Command_Close(object sender, ExecutedRoutedEventArgs e) => CloseRequested?.Invoke();

    private async void NewButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new CargoShipmentEditWindow(_services) { Owner = HostWindow };
        await form.PrepareNewAsync(_vm.Direction);
        if (form.ShowDialog() == true) await ReloadAsync();
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new CargoImportWindow(_services, _vm.Direction) { Owner = HostWindow };
        wizard.ShowDialog();
        // X ile kapatılsa bile içe aktarma yapıldıysa liste yenilenir
        if (wizard.ImportCompleted) await ReloadAsync();
    }

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var form = new CargoShipmentEditWindow(_services) { Owner = HostWindow };
        await form.PrepareCopyAsync(_vm.Selected);
        if (form.ShowDialog() == true) await ReloadAsync();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var form = new CargoShipmentEditWindow(_services) { Owner = HostWindow };
        await form.PrepareEditAsync(_vm.Selected);
        if (form.ShowDialog() == true) await ReloadAsync();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var label = string.IsNullOrWhiteSpace(_vm.Selected.ShipmentNumber)
            ? "seçili kargo kaydını"
            : $"'{_vm.Selected.ShipmentNumber}' kargo kaydını";

        if (!_dialogService.ShowConfirmation(
                $"{label} silmek istediğinize emin misiniz?", "Kargo Sil"))
            return;

        var handler     = _services.GetRequiredService<DeleteCargoShipmentHandler>();
        var userContext = _services.GetRequiredService<IUserContext>();

        var result = await handler.HandleAsync(new DeleteCargoShipmentRequest
        {
            Id              = _vm.Selected.Id,
            Direction       = _vm.Direction,
            DeletedByUserId = userContext.UserId
        });

        if (!result.Success)
            _dialogService.ShowError(result.ErrorMessage ?? "Beklenmeyen bir hata oluştu.");
        else
            await ReloadAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await ReloadAsync();

    private async void MainGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Tıklama başlık veya boş alan üzerindeyse form açılmaz
        var hit = e.OriginalSource as DependencyObject;
        while (hit is not null && hit is not DataGridRow)
            hit = VisualTreeHelper.GetParent(hit);
        if (hit is null) return;

        if (_vm.Selected is null) return;
        var form = new CargoShipmentEditWindow(_services) { Owner = HostWindow };
        await form.PrepareEditAsync(_vm.Selected);
        if (form.ShowDialog() == true) await ReloadAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await ReloadAsync();
    }

    private async void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // InitializeComponent sırasında ComboBox ItemsSource bağlanınca SelectionChanged tetiklenir.
        // IsLoaded false iken henüz Loaded event ateşlenmemiştir; data yükü Loaded handler'a bırakılır.
        if (!IsLoaded) return;
        await ReloadAsync();
    }

    private async void PriorityFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await ReloadAsync();
    }

    /// <summary>
    /// Seçili kargo için Operasyon Merkezini açar.
    /// Etiket, WhatsApp, Mail, Takip, Durum Değiştir işlemleri buradan yapılır.
    ///
    /// Sekme olarak açılır: farklı kargolar ayrı sekmelerde durabilir, aynı
    /// kargo ikinci kez açılmaz. Sekme modal olmadığı için "kapanınca yenile"
    /// beklenemez; tazeleme bu sekmeye geri dönüldüğünde yapılır (bkz. kurucu).
    /// </summary>
    private void OperationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;

        // Açılan örnek saklanır: sekmeye dönüldüğünde "değişti mi" diye ona
        // sorulur (bkz. ConsumeOperationCenterChange).
        var view = Navigator?.OpenScreenView(ScreenKey.CargoOperationCenter, _vm.Selected);

        if (view is CargoOperationCenterScreen opCenter)
            _openedOperationCenter = new WeakReference<CargoOperationCenterScreen>(opCenter);
    }

    /// <summary>
    /// Açtığımız operasyon merkezinde değişiklik oldu mu — ve bayrağı TÜKET.
    ///
    /// ZAYIF REFERANS bilinçli: operasyon merkezi sekmesi kapandığında bu
    /// liste onu bellekte tutmamalı (bkz. Faz F4 bellek ölçümü). Kapanmışsa
    /// zaten sorulacak bir şey yok.
    ///
    /// Bayrak tüketilir: aynı değişiklik için iki kez sorgu atılmaz.
    /// </summary>
    private bool ConsumeOperationCenterChange()
    {
        if (_openedOperationCenter is null) return false;
        if (!_openedOperationCenter.TryGetTarget(out var opCenter)) return false;
        if (!opCenter.WasModified) return false;

        opCenter.ClearModified();
        return true;
    }

    private WeakReference<CargoOperationCenterScreen>? _openedOperationCenter;

    /// <summary>
    /// Listeyi tazeler ve KULLANICININ YERİNİ korur (Faz F5): seçili kargo
    /// ve kaydırma konumu geri kurulur.
    ///
    /// Tazeleme koleksiyonu baştan kuruyor; seçim korunmazsa kullanıcı bir
    /// kaydı düzenleyip kaydettiğinde liste başa atlıyor ve üzerinde
    /// çalıştığı satırı yeniden arıyordu.
    /// </summary>
    private Task ReloadAsync() =>
        ScreenData.RefreshPreservingSelectionAsync(
            MainGrid,
            item => (item as CargoShipmentDto)?.Id,
            () => _vm.LoadAsync());

    /// <summary>Takip linkine tıklandığında default tarayıcıda açar.</summary>
    private void TrackingUrl_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        if (e.Uri is not null)
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
