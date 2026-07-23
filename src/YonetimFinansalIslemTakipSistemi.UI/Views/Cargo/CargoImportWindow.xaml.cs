using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

/// <summary>
/// Excel içe aktarma sihirbazı: Dosya Seç → Önizleme → İşlem → Sonuç.
/// Adımlar tek pencerede panel görünürlüğüyle yönetilir
/// (CargoNotificationPreviewWindow deseni). İçe aktarma gerçekleştiyse
/// pencere DialogResult=true ile kapanır — çağıran liste kendini yeniler.
/// </summary>
public partial class CargoImportWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IDialogService _dialogService;
    private readonly CargoImportViewModel _vm;
    private bool _importCompleted;

    /// <summary>
    /// İçe aktarma gerçekleşti mi — pencere X ile kapatılsa bile çağıran liste
    /// bu bayrağa bakarak kendini yeniler (DialogResult'a ek güvence).
    /// </summary>
    public bool ImportCompleted => _importCompleted;

    public CargoImportWindow(IServiceProvider services, CargoShipmentDirection direction)
    {
        InitializeComponent();
        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();

        // Direction runtime parametresi gerektirdiği için VM elle kurulur
        // (CargoShipmentListViewModel ile aynı desen)
        _vm = new CargoImportViewModel(
            services.GetRequiredService<AnalyzeCargoImportHandler>(),
            services.GetRequiredService<ImportCargoShipmentsHandler>(),
            services.GetRequiredService<IUserContext>(),
            direction);
        DataContext = _vm;

        Title = direction == CargoShipmentDirection.Incoming
            ? "Gelen Kargo — Excel'den İçe Aktar"
            : "Giden Kargo — Excel'den İçe Aktar";
        FileStepTitle.Text = Title;
    }

    private void ShowStep(int step)
    {
        StepFilePanel.Visibility     = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        StepPreviewPanel.Visibility  = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        StepProgressPanel.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        StepSummaryPanel.Visibility  = step == 4 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Adım 1: Dosya seç + şablon ──────────────────────────────────────────

    private void TemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title      = "Şablonu Kaydet",
            Filter     = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName   = "kargo-iceaktarma-sablonu.xlsx",
            DefaultExt = "xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _services.GetRequiredService<ICargoImportTemplateService>().CreateTemplate(dialog.FileName);
            _dialogService.ShowSuccess($"Şablon kaydedildi.\n{dialog.FileName}");
        }
        catch (Exception ex)
        {
            _ = _services.GetRequiredService<ISystemLogService>()
                .LogErrorAsync("CargoImport", "Şablon dosyası oluşturulamadı", ex, source: nameof(CargoImportWindow));
            _dialogService.ShowError("Şablon kaydedilemedi. Dosya açıksa kapatıp tekrar deneyin.");
        }
    }

    private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title  = "İçe Aktarılacak Excel Dosyasını Seçin",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx|Tüm Dosyalar (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        };
        if (dialog.ShowDialog() != true) return;

        ProgressTitle.Text = "Dosya analiz ediliyor…";
        ShowStep(3);

        var error = await _vm.AnalyzeAsync(dialog.FileName);
        if (error is not null)
        {
            _dialogService.ShowError(error, "Analiz Hatası");
            ShowStep(1);
            return;
        }

        ShowStep(2);
    }

    // ── Adım 2: Önizleme ────────────────────────────────────────────────────

    private void BackButton_Click(object sender, RoutedEventArgs e) => ShowStep(1);

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_dialogService.ShowConfirmation(
                $"{_vm.SelectedCount} kargo kaydı oluşturulacak.\n" +
                "İşlem başladıktan sonra iptal edilemez; kayıtların tamamı oluşturulur veya hiçbiri oluşturulmaz.\n\nDevam edilsin mi?",
                "İçe Aktarma Onayı"))
            return;

        ProgressTitle.Text = "Kayıtlar oluşturuluyor…";
        ShowStep(3);

        var result = await _vm.ImportAsync();
        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "İçe aktarma başarısız.", "İçe Aktarma Hatası");
            ShowStep(2);
            return;
        }

        _importCompleted = true;
        var r = result.Data!;
        SummaryMainText.Text = $"{r.ImportedCount} kargo kaydı oluşturuldu.\nNumara aralığı: {r.FirstShipmentNumber} – {r.LastShipmentNumber}";
        SummaryDetailText.Text =
            $"Dosya: {r.SourceName}\n" +
            $"Analiz: {r.TotalRows} satır — {r.ValidCount} geçerli, {r.WarningCount} uyarılı, " +
            $"{r.ErrorCount} hatalı, {r.DuplicateCount} mükerrer\n" +
            "Ayrıntılar Audit Log ekranında (İşlem: Kargo Toplu İçe Aktarma) görülebilir.";
        ShowStep(4);
    }

    // ── Adım 4: Sonuç ───────────────────────────────────────────────────────

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = _importCompleted;
        Close();
    }
}
