using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Common;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;

/// <summary>
/// Tek kabuk mimarisinin sekme ve navigasyon yönetimi.
///
/// SORUMLULUĞU: hangi ekranların görünebileceği (yetki), hangilerinin açık
/// olduğu (sekmeler) ve çıkış isteğinin dışarı taşınması. Ekranların İÇ
/// mantığına karışmaz — onlar kendi ViewModel'lerini kullanmaya devam eder.
///
/// ÇIKIŞ SÖZLEŞMESİ: Mevcut MainWindow / CargoDashboardWindow deseni korunur.
/// Kabuk penceresi <see cref="LogoutRequested"/> olayını dinleyip
/// <c>IsLogoutRequested = true; Close();</c> yapar. App.xaml.cs'teki
/// login → kabuk → logout → login döngüsü aynen çalışır; kabuk o döngüye
/// yeni bir kavram sokmaz.
/// </summary>
public sealed class ShellViewModel : INotifyPropertyChanged
{
    private readonly IServiceProvider                 _services;
    private readonly IUserContext                     _userContext;
    private readonly IReadOnlyList<ScreenDefinition>  _screens;

    /// <summary>
    /// Üretimde <see cref="ScreenRegistry.All"/> geçilir. Ekran listesi dışarıdan
    /// alınır ki testler kendi tanımlarını verebilsin — kabuk mantığı gerçek
    /// ekranlara bağımlı olmadan sınanabiliyor.
    /// </summary>
    public ShellViewModel(
        IServiceProvider                services,
        IUserContext                    userContext,
        IReadOnlyList<ScreenDefinition> screens)
    {
        _services    = services;
        _userContext = userContext;
        _screens     = screens;

        // Navigasyon rayı: yalnızca yetkisi olan VE taşınmış ekranlar.
        // Taşınmamış ekran gösterilseydi tıklandığında hiçbir şey olmazdı.
        NavigationItems = new ObservableCollection<ScreenDefinition>(
            _screens.Where(IsVisible));

        OpenScreenCommand = new RelayCommand(
            () => { if (SelectedNavigationItem is not null) OpenScreen(SelectedNavigationItem.Key); });

        CloseTabCommand = new RelayCommand(
            () => { if (ActiveTab is not null) CloseTab(ActiveTab); });

        // RequestLogout bool döner (çıkış iptal edilebilir); RelayCommand Action ister
        LogoutCommand = new RelayCommand(() => RequestLogout());
    }

    // ── Navigasyon ────────────────────────────────────────────────────────

    /// <summary>Kullanıcının görebildiği ekranlar. Yetkisiz olanlar hiç girmez.</summary>
    public ObservableCollection<ScreenDefinition> NavigationItems { get; }

