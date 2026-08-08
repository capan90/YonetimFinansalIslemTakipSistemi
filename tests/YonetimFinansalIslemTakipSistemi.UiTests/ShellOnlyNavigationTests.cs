using System.Text;
using System.Text.RegularExpressions;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// TEK GEZİNME MODELİ (Faz F1).
///
/// Uygulama Faz D'de kabuğa taşındı ama eski pencere yolu geri dönüş için
/// bırakıldı; Faz E'de donduruldu, Faz F1'de kaldırıldı. Artık aktif
/// uygulamada ekranlara ulaşmanın TEK yolu kabuk sekmeleridir.
///
/// NEDEN TEST GEREKİYOR: "sildik" bir kereye mahsus bir olaydır; ikinci
/// gezinme modeli sessizce geri gelebilir. Birinin bir liste ekranını
/// <c>new …Window(...).ShowDialog()</c> ile açması yeter — derlenir, çalışır
/// ve kabuk o ekranın açık olduğunu bilmez: sekme sayılmaz, kapatma
/// sözleşmesi işlemez, yetki kapısı ShellViewModel.Resolve'dan geçmez.
///
/// Bu testler o kapıyı kapalı tutar.
/// </summary>
public class ShellOnlyNavigationTests
{
    /// <summary>
    /// Pencere olarak açılmaya DEVAM EDEN tipler ve nedenleri.
    ///
    /// Ortak nitelikleri: hepsi bir işi bitirene kadar süren MODAL adımlardır
    /// (form, sihirbaz, önizleme, ayar) — kabukta sekme olmaları anlamsız
    /// olurdu. Ekran değiller; ekranların açtığı diyaloglar.
    ///
    /// Bu listeye yeni bir ad eklemek bilinçli bir karardır: "ekran mı,
    /// diyalog mu?" sorusu burada cevaplanır.
    /// </summary>
    private static readonly Dictionary<string, string> AllowedWindows = new(StringComparer.Ordinal)
    {
        ["LoginWindow"]                    = "Oturum başlamadan kabuk yok",
        ["ShellWindow"]                    = "Kabuğun kendisi",

        ["CashTransactionFormWindow"]      = "Kayıt formu (modal)",
        ["UserFormWindow"]                 = "Kayıt formu (modal)",
        ["CargoShipmentEditWindow"]        = "Kayıt formu (modal)",
        ["CargoCompanyEditWindow"]         = "Kayıt formu (modal)",
        ["CompanyDirectoryEditWindow"]     = "Kayıt formu (modal)",
        ["MailContactEditWindow"]          = "Kayıt formu (modal)",
        ["WhatsAppContactEditWindow"]      = "Kayıt formu (modal)",

        ["CashImportWindow"]               = "İçe aktarma sihirbazı",
        ["CargoImportWindow"]              = "İçe aktarma sihirbazı",
        ["DirectoryImportWindow"]          = "İçe aktarma sihirbazı",
        ["WhatsAppImportWindow"]           = "İçe aktarma sihirbazı",

        ["ReportPreviewWindow"]            = "Önizleme",
        ["CargoNotificationPreviewWindow"] = "Önizleme",
        ["SystemLogDetailWindow"]          = "Detay görüntüleyici",
        ["MailContactPickerWindow"]        = "Seçici",

        ["MailSettingsWindow"]             = "Ayar diyaloğu",
        ["TextCaseSettingsWindow"]         = "Ayar diyaloğu",
        ["AppearanceSettingsWindow"]       = "Ayar diyaloğu",
    };

    private static IEnumerable<string> WindowFiles() =>
        Directory.EnumerateFiles(UiSourceLocator.UiProjectDirectory, "*Window.xaml", SearchOption.AllDirectories)
                 .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                 .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    /// <summary>
    /// BEKÇİ. Yeni bir pencere eklenirse burada gerekçesiyle birlikte
    /// listelenmeli. Liste dışı bir pencere, ekranın sekme yerine pencere
    /// olarak açıldığı anlamına gelir.
    /// </summary>
    [Fact]
    public void Pencere_olarak_kalan_tipler_yalnizca_diyaloglar()
    {
        var unexpected = WindowFiles()
            .Select(f => Path.GetFileNameWithoutExtension(f)!)
            .Where(name => !AllowedWindows.ContainsKey(name))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(unexpected.Count == 0,
            "Beklenmeyen pencere (ekranlar sekme olmalı): " + string.Join(", ", unexpected));
    }

