using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.CashTransactions;

public partial class CashTransactionFormWindow : Window
{
    private readonly CashTransactionFormViewModel _vm;
    private readonly IDialogService _dialogService;
    private bool _hasChanges;

    public CashTransactionFormWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm             = services.GetRequiredService<CashTransactionFormViewModel>();
        _dialogService  = services.GetRequiredService<IDialogService>();
        DataContext     = _vm;
        _vm.SaveCompleted = () => { DialogResult = true; };

        // Kaydedilmemiş değişiklik takibi — X butonu ile kapatmada uyarı için
        Loaded += (_, _) =>
        {
            _hasChanges = false;
            _vm.PropertyChanged += TrackChange;
        };
    }

    private void TrackChange(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // ErrorMessage ve WindowTitle değişimleri kullanıcı eylemi değil
        if (e.PropertyName is nameof(CashTransactionFormViewModel.ErrorMessage)
                           or nameof(CashTransactionFormViewModel.WindowTitle))
            return;
        _hasChanges = true;
    }

    /// <summary>
    /// Düzenleme modunda açmak için ShowDialog() öncesi çağrılır.
    /// </summary>
    public void InitializeForEdit(CashTransactionDto dto) => _vm.Initialize(dto);

    /// <summary>
    /// Kopyalama modunda açmak için ShowDialog() öncesi çağrılır.
    /// Mevcut kayıt değişmez; kaydet tıklandığında yeni kayıt oluşturulur.
    /// </summary>
    public void InitializeForCopy(CashTransactionDto dto) => _vm.InitializeForCopy(dto);

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        // DialogResult != null → Kaydet (true) veya İptal butonu/ESC (false) — uyarı gerekmez
        if (DialogResult is not null) return;
        if (!_hasChanges) return;

        if (!_dialogService.ShowConfirmation(
                "Kaydedilmemiş değişiklikler var. Çıkmak istediğinize emin misiniz?",
                "Kaydedilmemiş Değişiklikler"))
            e.Cancel = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
