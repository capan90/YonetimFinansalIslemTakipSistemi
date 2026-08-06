using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Kaynak XAML'i <see cref="XamlReader"/>'ın kabul edeceği hâle getirir.
///
/// Pencerelerin kurucuları DI bağımlılıkları alır, bu yüzden canlı örnek
/// üretilemez. Bunun yerine kaynak metinden yalnızca KOD ARKASI BAĞLANTILARI
/// çıkarılır (x:Class, olay adları, Window.Icon). Kaynak referansları,
/// şablonlar, stiller ve bağlamalar olduğu gibi kalır — test edilen şey onlar.
/// </summary>
public static class XamlSanitizer
{
    private const string UiAssembly = "YonetimFinansalIslemTakipSistemi.UI";

    /// <summary>Ayrıştırır; başarısız olursa <c>null</c> döner (hata raporlaması XamlLoadTests'in işi).</summary>
    public static FrameworkElement? TryParse(string xamlPath)
    {
        try   { return XamlReader.Parse(Sanitize(xamlPath)) as FrameworkElement; }
        catch { return null; }
    }

    /// <summary>Ayrıştırır; başarısız olursa istisnayı yukarı verir.</summary>
    public static object Parse(string xamlPath) => XamlReader.Parse(Sanitize(xamlPath));

    public static string Sanitize(string xamlPath)
    {
        var markup = File.ReadAllText(xamlPath, Encoding.UTF8);

        // 1) x:Class — kod arkası tipini işaret eder, XamlReader onu üretemez
        markup = Regex.Replace(markup, @"\s+x:Class=""[^""]*""", string.Empty);

        // 2) Olay bağlantıları. Hangi özniteliğin olay olduğunu tahmin etmek
        //    yerine kardeş .xaml.cs dosyasındaki metot adları okunur ve yalnızca
        //    onlara eşit değerler silinir — hem kesin hem kendi kendini günceller.
        foreach (var handler in HandlerNames(xamlPath))
            markup = Regex.Replace(markup, $@"\s+[A-Za-z]+=""{Regex.Escape(handler)}""", string.Empty);

        // 3) Window.Icon — hem "pack://application:,,,/..." hem "Assets/AppIcon.ico"
        //    biçimi çalışan uygulamanın GİRİŞ assembly'sini arar; testte o
        //    testhost.exe'dir ve kaynak bulunamaz. İkon temayı ilgilendirmiyor.
        markup = Regex.Replace(markup, @"\s+Icon=""[^""]*""", string.Empty);

        // 4) clr-namespace'lere assembly adı eklenir. Kaynak XAML'de bu bilgi
        //    örtüktür ("derlendiği assembly"); XamlReader ise çağıran assembly'ye
        //    bakar ve tipleri test assembly'sinde arar.
        //
        //    Kapanış tırnağına kadar EŞLEŞTİRİLİR. Önceki hâli negatif ileri
        //    bakış kullanıyordu ("...(?!;assembly)") ve geri izleme yüzünden
        //    zaten assembly taşıyan bir namespace'i de bozuyordu: motor
        //    namespace'in bir harf kısasını eşleştirip lookahead'i geçiriyordu.
        //    Harici paket namespace'i (LiveCharts) gelene kadar fark edilmedi.
        markup = Regex.Replace(
            markup,
            @"clr-namespace:(?<ns>[A-Za-z0-9_.]+)(?="")",
            m => $"clr-namespace:{m.Groups["ns"].Value};assembly={UiAssembly}");

        return markup;
    }

    private static IReadOnlyList<string> HandlerNames(string xamlPath)
    {
        var codeBehind = xamlPath + ".cs";
        if (!File.Exists(codeBehind)) return [];

        // Metot bildirimlerini yakalar: "private void Foo(", "public async void Bar("
        var source = File.ReadAllText(codeBehind, Encoding.UTF8);
        return Regex.Matches(source, @"\b(?:private|public|protected|internal)\s+(?:async\s+)?[\w<>,\[\]\?\. ]+?\s+(?<name>\w+)\s*\(")
                    .Select(m => m.Groups["name"].Value)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
    }
}