    /// <summary>
    /// Listedeki her ad gerçekten var olmalı — silinen bir pencere listede
    /// kalırsa liste zamanla gerçekle ilgisini kaybeder.
    /// </summary>
    [Fact]
    public void Izin_listesi_gercek_dosyalarla_ortusuyor()
    {
        var actual = WindowFiles()
            .Select(f => Path.GetFileNameWithoutExtension(f)!)
            .ToHashSet(StringComparer.Ordinal);

        var stale = AllowedWindows.Keys.Where(k => !actual.Contains(k))
                                       .OrderBy(k => k, StringComparer.Ordinal)
                                       .ToList();

        Assert.True(stale.Count == 0,
            "İzin listesinde olup dosyası olmayan: " + string.Join(", ", stale));
    }

    /// <summary>
    /// Ekranlar başka EKRANA pencere açarak gezinmemeli. Gezinme isteği
    /// IShellNavigator üzerinden gider; orada yetki ve tekillik kontrolü var.
    /// Pencere açan bir ekran o kapıyı atlar.
    /// </summary>
    [Fact]
    public void Ekranlar_gezinmek_icin_pencere_acmiyor()
    {
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(
                     UiSourceLocator.UiProjectDirectory, "*Screen.xaml.cs", SearchOption.AllDirectories))
        {
            var code = File.ReadAllText(file, Encoding.UTF8);
            code = Regex.Replace(code, @"//[^\r\n]*", string.Empty);
            code = Regex.Replace(code, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

            foreach (Match m in Regex.Matches(code, @"new\s+(?:\w+\.)*(?<w>\w+Window)\s*\("))
            {
                var window = m.Groups["w"].Value;

                // Diyalog açmak serbest; EKRAN açmak değil.
                if (AllowedWindows.ContainsKey(window)) continue;

                violations.Add($"{Path.GetFileName(file)} → {window}");
            }
        }

        Assert.True(violations.Count == 0,
            "Ekran, gezinmek için pencere açıyor:\n  " + string.Join("\n  ", violations));
    }

    /// <summary>
    /// Ekranların KENDİ navigasyon şeridi olmamalı. Kargo panosunun şeridi
    /// (Gelen/Giden/Rehber düğmeleri + yardım menüsü + çıkış) ikinci bir
    /// kabuktu; kaldırıldı. Geri gelirse aynı kurallar iki yerde tutulur.
    /// </summary>
    [Fact]
    public void Ekranlarin_kendi_navigasyon_seridi_yok()
    {
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(
                     UiSourceLocator.UiProjectDirectory, "*Screen.xaml", SearchOption.AllDirectories))
        {
            var markup = Regex.Replace(
                File.ReadAllText(file, Encoding.UTF8), @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

            if (Regex.IsMatch(markup, @"x:Name=""NavBar"""))
                violations.Add($"{Path.GetFileName(file)}: NavBar");

            if (markup.Contains("Çıkış Yap", StringComparison.Ordinal))
                violations.Add($"{Path.GetFileName(file)}: kendi çıkış düğmesi");
        }

        Assert.True(violations.Count == 0,
            "Ekranda kabuğa ait öğe:\n  " + string.Join("\n  ", violations));
    }

    /// <summary>
    /// Gezinme sözleşmesinin tek uygulayıcısı kabuktur. İkinci bir uygulama,
    /// ikinci bir yetki kapısı demektir.
    /// </summary>
    [Fact]
    public void IShellNavigator_yalnizca_kabukta_uygulaniyor()
    {
        var implementers = Directory
            .EnumerateFiles(UiSourceLocator.UiProjectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f =>
            {
                var code = File.ReadAllText(f, Encoding.UTF8);
                return Regex.IsMatch(code, @"class\s+\w+\s*:[^{]*\bIShellNavigator\b");
            })
            .Select(Path.GetFileName)
            .ToList();

        Assert.Equal(["ShellViewModel.cs"], implementers);
    }

    /// <summary>
    /// Açılışta yalnızca kabuk penceresi kurulur. Login dışında pencere
    /// örnekleyen bir başlangıç dalı kalmamalı.
    /// </summary>
    [Fact]
    public void Baslangic_yalnizca_kabugu_aciyor()
    {
        var app = File.ReadAllText(
            Path.Combine(UiSourceLocator.UiProjectDirectory, "App.xaml.cs"), Encoding.UTF8);

        app = Regex.Replace(app, @"//[^\r\n]*", string.Empty);

        var opened = Regex.Matches(app, @"new\s+(?:\w+\.)*(?<w>\w+Window)\s*\(")
            .Select(m => m.Groups["w"].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(w => w, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["LoginWindow", "ShellWindow"], opened);
    }
}
