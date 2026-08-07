using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.DeleteCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.CashTransactions;

/// <summary>
/// Nakit işlemler ekranı — Faz D pilot dönüşümü.
///
/// MainWindow'un içeriği buraya taşındı; MainWindow yerinde durup bu ekranı
/// barındırıyor. Kullanıcının akışı değişmedi.
///
/// SORUMLULUK SINIRI:
///   Burada  → nakit işlem listesi, filtreler, CRUD, Excel içe aktarma,
///             kolon düzeni kalıcılığı, sütun başlığı context menu'sü,
///             bakiye kartları ve sparkline
///   Pencerede → menü, çıkış onayı/audit/kapatma, açılışta güncelleme kontrolü
///
/// Davranış İKİ YERDE tutulmuyor: pencere seviyesi işler MainWindow'da,
/// ekran işleri burada. Klavye kısayolları pencerede tanımlı kalır (odak
/// nerede olursa olsun çalışmaları için) ama buradaki genel metotlara
/// YÖNLENDİRİLİR — mantık kopyalanmaz.
///
/// FAZ D5: Ekran artık kabukta gerçek sekme olarak da açılıyor. Bunun için
/// iki bağlantı noktası eklendi, davranış değişmedi:
///   • <see cref="IShellLogoutSource"/> — araç çubuğundaki çıkış düğmesi
///     kabuk içinde de duyulsun diye
///   • Kendi <c>CommandBindings</c>'i (bkz. XAML) — kısayollar barındıran
///     pencereden bağımsız olarak bu ekrana ulaşsın diye
/// </summary>
public partial class CashTransactionsScreen : UserControl, IShellLogoutSource, IShellNavigationAware
{
    /// <summary>
    /// Kabuk sekme oluştururken atar; ince barındırıcı pencerede null kalır.
    ///
    /// Bu ekran başka ekran AÇMIYOR — gezgini yalnızca "kabuk içinde miyim?"
    /// sorusuna cevap olarak kullanıyor: araç çubuğundaki oturum bölümü
    /// (kullanıcı adı + çıkış) kabukta durum şeridi ve navigasyon rayıyla
    /// tekrarlanacağı için orada gizleniyor.
    /// </summary>
    public IShellNavigator? Navigator { get; set; }

    /// <summary>Kolon düzeninin kullanıcı bazında saklandığı ekran anahtarı.</summary>
    private const string LayoutScreenKey = "CashTransactionList";

    private readonly IServiceProvider            _services;
    private readonly CashTransactionListViewModel _listVm;
    private readonly IDialogService              _dialogService;

    // Kolon adı → DataGridColumn eşlemesi
    private readonly Dictionary<string, DataGridColumn> _columnByKey;

    // Bakiye kolonları için kullanıcının bireysel gizleme tercihi
    // true = kullanıcı bu kolonu görmek istiyor; false = gizledi
    // Default: hepsi true. Currency filter ile AND'lenir.
    private readonly Dictionary<string, bool> _userBalancePref = new()
    {
        ["TlBakiye"]  = true,
        ["UsdBakiye"] = true,
        ["EurBakiye"] = true
    };

    /// <summary>
    /// Kullanıcı çıkış istedi. Onay, audit ve pencere kapatma PENCERE
    /// seviyesindedir; ekran yalnızca isteği yayar.
    /// </summary>
    public event Action? LogoutRequested;

    public CashTransactionsScreen(IServiceProvider services)
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

        // Sparkline SkiaSharp ile çizilir ve DynamicResource'u görmez;
        // tema değişince yeniden boyanmalı (yeniden sorgu atmadan).
        ChartPalette.ThemeChanged += _listVm.RebuildSparklines;
        Unloaded += (_, _) => ChartPalette.ThemeChanged -= _listVm.RebuildSparklines;

