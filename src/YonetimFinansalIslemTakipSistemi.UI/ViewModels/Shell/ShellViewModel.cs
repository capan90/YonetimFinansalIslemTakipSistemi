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
public sealed class ShellViewModel : INotifyPropertyChanged, IShellNavigator
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

        // Gruplu görünüm: menü çubuğunun yerini alan başlıklar (Finans, Kargo
        // Takip, Yönetim, Ayarlar). Grup sırası kayıt tablosundaki İLK
        // görünme sırasıdır — alfabetik değil; kullanıcının menüde alıştığı
        // sıra korunuyor. Tamamen boşalan grup hiç görünmez.
        NavigationGroups = new ObservableCollection<ScreenGroup>(
            NavigationItems
                .GroupBy(s => s.NavGroup)
                .Select(g => new ScreenGroup(g.Key, g.ToList())));

        OpenScreenCommand = new RelayCommand(
            () => { if (SelectedNavigationItem is not null) OpenScreen(SelectedNavigationItem.Key); });

        CloseTabCommand = new RelayCommand(
            () => { if (ActiveTab is not null) CloseTab(ActiveTab); });

        Palette            = new CommandPaletteViewModel(NavigationItems);
        OpenPaletteCommand = new RelayCommand(OpenPalette);

        NextTabCommand     = new RelayCommand(ActivateNextTab);
        PreviousTabCommand = new RelayCommand(ActivatePreviousTab);

        // Sıra XAML'den metin olarak gelir (CommandParameter="1"); çözülemeyen
        // değer yok sayılır — kısayol hiçbir şey yapmaz, hata üretmez.
        ActivateTabCommand = new RelayCommand(parameter =>
        {
            if (int.TryParse(parameter?.ToString(), out var position))
                ActivateTabAt(position);
        });

        // RequestLogout bool döner (çıkış iptal edilebilir); RelayCommand Action ister
        LogoutCommand = new RelayCommand(() => RequestLogout());
    }

    // ── Navigasyon ────────────────────────────────────────────────────────

    /// <summary>Kullanıcının görebildiği ekranlar. Yetkisiz olanlar hiç girmez.</summary>
    public ObservableCollection<ScreenDefinition> NavigationItems { get; }

    /// <summary>
    /// Aynı ekranların başlık altında gruplanmış hâli — navigasyon rayı bunu
    /// gösterir. <see cref="NavigationItems"/> düz liste olarak kalır; yetki
    /// ve tekillik testleri onun üzerinden yürüyor.
    /// </summary>
    public ObservableCollection<ScreenGroup> NavigationGroups { get; }

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

        Attach(tab);
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

        Attach(tab);
        Tabs.Add(tab);
        ActiveTab = tab;
        return tab;
    }

    // ── Ekran ↔ kabuk bağlantısı ──────────────────────────────────────────
    //
    // Kabuk ekranların TÜRÜNÜ bilmez; yalnızca uyguladıkları sözleşmeyi
    // dinler. Bağlanan her şey Detach'te sökülür — kapanan sekme kabukta
    // referansla tutulmamalı (bkz. Faz F4 bellek ölçümü).

    private void Attach(ShellTab tab)
    {
        // Başka ekran açması gereken ekranlara gezgin verilir.
        if (tab.View is IShellNavigationAware aware)
            aware.Navigator = this;

        // "Kapat" düğmesi/Esc: ekran yalnızca haber verir, hangi sekme
        // olduğunu bilmez; kapanış eylemi burada sekmeye bağlanır.
        if (tab.View is IShellCloseSource closable)
        {
            tab.CloseHandler = () => CloseTab(tab);
            closable.CloseRequested += tab.CloseHandler;
        }
    }

    private void Detach(ShellTab tab)
    {
        if (tab.View is IShellNavigationAware aware)
            aware.Navigator = null;

        if (tab.View is IShellCloseSource closable && tab.CloseHandler is not null)
        {
            closable.CloseRequested -= tab.CloseHandler;
            tab.CloseHandler = null;
        }
    }


    // ── IShellNavigator ───────────────────────────────────────────────────
    //
    // Ekranlar yalnızca "şu ekranı aç" diyebilir. Yetki ve tekillik kontrolü
    // aşağıdaki OpenScreen'lerin içinde — ekran kendi başına kapı açamaz.

    bool IShellNavigator.OpenScreen(ScreenKey key) => OpenScreen(key) is not null;

    bool IShellNavigator.OpenScreen(ScreenKey key, object parameter)
        => OpenScreen(key, parameter) is not null;

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

        Detach(tab);
        Tabs.Remove(tab);

        // Kapatılan sekme aktifse odak komşuya geçer; kabuk boş sekmeyle kalmaz
        if (ReferenceEquals(ActiveTab, tab))
            ActiveTab = Tabs.Count == 0 ? null : Tabs[Math.Min(index, Tabs.Count - 1)];

        return true;
    }

    // ── Komut paleti (Faz E6) ─────────────────────────────────────────────

    /// <summary>
    /// Ctrl+K paleti. Listesi <see cref="NavigationItems"/>'dan gelir, yani
    /// kullanıcının zaten görebildiği ekranlardan — palet yeni bir kapı
    /// açmaz, var olana kısa yoldur.
    /// </summary>
    public CommandPaletteViewModel Palette { get; }

    private bool _isPaletteOpen;
    public bool IsPaletteOpen
    {
        get => _isPaletteOpen;
        private set { _isPaletteOpen = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Paleti açar. Sorgu her açılışta SIFIRLANIR: palet bir "kaldığın yerden
    /// devam" aracı değil, her seferinde baştan arama.
    /// </summary>
    public void OpenPalette()
    {
        Palette.Query = string.Empty;
        IsPaletteOpen = true;
    }

    public void ClosePalette() => IsPaletteOpen = false;

    /// <summary>
    /// Paletteki seçimi açar ve paleti kapatır.
    /// </summary>
    /// <returns>Bir ekran açıldıysa <c>true</c>.</returns>
    public bool AcceptPalette()
    {
        var screen = Palette.Selected;
        if (screen is null) return false;

        // Yetki kontrolü yine OpenScreen'in içinde — palet onu atlamaz.
        var opened = OpenScreen(screen.Key) is not null;

        ClosePalette();
        return opened;
    }

    // ── Toplu kapatma (Faz E4) ────────────────────────────────────────────
    //
    // Üçü de tek tek CloseTab'dan geçer: CanClose ve RequestClose kontrolü
    // TEKRARLANMAZ. Kapanmayı reddeden bir ekran diğerlerini durdurmaz —
    // kullanıcı "diğerlerini kapat" dediyse kapanabilenler kapanmalı; tek bir
    // itiraz yüzünden hiçbir şey olmaması sessiz bir başarısızlık olurdu.
    //
    // CloseAllTabs (çıkış akışı) ile karıştırılmamalı: o CanClose'u BİLEREK
    // yok sayar, çünkü çıkışta kabuk tamamen boşalmalıdır.

    /// <summary>Verilen sekme dışındaki tüm sekmeleri kapatır.</summary>
    /// <returns>Kapatılan sekme sayısı.</returns>
    public int CloseOtherTabs(ShellTab keep)
    {
        ArgumentNullException.ThrowIfNull(keep);

        return CloseEach(Tabs.Where(t => !ReferenceEquals(t, keep)));
    }

    /// <summary>Verilen sekmenin SAĞINDAKİ sekmeleri kapatır.</summary>
    /// <returns>Kapatılan sekme sayısı.</returns>
    public int CloseTabsToTheRight(ShellTab from)
    {
        ArgumentNullException.ThrowIfNull(from);

        var index = Tabs.IndexOf(from);
        if (index < 0) return 0;

        return CloseEach(Tabs.Skip(index + 1));
    }

    /// <summary>
    /// Kullanıcının "tümünü kapat" isteği. Kapatılamaz sekme (Nakit İşlemler)
    /// açık kalır — çıkıştaki CloseAllTabs'tan farkı budur.
    /// </summary>
    /// <returns>Kapatılan sekme sayısı.</returns>
    public int CloseClosableTabs() => CloseEach(Tabs);

    private int CloseEach(IEnumerable<ShellTab> tabs)
    {
        // Anlık görüntü: CloseTab koleksiyonu değiştiriyor.
        var closed = 0;

        foreach (var tab in tabs.ToList())
            if (CloseTab(tab)) closed++;

        return closed;
    }

    // ── Sekmeler arası gezinme (Faz E5) ───────────────────────────────────

    /// <summary>
    /// Sonraki sekmeye geçer; sondaysa başa döner. Tek sekmede hiçbir şey
    /// yapmaz.
    /// </summary>
    public void ActivateNextTab() => ActivateRelative(+1);

    /// <summary>Önceki sekmeye geçer; baştaysa sona döner.</summary>
    public void ActivatePreviousTab() => ActivateRelative(-1);

    private void ActivateRelative(int step)
    {
        if (Tabs.Count == 0) return;

        var current = ActiveTab is null ? -1 : Tabs.IndexOf(ActiveTab);
        if (current < 0) { ActiveTab = Tabs[0]; return; }

        // Negatif mod C#'ta negatif kalır; Count eklenip tekrar mod alınıyor.
        var next = ((current + step) % Tabs.Count + Tabs.Count) % Tabs.Count;
        ActiveTab = Tabs[next];
    }

    /// <summary>
    /// Sıradaki sekmeyi etkinleştirir (Ctrl+1..9). Aralık dışı istek yok
    /// sayılır — kullanıcı olmayan bir sekmeye basınca hiçbir şey olmamalı.
    /// </summary>
    /// <param name="position">1 tabanlı sekme sırası.</param>
    public void ActivateTabAt(int position)
    {
        if (position < 1 || position > Tabs.Count) return;

        ActiveTab = Tabs[position - 1];
    }

    // ── Çıkış ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Kullanıcı çıkış istedi. Kabuk penceresi bunu dinleyip mevcut
    /// IsLogoutRequested sözleşmesine çevirir.
    /// </summary>
    public event Action? LogoutRequested;

    /// <summary>
    /// Çıkış onayı. Kabuk penceresi atar — diyalog gösterimi pencerenin işi,
    /// ViewModel IDialogService bilmez.
    ///
    /// Atanmazsa onay sorulmaz; kabuk mantığı diyalogsuz da sınanabilir.
    /// </summary>
    public Func<bool>? ConfirmLogout { get; set; }

    /// <summary>
    /// Çıkış akışı: önce kullanıcı onayı, sonra açık ekranların sözü, en son
    /// çıkış isteğinin yayılması.
    ///
    /// SIRA ÖNEMLİ. Onay sekmeler kapatılmadan ÖNCE sorulur: kullanıcı
    /// vazgeçtiğinde kabuk boş sekme listesiyle kalmamalı. Bir ekran
    /// kaydedilmemiş değişiklik yüzünden kapanmayı reddederse çıkış İPTAL
    /// edilir — kullanıcı verisi sessizce kaybolmaz.
    /// </summary>
    /// <returns>Çıkış isteği yayıldıysa <c>true</c>.</returns>
    public bool RequestLogout()
    {
        if (ConfirmLogout is not null && !ConfirmLogout()) return false;

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

            Detach(tab);
            Tabs.Remove(tab);
        }

        ActiveTab = null;
        return true;
    }

    // ── Komutlar ──────────────────────────────────────────────────────────

    public ICommand OpenScreenCommand  { get; }
    public ICommand CloseTabCommand    { get; }
    public ICommand LogoutCommand      { get; }
    public ICommand NextTabCommand     { get; }
    public ICommand PreviousTabCommand { get; }
    public ICommand ActivateTabCommand { get; }
    public ICommand OpenPaletteCommand { get; }

    // ── INotifyPropertyChanged ────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
