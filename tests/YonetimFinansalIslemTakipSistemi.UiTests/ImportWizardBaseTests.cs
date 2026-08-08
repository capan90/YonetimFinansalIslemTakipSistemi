using System.Reflection;
using System.Text;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Import;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// İçe aktarma sihirbazlarının ORTAK TABANI (Faz F3).
///
/// Dört sihirbaz var: Kargo, Firma Rehberi, WhatsApp Rehberi, Nakit İşlem.
/// İlerleme çubuğu, durum filtresi ("✔ Geçerli (35)"), özet metni ve seçim
/// sayacı hepsinde AYNI.
///
/// KargoImportViewModel taban sınıftan ÖNCE yazılmıştı ve tabanın tamamının
/// kopyasını taşıyordu. Kopya sessizce kayar: filtre metnindeki bir düzeltme
/// üç sihirbazda uygulanır, dördüncüsünde unutulur — kimse fark etmez çünkü
/// her biri ayrı ayrı çalışır.
///
/// Bu testler tabanın gerçekten paylaşıldığını sabitler.
/// </summary>
public class ImportWizardBaseTests
{
    private static readonly Assembly Ui = typeof(ImportWizardViewModelBase<>).Assembly;

    /// <summary>Adı ImportViewModel ile biten tüm sihirbaz VM'leri.</summary>
    private static List<Type> WizardViewModels() =>
        Ui.GetTypes()
          .Where(t => t is { IsClass: true, IsAbstract: false })
          .Where(t => t.Name.EndsWith("ImportViewModel", StringComparison.Ordinal))
          .OrderBy(t => t.Name, StringComparer.Ordinal)
          .ToList();

    private static bool DerivesFromOpenGeneric(Type type, Type openGeneric)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
            if (t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric)
                return true;

        return false;
    }

    [Fact]
    public void Dort_sihirbaz_da_bulunuyor()
    {
        var names = WizardViewModels().Select(t => t.Name).ToList();

        Assert.Equal(
            ["CargoImportViewModel", "CashImportViewModel", "DirectoryImportViewModel", "WhatsAppImportViewModel"],
            names);
    }

    /// <summary>
    /// BEKÇİ: her sihirbaz ortak tabandan türemeli. Yeni bir sihirbaz kendi
    /// kopyasıyla gelirse bu test düşer.
    /// </summary>
    [Fact]
    public void Her_sihirbaz_ortak_tabandan_turuyor()
    {
        var forked = WizardViewModels()
            .Where(t => !DerivesFromOpenGeneric(t, typeof(ImportWizardViewModelBase<>)))
            .Select(t => t.Name)
            .ToList();

        Assert.True(forked.Count == 0,
            "Ortak tabanı kullanmayan sihirbaz: " + string.Join(", ", forked));
    }

    /// <summary>
    /// Her satır modeli de ortak tabandan türemeli — durum simgesi ve mesaj
    /// birleştirme orada.
    /// </summary>
    [Fact]
    public void Her_satir_modeli_ortak_tabandan_turuyor()
    {
        var rowItems = Ui.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.Name.EndsWith("ImportRowItem", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(rowItems);

        var forked = rowItems
            .Where(t => !typeof(ImportRowItemBase).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        Assert.True(forked.Count == 0,
            "ImportRowItemBase kullanmayan satır modeli: " + string.Join(", ", forked));
    }

    /// <summary>
    /// Tabandaki üyeler türeyen sınıflarda YENİDEN TANIMLANMAMALI. Aynı adlı
    /// ikinci bir property, tabandaki düzeltmeyi o sihirbazda etkisiz kılar
    /// ve fark edilmesi zordur (ikisi de derlenir, ikisi de "çalışır").
    /// </summary>
    [Fact]
    public void Sihirbazlar_taban_uyelerini_yeniden_tanimlamiyor()
    {
        string[] baseMembers =
        [
            "IsBusy", "ProgressText", "ProgressValue", "ProgressMax", "ProgressIndeterminate",
            "FilteredRows", "FilterOptions", "SelectedFilter", "AnalysisSummary",
            "SelectedCount", "CanImport", "ImportButtonText"
        ];

        var violations = new List<string>();

        foreach (var vm in WizardViewModels())
            foreach (var member in baseMembers)
            {
                var declared = vm.GetProperty(member,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (declared is not null)
                    violations.Add($"{vm.Name}.{member}");
            }

        Assert.True(violations.Count == 0,
            "Taban üyesi türeyen sınıfta yeniden tanımlanmış: " + string.Join(", ", violations));
    }

    /// <summary>
    /// Durum filtresi metinleri TEK yerde üretilmeli. İkinci bir kopya,
    /// "✔ Geçerli" ile "Geçerli ✔" gibi ayrışmalara ve filtrenin sessizce
    /// eşleşmemesine yol açar.
    /// </summary>
    [Fact]
    public void Filtre_secenekleri_yalnizca_tabanda_uretiliyor()
    {
        var producers = Directory
            .EnumerateFiles(UiSourceLocator.UiProjectDirectory, "*ImportViewModel.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f, Encoding.UTF8).Contains("✔ Geçerli", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(producers.Count == 0,
            "Filtre metni sihirbazda üretiliyor (tabana ait): " + string.Join(", ", producers));
    }
}