    private ScreenDefinition? _selectedNavigationItem;
    public ScreenDefinition? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set { _selectedNavigationItem = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Ekran navigasyon rayında görünür mü.
    ///
    /// Parametreli ekranlar rayda YER ALMAZ: bir kayıt üzerinde çalışırlar ve
    /// raydan tıklanınca hangi kaydı açacakları belli değildir. Onlar kendi
    /// liste ekranlarından açılır (ör. kargo listesindeki "Operasyon" butonu).
    /// </summary>
    private bool IsVisible(ScreenDefinition screen) =>
        screen.IsMigrated && !screen.IsParameterized && HasPermission(screen);

    private bool HasPermission(ScreenDefinition screen) =>
        screen.IsAllowedFor(_userContext.HasPermission);

    // ── Sekmeler ──────────────────────────────────────────────────────────

    public ObservableCollection<ShellTab> Tabs { get; } = new();

    private ShellTab? _activeTab;
    public ShellTab? ActiveTab
    {
        get => _activeTab;
        set { _activeTab = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Ekranı açar; zaten açıksa YENİ SEKME OLUŞTURMAZ, mevcut sekmeye odaklanır.
    /// </summary>
    /// <returns>
    /// Açılan veya odaklanılan sekme. Ekran yoksa, yetki yoksa ya da henüz
    /// taşınmamışsa <c>null</c>.
    /// </returns>
    public ShellTab? OpenScreen(ScreenKey key)
    {
        var definition = Resolve(key);
        if (definition is null) return null;

        // Parametreli ekran parametresiz açılamaz — çağıran taraftaki hata.
        if (definition.IsParameterized) return null;

        // Zaten açık mı — tekillik kontrolü sekme üretiminden ÖNCE
        var existing = Find(key, instanceKey: null);
        if (existing is not null)
        {
            ActiveTab = existing;
            return existing;
        }

        var tab = new ShellTab(definition, definition.CreateView!(_services));

        Tabs.Add(tab);
        ActiveTab = tab;
        return tab;
    }

    /// <summary>
    /// Bir KAYIT üzerinde çalışan ekranı açar (ör. Kargo Operasyon Merkezi).
    ///
    /// Aynı ekran türü altında farklı kayıtlar ayrı sekmelerde açılır; aynı
    /// kayıt ikinci kez açılırsa mevcut sekmeye odaklanılır.
    /// </summary>
    /// <param name="key">Ekran kimliği.</param>
    /// <param name="parameter">Ekranın üzerinde çalışacağı kayıt.</param>
    public ShellTab? OpenScreen(ScreenKey key, object parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        var definition = Resolve(key);
        if (definition is null) return null;

        // Tekil ekran parametre kabul etmez — yine çağıran taraftaki hata.
        if (!definition.IsParameterized) return null;

        // Örnek burada üretilir çünkü kimliği (InstanceKey) parametreden çıkar;
        // üretmeden hangi sekmeye denk geldiğini bilemeyiz.
        var instance = definition.CreateInstance!(_services, parameter);

        var existing = Find(key, instance.InstanceKey);
        if (existing is not null)
        {
            ActiveTab = existing;
            return existing;
        }

        var tab = new ShellTab(definition, instance.View, instance.InstanceKey, instance.Title);

        Tabs.Add(tab);
        ActiveTab = tab;
        return tab;
    }

    /// <summary>
    /// Ekranı kayıt tablosunda bulur ve açılabilirliğini doğrular.
    ///
    /// Yetki kontrolü burada yapılır: navigasyonda gizlemek YETMEZ, ekran
    /// programatik olarak da (kısayol, komut paleti, kod) açılamamalı.
    /// </summary>
    private ScreenDefinition? Resolve(ScreenKey key)
    {
        var definition = _screens.FirstOrDefault(s => s.Key == key);

        if (definition is null)            return null;
        if (!HasPermission(definition))    return null;
        if (!definition.IsMigrated)        return null;

        return definition;
    }

    private ShellTab? Find(ScreenKey key, string? instanceKey)
        => Tabs.FirstOrDefault(t => t.Matches(key, instanceKey));

    /// <summary>
    /// Sekmeyi kapatır. Kapatılamaz sekmeler ve kaydedilmemiş değişikliği olan
    /// ekranlar reddedebilir.
    /// </summary>
    /// <returns>Kapatıldıysa <c>true</c>.</returns>
    public bool CloseTab(ShellTab tab)
    {
        if (!tab.CanClose)   return false;
        if (!tab.RequestClose()) return false;

        var index = Tabs.IndexOf(tab);
        if (index < 0) return false;

        Tabs.Remove(tab);

        // Kapatılan sekme aktifse odak komşuya geçer; kabuk boş sekmeyle kalmaz
        if (ReferenceEquals(ActiveTab, tab))
            ActiveTab = Tabs.Count == 0 ? null : Tabs[Math.Min(index, Tabs.Count - 1)];

        return true;
    }

    // ── Çıkış ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Kullanıcı çıkış istedi. Kabuk penceresi bunu dinleyip mevcut
    /// IsLogoutRequested sözleşmesine çevirir.
    /// </summary>
    public event Action? LogoutRequested;

    /// <summary>
    /// Tüm sekmeleri kapatmayı dener, hepsi kapanırsa çıkış isteğini yayar.
    ///
    /// Bir ekran kaydedilmemiş değişiklik yüzünden kapanmayı reddederse çıkış
    /// İPTAL edilir — kullanıcı verisi sessizce kaybolmaz.
    /// </summary>
    /// <returns>Çıkış isteği yayıldıysa <c>true</c>.</returns>
    public bool RequestLogout()
    {
        if (!CloseAllTabs()) return false;

        LogoutRequested?.Invoke();
        return true;
    }

    /// <summary>
    /// Tüm sekmeleri kapatır. Biri reddederse durur ve <c>false</c> döner;
    /// o ana kadar kapananlar kapalı kalır, reddeden sekme aktif yapılır.
    /// </summary>
    public bool CloseAllTabs()
    {
        // CanClose=false olan sekmeler de kapatılır: logout'ta kabuk tamamen
        // boşalmalı. CanClose yalnızca KULLANICININ kapatmasını engeller.
        foreach (var tab in Tabs.ToList())
        {
            if (!tab.RequestClose())
            {
                ActiveTab = tab;
                return false;
            }

            Tabs.Remove(tab);
        }

        ActiveTab = null;
        return true;
    }

    // ── Komutlar ──────────────────────────────────────────────────────────

    public ICommand OpenScreenCommand { get; }
    public ICommand CloseTabCommand   { get; }
    public ICommand LogoutCommand     { get; }

    // ── INotifyPropertyChanged ────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
