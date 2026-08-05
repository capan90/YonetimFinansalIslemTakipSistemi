using System.Reflection;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Test çıktısından depo kökündeki UI kaynak klasörünü bulur.
/// XAML parse ve ham renk taraması testleri derlenmiş BAML'a değil, kaynak
/// dosyalara bakar — amaç geliştiricinin yazdığı metni denetlemek.
/// </summary>
public static class UiSourceLocator
{
    private static string? _cached;

    public static string UiProjectDirectory => _cached ??= Locate();

    public static IReadOnlyList<string> XamlFiles(bool includeResources) =>
        Directory.EnumerateFiles(UiProjectDirectory, "*.xaml", SearchOption.AllDirectories)
                 .Where(p => !IsBuildArtifact(p))
                 .Where(p => includeResources || !IsResourceDictionary(p))
                 .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                 .ToList();

    public static IReadOnlyList<string> CsFiles() =>
        Directory.EnumerateFiles(UiProjectDirectory, "*.cs", SearchOption.AllDirectories)
                 .Where(p => !IsBuildArtifact(p))
                 .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                 .ToList();

    /// <summary>Tema sözlükleri ham hex tanımlamak ZORUNDADIR — taramadan muaftır.</summary>
    public static bool IsThemeDictionary(string path) =>
        path.Replace('\\', '/').Contains("/Resources/Themes/", StringComparison.OrdinalIgnoreCase);

    public static string Relative(string path) =>
        Path.GetRelativePath(UiProjectDirectory, path).Replace('\\', '/');

    private static bool IsBuildArtifact(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsResourceDictionary(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/Resources/", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("/App.xaml", StringComparison.OrdinalIgnoreCase);
    }

    private static string Locate()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        for (var current = new DirectoryInfo(dir); current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, "src", "YonetimFinansalIslemTakipSistemi.UI");
            if (Directory.Exists(candidate)) return candidate;
        }

        throw new DirectoryNotFoundException(
            $"UI projesi bulunamadı. Test çıktısı: {dir}");
    }
}