        Loaded += async (_, _) =>
        {
            // Kabukta kullanıcı adı durum şeridinde, çıkış navigasyon rayında.
            // Gezgin ancak sekme oluşturulurken atandığı için karar Loaded'da
            // veriliyor; kurucuda henüz bilinmiyor.
            if (Navigator is not null)
                SessionBar.Visibility = Visibility.Collapsed;

            ApplyColumnHeaderContextMenu();
            await ApplySavedLayoutAsync();
            ApplyBalanceColumnVisibility();
            await _listVm.LoadTransactionsAsync();
        };
    }

    /// <summary>
    /// Yetkiye bağlı buton görünürlükleri. Pencere menü görünürlüğünü kendisi
    /// yönetir; ekran kendi butonlarını yönetir.
    /// </summary>
    public void RefreshPermissionVisibility(IUserContext userContext)
    {
        // İşlem kopyalama ve toplu içe aktarma — create yetkisi gerekir
        var canCreate = userContext.HasPermission(PermissionType.CanCreateTransaction);
        CopyTransactionButton.Visibility = canCreate ? Visibility.Visible : Visibility.Collapsed;
        CashImportButton.Visibility      = canCreate ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Klavye kısayolu giriş noktaları ───────────────────────────────────────
    //
    // TUŞ ATAMALARI (InputBindings) barındıran PENCEREDEDİR: odak navigasyon
    // rayında ya da menüdeyken de çalışmaları gerekiyor.
    //
    // KOMUTLARIN GÖVDESİ ise burada, ekranın kendi CommandBindings'inde (bkz.
    // XAML). Odak ekranın içindeyse komut zaten buraya kadar yükselir ve
    // pencere hiç devreye girmez; odak dışarıdaysa pencere komutu aktif
    // ekrana yönlendirir. Her iki yol da aşağıdaki TEK gövdeye çıkar.
    //
    // Buton görünürlüğü yetki kapısı olduğu için kısayol da yalnızca buton
    // görünürken çalışır: yetkisiz kullanıcı Ctrl+D ile form açamamalı.

    private void Command_New(object sender, ExecutedRoutedEventArgs e)         => NewTransaction();
    private void Command_Duplicate(object sender, ExecutedRoutedEventArgs e)   => DuplicateTransaction();
    private void Command_Delete(object sender, ExecutedRoutedEventArgs e)      => DeleteSelectedTransaction();
    private void Command_ImportExcel(object sender, ExecutedRoutedEventArgs e) => ImportExcel();
    private void Command_FocusSearch(object sender, ExecutedRoutedEventArgs e) => FocusSearch();
    private void Command_Refresh(object sender, ExecutedRoutedEventArgs e)     => RefreshList();

    /// <summary>Ctrl+N — yetki ve seçim kontrolleri handler içindedir.</summary>
    public void NewTransaction() => NewTransactionButton_Click(this, new RoutedEventArgs());

    /// <summary>Ctrl+D — yalnızca kopyalama butonu görünürken.</summary>
    public void DuplicateTransaction()
    {
        if (CopyTransactionButton.Visibility == Visibility.Visible)
            CopyTransactionButton_Click(this, new RoutedEventArgs());
    }

    /// <summary>Delete — mevcut onay diyaloğu handler içinde, kısayol onu atlamaz.</summary>
    public void DeleteSelectedTransaction() => DeleteTransactionButton_Click(this, new RoutedEventArgs());

    /// <summary>Ctrl+E — yalnızca içe aktarma butonu görünürken.</summary>
    public void ImportExcel()
    {
        if (CashImportButton.Visibility == Visibility.Visible)
            CashImportButton_Click(this, new RoutedEventArgs());
    }

    /// <summary>
    /// F5 — listeyi mevcut filtrelerle yeniden yükler.
    ///
    /// Komut doğrudan ViewModel'e bağlanmıyor: ViewModel Transient kayıtlı,
    /// dolayısıyla barındıran pencerenin kendi örneğini çözüp bağlaması
    /// başka bir listeyi filtrelemek olurdu.
    /// </summary>
    public void RefreshList()
    {
        if (_listVm.FilterCommand.CanExecute(null))
            _listVm.FilterCommand.Execute(null);
    }

    /// <summary>Ctrl+F — açıklama filtresine odaklan.</summary>
    public void FocusSearch()
    {
        DescriptionFilterBox.Focus();
        DescriptionFilterBox.SelectAll();
    }

    // ── Bakiye kartı ──────────────────────────────────────────────────────────

    /// <summary>
    /// Bakiye kartına tıklama → listeyi o para birimine filtreler (Faz C bonus).
    /// Aynı kart yeniden tıklanırsa filtre kaldırılır; kullanıcı kartı geri
    /// almak için filtre panelini aramak zorunda kalmasın.
    /// Filtreleme mevcut ViewModel yolundan geçer — yeni iş mantığı yazılmadı.
    /// </summary>
    private async void BalanceCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string currency }) return;

        _listVm.SelectedCurrencyType =
            _listVm.SelectedCurrencyType == currency ? "Tümü" : currency;

        await _listVm.LoadTransactionsAsync();
    }

    // ── Column Header Context Menu ────────────────────────────────────────────

    private void ApplyColumnHeaderContextMenu()
    {
        var cm = new ContextMenu();
        cm.Opened += ColumnHeaderContextMenu_Opened;

        // BasedOn ZORUNLU: setter'sız bir Style, kontrolü uygulamanın tema
        // stilinden koparıp WPF'in yerleşik (sabit renkli) şablonuna düşürür.
        // Bu satır olmadan koyu temada sütun başlıkları açık gri kalıyordu.
        var themeHeaderStyle = (Style?)TryFindResource(typeof(DataGridColumnHeader));
        var headerStyle      = new Style(typeof(DataGridColumnHeader), themeHeaderStyle);
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
            var json = await layoutService.GetLayoutAsync(userContext.UserId, LayoutScreenKey);
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
            await layoutService.SaveLayoutAsync(userContext.UserId, LayoutScreenKey, json);
            _dialogService.ShowSuccess("Kolon tasarımı kaydedildi.");
        }
        catch (Exception ex)
        {
            // DB yazma hatası — kullanıcıya bildirilir, teşhis için loglanır
            Log.Warning(ex, "Kolon tasarımı kaydedilemedi (ScreenKey={ScreenKey})", LayoutScreenKey);
            _dialogService.ShowError("Kolon tasarımı kaydedilemedi.");
        }
    }

    private async Task ResetGridLayoutAsync()
    {
        try
        {
            var userContext   = _services.GetRequiredService<IUserContext>();
            var layoutService = _services.GetRequiredService<IUserGridLayoutService>();
            await layoutService.DeleteLayoutAsync(userContext.UserId, LayoutScreenKey);

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
        catch (Exception ex)
        {
            // DB silme hatası — kullanıcıya bildirilir, teşhis için loglanır
            Log.Warning(ex, "Kolon tasarımı sıfırlanamadı (ScreenKey={ScreenKey})", LayoutScreenKey);
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
                var hideItem = new MenuItem { Header = "Bu Kolonu Gizle", ToolTip = "Bu kolonu listeden gizler. Sağ tıklayarak tekrar gösterebilirsiniz." };
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

        var saveItem = new MenuItem { Header = "Tasarımı Kaydet", ToolTip = "Mevcut kolon düzenini (sıra, genişlik, görünürlük) kaydeder." };
        saveItem.Click += async (_, _) => await SaveGridLayoutAsync();
        cm.Items.Add(saveItem);

        var resetItem = new MenuItem { Header = "Varsayılan Tasarıma Dön", ToolTip = "Kolon düzenini fabrika varsayılanına sıfırlar." };
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

    // ── İşlem Butonları ───────────────────────────────────────────────────────
    //
    // Alt pencereler Owner olarak barındıran PENCEREYİ alır: UserControl bir
    // Window değildir, Owner ona atanamaz. Window.GetWindow(this) ekranın
    // hangi kabukta barındığını sorar — MainWindow bugün, ShellWindow yarın.

    private Window? HostWindow => Window.GetWindow(this);

    private async void NewTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new CashTransactionFormWindow(_services) { Owner = HostWindow };
        if (form.ShowDialog() == true)
            await _listVm.LoadTransactionsAsync();
    }

    private async void CashImportButton_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new CashImportWindow(_services) { Owner = HostWindow };
        wizard.ShowDialog();
        // X ile kapatılsa bile içe aktarma yapıldıysa liste + bakiye barı yenilenir
        if (wizard.ImportCompleted)
            await _listVm.LoadTransactionsAsync();
    }

    private async void EditTransactionButton_Click(object sender, RoutedEventArgs e)
        => await OpenEditTransactionAsync();

    private async void TransactionDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Sadece satır üzerinde çift tıklamada aç; başlık tıklamasında DataGridRow yoktur
        if (e.OriginalSource is DependencyObject src &&
            ItemsControl.ContainerFromElement(TransactionDataGrid, src) is DataGridRow)
        {
            await OpenEditTransactionAsync();
        }
    }

    private async Task OpenEditTransactionAsync()
    {
        var userContext = _services.GetRequiredService<IUserContext>();
        if (!userContext.HasPermission(PermissionType.CanEditTransaction))
        {
            _dialogService.ShowWarning("Bu işlemi düzenlemek için yetkiniz bulunmamaktadır.", "Yetki Gerekli");
            return;
        }

        var selected = _listVm.SelectedTransaction;
        if (selected is null) return;

        var form = new CashTransactionFormWindow(_services) { Owner = HostWindow };
        form.InitializeForEdit(selected);
        if (form.ShowDialog() == true)
            await _listVm.LoadTransactionsAsync();
    }

    private async void CopyTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _listVm.SelectedTransaction;
        if (selected is null)
        {
            _dialogService.ShowWarning("Kopyalamak için listeden bir işlem seçin.", "İşlem Seç");
            return;
        }

        var form = new CashTransactionFormWindow(_services) { Owner = HostWindow };
        form.InitializeForCopy(selected);
        if (form.ShowDialog() == true)
        {
            _dialogService.ShowSuccess("İşlem kopyalanarak yeni kayıt oluşturuldu.", "Kopyalama Başarılı");
            await _listVm.LoadTransactionsAsync();
        }
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

        await _listVm.LoadTransactionsAsync();
    }

    /// <summary>
    /// Çıkış butonu yalnızca isteği yayar. Onay diyaloğu, audit kaydı ve
    /// pencerenin kapatılması PENCERE seviyesindedir (bkz. MainWindow).
    /// </summary>
    private void Logout_Click(object sender, RoutedEventArgs e) => LogoutRequested?.Invoke();
}

internal sealed record GridColumnState(
    string Key,
    bool   IsVisible,
    int    DisplayIndex,
    double Width);
