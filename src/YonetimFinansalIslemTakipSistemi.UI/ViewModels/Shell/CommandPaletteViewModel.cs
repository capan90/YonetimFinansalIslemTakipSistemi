using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;

/// <summary>
/// Komut paleti (Ctrl+K) — klavyeyle ekran arama ve açma.
///
/// NEDEN VAR: navigasyon rayı 18 ekranı dört grupta taşıyor. Bilinen bir
/// ekrana gitmek için doğru grubu bulup listede aramak gerekiyor; palet aynı
/// işi adını yazarak yaptırır.
///
/// YETKİ BURADA YOK. Paletin listesi kabuğun GÖRÜNÜR ekran listesinden gelir
/// ve açma isteği yine <see cref="ShellViewModel.OpenScreen(ScreenKey)"/>
/// üzerinden geçer — orada yetki yeniden kontrol edilir. Palet yeni bir kapı
/// açmaz; var olan kapıya kısa yoldur.
/// </summary>
public sealed class CommandPaletteViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// tr-TR duyarsız arama. OrdinalIgnoreCase "İŞLEM" ile "işlem"i farklı
    /// sayar (I↔ı eşleşmez) ve kullanıcı aradığını bulamazdı — projede aynı
    /// tercih içe aktarma eşlemelerinde de yapılmıştı.
    /// </summary>
    private static readonly CompareInfo Tr = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    private readonly IReadOnlyList<ScreenDefinition> _screens;

    public CommandPaletteViewModel(IReadOnlyList<ScreenDefinition> screens)
    {
        _screens = screens ?? throw new ArgumentNullException(nameof(screens));
        Results  = new ObservableCollection<ScreenDefinition>();

        Refresh();
    }

    /// <summary>Eşleşen ekranlar — sıralama alaka düzeyine göre.</summary>
    public ObservableCollection<ScreenDefinition> Results { get; }

    private string _query = string.Empty;
    public string Query
    {
        get => _query;
        set
        {
            if (_query == value) return;

            _query = value ?? string.Empty;
            OnPropertyChanged();
            Refresh();
        }
    }

    private int _selectedIndex;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set { _selectedIndex = value; OnPropertyChanged(); }
    }

    /// <summary>Şu an vurgulanan ekran; sonuç yoksa <c>null</c>.</summary>
    public ScreenDefinition? Selected =>
        SelectedIndex >= 0 && SelectedIndex < Results.Count ? Results[SelectedIndex] : null;

    /// <summary>Vurguyu bir alta taşır; sondaysa başa döner.</summary>
    public void MoveNext() => Move(+1);

    /// <summary>Vurguyu bir üste taşır; baştaysa sona döner.</summary>
    public void MovePrevious() => Move(-1);

    private void Move(int step)
    {
        if (Results.Count == 0) { SelectedIndex = 0; return; }

        SelectedIndex = ((SelectedIndex + step) % Results.Count + Results.Count) % Results.Count;
    }

    /// <summary>
    /// Sonuç listesini yeniden kurar.
    ///
    /// SIRALAMA alakaya göre: başlığı sorguyla BAŞLAYAN ekranlar önce gelir.
    /// "Kargo" yazan kullanıcı "Kargo Dashboard"ı, "Gelen Kargolar"dan önce
    /// görmeli — aradığı şey genelde adı o kelimeyle başlayandır.
    ///
    /// Grup adı da aranır: "finans" yazınca Finans grubundaki ekranlar gelir.
    /// </summary>
    private void Refresh()
    {
        var query = _query.Trim();

        Results.Clear();

        IEnumerable<ScreenDefinition> matches = query.Length == 0
            ? _screens
            : _screens.Where(s => Contains(s.Title, query) || Contains(s.NavGroup, query))
                      .OrderByDescending(s => StartsWith(s.Title, query));

        foreach (var screen in matches)
            Results.Add(screen);

        // Sorgu her değiştiğinde vurgu başa döner: kullanıcı yazmaya devam
        // ederken listedeki konum kaymamalı.
        SelectedIndex = 0;
    }

    private static bool Contains(string text, string query) =>
        !string.IsNullOrEmpty(text) && Tr.IndexOf(text, query, CompareOptions.IgnoreCase) >= 0;

    private static bool StartsWith(string text, string query) =>
        !string.IsNullOrEmpty(text) && Tr.IsPrefix(text, query, CompareOptions.IgnoreCase);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
