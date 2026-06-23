using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.DeleteCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Views.AuditLogs;
using YonetimFinansalIslemTakipSistemi.UI.Views.CashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.Views.Permissions;
using YonetimFinansalIslemTakipSistemi.UI.Views.Reports;
using YonetimFinansalIslemTakipSistemi.UI.Views.Users;
using YonetimFinansalIslemTakipSistemi.UI.Views.Analysis;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Analysis;
using YonetimFinansalIslemTakipSistemi.UI.Views.ExchangeRates;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class MainWindow : Window
{
    private const string ScreenKey = "CashTransactionList";

    private readonly IServiceProvider _services;
    private readonly CashTransactionListViewModel _listVm;
    private readonly IDialogService _dialogService;

    // Kolon adı → DataGridColumn eşlemesi
    private Dictionary<string, DataGridColumn> _columnByKey = new();

    // Bakiye kolonları için kullanıcının bireysel gizleme tercihi
    // true = kullanıcı bu kolonu görmek istiyor; false = gizledi
    // Default: hepsi true. Currency filter ile AND'lenir.
    private Dictionary<string, bool> _userBalancePref = new()
    {
        ["TlBakiye"]  = true,
        ["UsdBakiye"] = true,
        ["EurBakiye"] = true
    };

    public bool IsLogoutRequested { get; private set; }

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services      = services;
        _listVm        = services.GetRequiredService<CashTransactionListViewModel>();
        _dialogService = services.GetRequiredService<IDialogService>();
        DataContext    = _listVm;

        // Kolon key → column eşlemesi (InitializeComponent sonrası alanlar erişilebilir)
        _columnByKey = new Dictionary<string, DataGridColumn>
        {
            ["Tarih"]        = ColTarih,
            ["Tur"]          = ColTur,
            ["ParaBir"]      = ColParaBir,
            ["Aciklama"]     = ColAciklama,
            ["Borc"]         = ColBorc,
            ["Alacak"]       = ColAlacak,
            ["TlBakiye"]     = ColTlBakiye,
            ["UsdBakiye"]    = ColUsdBakiye,
            ["EurBakiye"]    = ColEurBakiye,
            ["OlusturulmaT"] = ColOlusturulmaT
        };

        var userContext = services.GetRequiredService<IUserContext>();
        LoggedInUserText.Text = string.IsNullOrWhiteSpace(userContext.FullName)
            ? string.Empty
            : userContext.FullName;

        // Currency filter değişince bakiye kolonlarını güncelle
        _listVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(CashTransactionListViewModel.ShowTlBalance)
                               or nameof(CashTransactionListViewModel.ShowUsdBalance)
                               or nameof(CashTransactionListViewModel.ShowEurBalance))
            {
                ApplyBalanceColumnVisibility();
            }
        };

        Loaded += async (_, _) =>
        {
            ApplyColumnHeaderContextMenu();
            await ApplySavedLayoutAsync();
            ApplyBalanceColumnVisibility();
            await _listVm.LoadAsync();
            RefreshMenuVisibility(userContext);
        };
    }

    // ── Column Header Context Menu ────────────────────────────────────────────

    private void ApplyColumnHeaderContextMenu()
    {
        var cm = new ContextMenu();
        cm.Opened += ColumnHeaderContextMenu_Opened;

        var headerStyle = new Style(typeof(DataGridColumnHeader));
        headerStyle.Setters.Add(new Setter(FrameworkElement.ContextMenuProperty, cm));
        TransactionDataGrid.ColumnHeaderStyle = headerStyle;
    }

    // ── Bakiye Kolonu Görünürlüğü ────────────────────────────────────────────
    // Efektif görünürlük = currencyFilterAllows AND userPref
    // Bu yaklaşım: currency filter "Tümü" iken kullanıcı TL Bakiye'yi gizleyebilir.
    // Filter değişince kullanıcı tercihi kaybolmaz.

    private void ApplyBalanceColumnVisibility()
    {
        SetBalanceColumnVisibility("TlBakiye",  _listVm.ShowTlBalance,  ColTlBakiye);
        SetBalanceColumnVisibility("UsdBakiye", _listVm.ShowUsdBalance, ColUsdBakiye);
        SetBalanceColumnVisibility("EurBakiye", _listVm.ShowEurBalance, ColEurBakiye);
    }

    private void SetBalanceColumnVisibility(string key, bool currencyAllows, DataGridColumn col)
    {
        var userWants = _userBalancePref.TryGetValue(key, out var pref) ? pref : true;
        col.Visibility = currencyAllows && userWants ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Layout Kayıt / Yükleme ────────────────────────────────────────────────

    private async Task ApplySavedLayoutAsync()
    {
        try
        {
            var userContext   = _services.GetRequiredService<IUserContext>();
            var layoutService = _services.GetRequiredService<IUserGridLayoutService>();
            var json = await layoutService.GetLayoutAsync(userContext.UserId, ScreenKey);
            if (string.IsNullOrEmpty(json)) return;

            var states = JsonSerializer.Deserialize<List<GridColumnState>>(json);
            if (states is null) return;

            foreach (var state in states)
            {
                if (!_columnByKey.TryGetValue(state.Key, out var col)) continue;

                // Bakiye kolonları için: kullanıcı tercihini geri yükle (currency filter sonradan AND'lenir)
                if (IsBalanceColumn(state.Key))
                {
                    _userBalancePref[state.Key] = state.IsVisible;
                }
                else
                {
                    col.Visibility = state.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                }

                if (state.DisplayIndex >= 0 && state.DisplayIndex < TransactionDataGrid.Columns.Count)
                    col.DisplayIndex = state.DisplayIndex;
                if (state.Width > 0)
                    col.Width = new DataGridLength(state.Width);
            }
        }
        catch
        {
            // Layout yüklenemezse varsayılana dön; kritik değil
        }
    }

    private async Task SaveGridLayoutAsync()
    {
        try
        {
            var userContext   = _services.GetRequiredService<IUserContext>();
            var layoutService = _services.GetRequiredService<IUserGridLayoutService>();

            var keyByColumn = _columnByKey.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            var states = TransactionDataGrid.Columns
                .Where(col => keyByColumn.ContainsKey(col))
                .Select(col =>
                {
                    var key = keyByColumn[col];
                    // Bakiye kolonları için kullanıcı tercihini kaydet (filtre durumunu değil)
                    var isVisible = IsBalanceColumn(key)
                        ? (_userBalancePref.TryGetValue(key, out var p) ? p : true)
                        : col.Visibility == Visibility.Visible;

                    return new GridColumnState(
                        Key:          key,
                        IsVisible:    isVisible,
                        DisplayIndex: col.DisplayIndex,
                        Width:        col.ActualWidth > 0 ? col.ActualWidth : col.Width.Value);
                })
                .ToList();

            var json = JsonSerializer.Serialize(states);
            await layoutService.SaveLayoutAsync(userContext.UserId, ScreenKey, json);
            _dialogService.ShowSuccess("Kolon tasarımı kaydedildi.");
        }
        catch
        {
            _dialogService.ShowError("Kolon tasarımı kaydedilemedi.");
        }
    }

    private async Task ResetGridLayoutAsync()
    {
        try
        {
            var userContext   = _services.GetRequiredService<IUserContext>();
            var layoutService = _services.GetRequiredService<IUserGridLayoutService>();
            await layoutService.DeleteLayoutAsync(userContext.UserId, ScreenKey);

            // Bakiye tercihleri sıfırla
            _userBalancePref["TlBakiye"]  = true;
            _userBalancePref["UsdBakiye"] = true;
            _userBalancePref["EurBakiye"] = true;

            // Tüm kolonları varsayılan görünürlüğe döndür
            foreach (var kvp in _columnByKey)
            {
                if (!IsBalanceColumn(kvp.Key))
                    kvp.Value.Visibility = Visibility.Visible;
            }

            // Bakiye kolonlarını currency filter ile yeniden uygula
            ApplyBalanceColumnVisibility();

            _dialogService.ShowSuccess("Kolon tasarımı varsayılana döndürüldü.");
        }
        catch
        {
            _dialogService.ShowError("Kolon tasarımı sıfırlanamadı.");
        }
    }

    private static bool IsBalanceColumn(string key)
        => key is "TlBakiye" or "UsdBakiye" or "EurBakiye";

    private static string GetColumnDisplayName(string key) => key switch
    {
        "Tarih"        => "Tarih",
        "Tur"          => "Tür",
        "ParaBir"      => "Para Bir.",
        "Aciklama"     => "Açıklama",
        "Borc"         => "Borç",
        "Alacak"       => "Alacak",
        "TlBakiye"     => "TL Bakiye",
        "UsdBakiye"    => "USD Bakiye",
        "EurBakiye"    => "EUR Bakiye",
        "OlusturulmaT" => "Oluşturulma",
        _              => key
    };

    // ── DataGrid Sağ Tıklama Context Menu ────────────────────────────────────

    private void ColumnHeaderContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu cm) return;
        cm.Items.Clear();

        var header        = cm.PlacementTarget as DataGridColumnHeader;
        var clickedColumn = header?.Column;

        // "Bu Kolonu Gizle"
        if (clickedColumn is not null)
        {
            var keyByCol = _columnByKey.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            if (keyByCol.TryGetValue(clickedColumn, out var clickedKey))
            {
                var hideItem = new MenuItem { Header = "Bu Kolonu Gizle" };
                hideItem.Click += (_, _) =>
                {
                    if (IsBalanceColumn(clickedKey))
                    {
                        _userBalancePref[clickedKey] = false;
                        ApplyBalanceColumnVisibility();
                    }
                    else
                    {
                        clickedColumn.Visibility = Visibility.Collapsed;
                    }
                };
                cm.Items.Add(hideItem);
                cm.Items.Add(new Separator());
            }
        }

        // "Gizlenen Kolonlar" bölümü
        var keyByColumn2 = _columnByKey.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        var hiddenCols   = TransactionDataGrid.Columns
            .Where(col => keyByColumn2.ContainsKey(col) && !IsEffectivelyVisible(col, keyByColumn2[col]))
            .ToList();

        var hiddenHeader = new MenuItem { Header = "Gizlenen Kolonlar", IsEnabled = false };
        cm.Items.Add(hiddenHeader);

        if (hiddenCols.Count == 0)
        {
            cm.Items.Add(new MenuItem { Header = "  (Gizlenen kolon yok)", IsEnabled = false });
        }
        else
        {
            foreach (var col in hiddenCols)
            {
                var colRef = col;
                var key    = keyByColumn2[col];
                var item   = new MenuItem { Header = $"  {GetColumnDisplayName(key)} — Göster" };
                item.Click += (_, _) =>
                {
                    if (IsBalanceColumn(key))
                    {
                        _userBalancePref[key] = true;
                        ApplyBalanceColumnVisibility();
                    }
                    else
                    {
                        colRef.Visibility = Visibility.Visible;
                    }
                };
                cm.Items.Add(item);
            }
        }

        cm.Items.Add(new Separator());

        // Tüm kolonlar için toggle
        var allHeader = new MenuItem { Header = "Kolonlar", IsEnabled = false };
        cm.Items.Add(allHeader);

        foreach (var col in TransactionDataGrid.Columns)
        {
            if (!keyByColumn2.TryGetValue(col, out var key)) continue;

            var colRef      = col;
            var isVisible   = IsEffectivelyVisible(col, key);
            var label       = GetColumnDisplayName(key);
            var isBalance   = IsBalanceColumn(key);
            var item = new MenuItem
            {
                Header      = $"  {label}",
                IsCheckable = true,
                IsChecked   = isVisible
            };
            item.Click += (_, _) =>
            {
                if (isBalance)
                {
                    _userBalancePref[key] = !_userBalancePref.TryGetValue(key, out var p) || !p;
                    ApplyBalanceColumnVisibility();
                    item.IsChecked = _userBalancePref[key];
                }
                else
                {
                    colRef.Visibility = colRef.Visibility == Visibility.Visible
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                    item.IsChecked = colRef.Visibility == Visibility.Visible;
                }
            };
            cm.Items.Add(item);
        }

        cm.Items.Add(new Separator());

        var saveItem = new MenuItem { Header = "Tasarımı Kaydet" };
        saveItem.Click += async (_, _) => await SaveGridLayoutAsync();
        cm.Items.Add(saveItem);

        var resetItem = new MenuItem { Header = "Varsayılan Tasarıma Dön" };
        resetItem.Click += async (_, _) => await ResetGridLayoutAsync();
        cm.Items.Add(resetItem);
    }

    // Bir kolonun gerçek görünürlüğü: bakiye kolonları için user pref + currency filter, diğerleri direkt
    private bool IsEffectivelyVisible(DataGridColumn col, string key)
    {
        if (IsBalanceColumn(key))
        {
            var currencyAllows = key switch
            {
                "TlBakiye"  => _listVm.ShowTlBalance,
                "UsdBakiye" => _listVm.ShowUsdBalance,
                "EurBakiye" => _listVm.ShowEurBalance,
                _           => false
            };
            var userWants = _userBalancePref.TryGetValue(key, out var p) ? p : true;
            return currencyAllows && userWants;
        }
        return col.Visibility == Visibility.Visible;
    }

    // ── Menü Görünürlüğü ──────────────────────────────────────────────────────

    private void RefreshMenuVisibility(IUserContext userContext)
    {
        var canManage   = userContext.HasPermission(PermissionType.CanManageUsers);
        var canAudit    = userContext.HasPermission(PermissionType.CanViewAuditLog);
        var canReports  = userContext.HasPermission(PermissionType.CanViewReports);
        var canExchange = userContext.HasPermission(PermissionType.CanManageExchangeRates);

        MenuItemKullanicilar.Visibility = canManage   ? Visibility.Visible : Visibility.Collapsed;
        MenuItemYetkiler.Visibility     = canManage   ? Visibility.Visible : Visibility.Collapsed;
        MenuItemDenetim.Visibility      = canAudit    ? Visibility.Visible : Visibility.Collapsed;
        MenuItemRaporlar.Visibility     = canReports  ? Visibility.Visible : Visibility.Collapsed;
        MenuItemAnaliz.Visibility       = canReports  ? Visibility.Visible : Visibility.Collapsed;
        MenuItemDoviz.Visibility        = canExchange ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── İşlem Butonları ───────────────────────────────────────────────────────

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
        if (!_dialogService.ShowConfirmation("Oturumu kapatmak istediğinize emin misiniz?", "Çıkış Yap"))
            return;

        IsLogoutRequested = true;
        Close();
    }

    // ── Menü Tıklamaları ─────────────────────────────────────────────────────

    private void OpenUserManagement_Click(object sender, RoutedEventArgs e)
    {
        new UserManagementWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenAuditLog_Click(object sender, RoutedEventArgs e)
    {
        new AuditLogWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenPermissions_Click(object sender, RoutedEventArgs e)
    {
        new UserPermissionWindow(_services) { Owner = this }.ShowDialog();
        var userContext = _services.GetRequiredService<IUserContext>();
        RefreshMenuVisibility(userContext);
    }

    private void OpenReports_Click(object sender, RoutedEventArgs e)
    {
        new ReportWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenAnalysis_Click(object sender, RoutedEventArgs e)
    {
        var vm = _services.GetRequiredService<AnalysisViewModel>();
        new AnalysisWindow(vm) { Owner = this }.ShowDialog();
    }

    private void OpenExchangeRates_Click(object sender, RoutedEventArgs e)
    {
        new ExchangeRateWindow(_services) { Owner = this }.ShowDialog();
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        var updateService = _services.GetRequiredService<IUpdateService>();

        if (!updateService.IsClickOnceDeployment)
        {
            _dialogService.ShowInfo("Güncelleme kontrolü yalnızca ClickOnce ile kurulu sürümde kullanılabilir.");
            return;
        }

        var result = await updateService.CheckForUpdateAsync();

        if (result.ErrorMessage == "io_error")
        {
            _dialogService.ShowWarning("Güncelleme sunucusuna erişilemiyor. Ağ bağlantınızı kontrol edin.");
            return;
        }

        if (result.ErrorMessage is not null)
        {
            _dialogService.ShowWarning("Güncelleme kontrolü sırasında beklenmeyen bir hata oluştu.");
            return;
        }

        if (!result.IsUpdateAvailable)
        {
            _dialogService.ShowInfo($"Uygulamanız güncel.\nMevcut sürüm: v{result.CurrentVersion}");
            return;
        }

        if (!_dialogService.ShowConfirmation(
                $"Yeni sürüm mevcut: v{result.LatestVersion}\nMevcut sürüm: v{result.CurrentVersion}\n\nŞimdi güncellemek ister misiniz?",
                "Güncelleme Mevcut"))
            return;

        if (!_dialogService.ShowConfirmation(
                "Güncelleme başlatılacak ve uygulama kapatılacak.\nDevam etmek istiyor musunuz?",
                "Uygulama Kapatılıyor"))
            return;

        // LaunchInstaller başarısız olursa (dosya yok, shell hatası) Shutdown çağrılmaz.
        if (!updateService.LaunchInstaller())
        {
            _dialogService.ShowError(
                "Güncelleme başlatılamadı. Güncelleme sunucusuna erişilemiyor veya kurulum dosyası bulunamadı.");
            return;
        }

        // Yeni sürecin spawn olması için kısa bekleme; ardından eski sürüm güvenle kapanır.
        await Task.Delay(800);
        System.Windows.Application.Current.Shutdown();
    }
}

internal sealed record GridColumnState(
    string Key,
    bool   IsVisible,
    int    DisplayIndex,
    double Width);
