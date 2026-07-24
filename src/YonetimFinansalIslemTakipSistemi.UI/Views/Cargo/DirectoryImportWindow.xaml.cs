using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Import;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

/// <summary>
/// Firma rehberi Excel içe aktarma sihirbazı — CargoImportWindow ile aynı 4 adımlı desen.
/// </summary>
public partial class DirectoryImportWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IDialogService _dialogService;
    private readonly DirectoryImportViewModel _vm;
    private bool _importCompleted;

    /// <summary>Pencere X ile kapatılsa bile çağıran liste bu bayrağa bakarak kendini yeniler.</summary>
    public bool ImportCompleted => _importCompleted;

    public DirectoryImportWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();
        _vm            = services.GetRequiredService<DirectoryImportViewModel>();
        DataContext    = _vm;
    }

    private void ShowStep(int step)
    {
        StepFilePanel.Visibility     = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        StepPreviewPanel.Visibility  = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        StepProgressPanel.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        StepSummaryPanel.Visibility  = step == 4 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title      = "Şablonu Kaydet",
            Filter     = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName   = "firma-rehberi-sablonu.xlsx",
            DefaultExt = "xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _services.GetRequiredService<ICargoImportTemplateService>().CreateDirectoryTemplate(dialog.FileName);
            _dialogService.ShowSuccess($"Şablon kaydedildi.\n{dialog.FileName}");
        }
        catch (Exception ex)
        {
            _ = _services.GetRequiredService<ISystemLogService>()
                .LogErrorAsync("DirectoryImport", "Şablon dosyası oluşturulamadı", ex, source: nameof(DirectoryImportWindow));
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

    private void BackButton_Click(object sender, RoutedEventArgs e) => ShowStep(1);

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_dialogService.ShowConfirmation(
                $"{_vm.SelectedCount} firma kaydı rehbere eklenecek.\n" +
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
        SummaryMainText.Text = $"{r.ImportedCount} firma kaydı rehbere eklendi.";
        SummaryDetailText.Text =
            $"Dosya: {r.SourceName}\n" +
            $"Analiz: {r.TotalRows} satır — {r.ValidCount} geçerli, {r.WarningCount} uyarılı, " +
            $"{r.ErrorCount} hatalı, {r.DuplicateCount} mükerrer\n" +
            "Ayrıntılar Audit Log ekranında (İşlem: Firma Rehberi Toplu İçe Aktarma) görülebilir.";
        ShowStep(4);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = _importCompleted;
        Close();
    }
}
