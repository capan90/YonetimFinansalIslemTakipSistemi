using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Global ComboBox şablonunun WPF seçili-öğe gösterim mekanizmasını koruduğunu
/// doğrular.
///
/// NEDEN VAR: Koyu tema için ComboBox şablonu yeniden yazılırken kapalı alandaki
/// <c>ContentPresenter</c>'a <c>ContentTemplateSelector</c> bağlaması atlanmıştı.
/// Sonuç: <c>DisplayMemberPath</c> kullanan her ComboBox kapalı alanda nesnenin
/// <c>ToString()</c> çıktısını gösteriyordu ("ComboItem { Label = Gelen, ... }").
///
/// Mekanizma: <c>SelectionBoxItemTemplate</c>, <c>DisplayMemberPath</c>'i TAŞIMAZ —
/// o durumda null kalır (WPF'in kendi şablonunda da öyle). <c>DisplayMemberPath</c>
/// atandığında <c>ItemsControl</c> dahili bir <c>DisplayMemberTemplateSelector</c>
/// üretip <c>ItemTemplateSelector</c>'a koyar. Kapalı alana ulaşmasının TEK yolu
/// <c>ContentTemplateSelector="{TemplateBinding ItemTemplateSelector}"</c>'dır.
///
/// Bu yüzden testler token değil, GÖRÜNEN METİN üzerinden çalışır: şablon
/// bağlamalarından biri düşerse metin nesne dump'ına döner ve test kırılır.
/// </summary>
public class ComboBoxTemplateTests
{
    // Uygulamadaki ComboItem<T> ile aynı şekil: positional record.
    // ToString() -> "Lookup { Label = Gelen, Value = In }"
    private record Lookup(string Label, string? Value);

    private enum Direction { Gelen, Giden }

    public static TheoryData<string> ThemeNames() => [ThemeTestHost.Light, ThemeTestHost.Dark];

    // ── 1 & 2: DisplayMemberPath ─────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void DisplayMemberPath_kapali_alanda_yalnizca_Label_gosterir(string themeName)
    {
        var text = RenderClosed(themeName, cb =>
        {
            cb.ItemsSource       = new[] { new Lookup("Tümü", null), new Lookup("Gelen", "In") };
            cb.DisplayMemberPath = "Label";
            cb.SelectedIndex     = 1;
        });

        Assert.Equal("Gelen", text);
        AssertNoTechnicalText(text);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void DisplayMemberPath_secim_degisince_guncellenir(string themeName)
    {
        var text = RenderClosed(themeName, cb =>
        {
            cb.ItemsSource       = new[] { new Lookup("Tümü", null), new Lookup("Gelen", "In") };
            cb.DisplayMemberPath = "Label";
            cb.SelectedIndex     = 1;
            cb.SelectedIndex     = 0;   // seçim değişti
        });

        Assert.Equal("Tümü", text);
    }

    // ── 3: ItemTemplate ──────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void ItemTemplate_kapali_alanda_uygulanir(string themeName)
    {
        var text = RenderClosed(themeName, cb =>
        {
            cb.ItemsSource   = new[] { new Lookup("Denetim", "audit") };
            cb.ItemTemplate  = BuildTemplate("Label");
            cb.SelectedIndex = 0;
        });

        Assert.Equal("Denetim", text);
        AssertNoTechnicalText(text);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void ItemTemplateSelector_kapali_alanda_uygulanir(string themeName)
    {
        var text = RenderClosed(themeName, cb =>
        {
            cb.ItemsSource           = new[] { new Lookup("Seçici", "x") };
            cb.ItemTemplateSelector  = new LabelTemplateSelector();
            cb.SelectedIndex         = 0;
        });

        Assert.Equal("Seçici", text);
    }

    // ── 4 & 5: string ve enum listeleri ──────────────────────────────────────

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void String_listesi_dogru_gorunur(string themeName)
    {
        var text = RenderClosed(themeName, cb =>
        {
            cb.ItemsSource   = new[] { "Tümü", "TL", "USD" };
            cb.SelectedIndex = 1;
        });

        Assert.Equal("TL", text);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Enum_listesi_tip_adi_gostermeden_gorunur(string themeName)
    {
        var text = RenderClosed(themeName, cb =>
        {
            cb.ItemsSource   = Enum.GetValues<Direction>();
            cb.SelectedIndex = 1;
        });

        // Enum.ToString() zaten üye adıdır; namespace/tip adı sızmamalı
        Assert.Equal("Giden", text);
        Assert.DoesNotContain("Direction", text, StringComparison.Ordinal);
        Assert.DoesNotContain("YonetimFinansal", text, StringComparison.Ordinal);
    }

    // ── 6: null seçim ────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Secim_yokken_teknik_metin_uretilmez(string themeName)
    {
        var text = RenderClosed(themeName, cb =>
        {
            cb.ItemsSource       = new[] { new Lookup("Gelen", "In") };
            cb.DisplayMemberPath = "Label";
            // SelectedIndex atanmadı — seçim yok
        });

        Assert.True(string.IsNullOrEmpty(text),
            $"Seçim yokken kapalı alanda metin bekleniyordu boş, gelen: \"{text}\"");
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Null_deger_tasiyan_oge_Label_ile_gorunur(string themeName)
    {
        // "Tümü" seçeneği Value=null taşır; ekranlarda varsayılan/placeholder rolündedir.
        var text = RenderClosed(themeName, cb =>
        {
            cb.ItemsSource       = new[] { new Lookup("Tümü", null) };
            cb.DisplayMemberPath = "Label";
            cb.SelectedIndex     = 0;
        });

        Assert.Equal("Tümü", text);
        AssertNoTechnicalText(text);
    }

    // ── 7: IsEditable ────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Editable_ComboBox_serbest_metni_korur(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var cb = new ComboBox { IsEditable = true, ItemsSource = new[] { "Ahmet", "Mehmet" } };
            Realize(cb);

            var editBox = cb.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            Assert.True(editBox is not null, "PART_EditableTextBox şablonda bulunamadı — editable mod çöker.");
            Assert.Equal(Visibility.Visible, editBox!.Visibility);

            // Listede olmayan serbest metin yazılabilmeli
            cb.Text = "Yeni Kişi";
            cb.UpdateLayout();
            Assert.Equal("Yeni Kişi", cb.Text);
        });
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Editable_olmayan_ComboBox_ta_metin_kutusu_gizlidir(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var cb = new ComboBox { ItemsSource = new[] { "A", "B" }, SelectedIndex = 0 };
            Realize(cb);

            var editBox = (TextBox)cb.Template.FindName("PART_EditableTextBox", cb)!;
            Assert.NotEqual(Visibility.Visible, editBox.Visibility);
        });
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Editable_ComboBox_DisplayMemberPath_ile_secili_Label_gosterir(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var cb = new ComboBox
            {
                IsEditable        = true,
                ItemsSource       = new[] { new Lookup("Aras Kargo", "aras") },
                DisplayMemberPath = "Label",
                SelectedIndex     = 0,
            };
            Realize(cb);

            AssertNoTechnicalText(cb.Text);
            Assert.Equal("Aras Kargo", cb.Text);
        });
    }

    // ── 9: açılır liste ile kapalı alan tutarlılığı ──────────────────────────

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Acilir_liste_metni_kapali_alan_metniyle_ayni(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var cb = new ComboBox
            {
                ItemsSource       = new[] { new Lookup("Bekliyor", "w"), new Lookup("Teslim Edildi", "d") },
                DisplayMemberPath = "Label",
                SelectedIndex     = 1,
            };
            Realize(cb);

            var container    = GenerateContainer(cb, index: 1);
            var dropdownText = VisibleText(container);
            var closedText   = ClosedBoxText(cb);

            Assert.Equal("Teslim Edildi", dropdownText);
            Assert.Equal(dropdownText, closedText);
        });
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Acilir_listedeki_oge_teknik_metin_gostermez(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var cb = new ComboBox
            {
                ItemsSource       = new[] { new Lookup("Aras Kargo", "aras") },
                DisplayMemberPath = "Label",
            };
            Realize(cb);

            AssertNoTechnicalText(VisibleText(GenerateContainer(cb, index: 0)));
        });
    }

    // ── 10: SelectedItem / SelectedValue / SelectedValuePath ─────────────────

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void SelectedValuePath_ile_SelectedValue_bozulmaz(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var items = new[] { new Lookup("Bekliyor", "waiting"), new Lookup("Teslim", "delivered") };

            var cb = new ComboBox
            {
                ItemsSource       = items,
                DisplayMemberPath = "Label",
                SelectedValuePath = "Value",
            };
            Realize(cb);

            // SelectedValue ile seçim — filtre ekranlarının kullandığı yol
            cb.SelectedValue = "delivered";
            cb.UpdateLayout();

            Assert.Same(items[1], cb.SelectedItem);
            Assert.Equal("delivered", cb.SelectedValue);
            Assert.Equal("Teslim", ClosedBoxText(cb));
        });
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void SelectedItem_atamasi_kapali_alani_gunceller(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var items = new[] { new Lookup("Normal", "n"), new Lookup("Çok Acil", "u") };
            var cb = new ComboBox { ItemsSource = items, DisplayMemberPath = "Label" };
            Realize(cb);

            cb.SelectedItem = items[1];
            cb.UpdateLayout();

            Assert.Equal(1, cb.SelectedIndex);
            Assert.Equal("Çok Acil", ClosedBoxText(cb));
        });
    }

    // ── XAML'de sabit tanımlı ComboBoxItem (Görünüm/Harf Duyarlılığı ayarları) ──

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Sabit_ComboBoxItem_icerigi_gorunur(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var cb = new ComboBox();
            cb.Items.Add(new ComboBoxItem { Content = "Açık Tema", Tag = "Light" });
            cb.Items.Add(new ComboBoxItem { Content = "Koyu Tema", Tag = "Dark" });
            cb.SelectedIndex = 1;
            Realize(cb);

            var text = ClosedBoxText(cb);
            Assert.Equal("Koyu Tema", text);
            Assert.DoesNotContain("ComboBoxItem", text, StringComparison.Ordinal);
        });
    }

    // ── Şablon bağlamalarının varlığı (regresyonun kendisi) ──────────────────

    /// <summary>
    /// Kapalı seçim alanındaki dört bağlamanın da yerinde olduğunu doğrular.
    /// Görünen metin testleri zaten kırılırdı; bu test hangi bağlamanın düştüğünü
    /// doğrudan söyler.
    /// </summary>
    [Fact]
    public void Kapali_secim_alani_dort_bagllamayi_da_tasir()
    {
        ThemeTestHost.Run(() =>
        {
            var cb = new ComboBox
            {
                ItemsSource       = new[] { new Lookup("X", "x") },
                DisplayMemberPath = "Label",
                SelectedIndex     = 0,
            };
            Realize(cb);

            var site = cb.Template.FindName("ContentSite", cb) as ContentPresenter;
            Assert.True(site is not null, "ContentSite ContentPresenter şablonda bulunamadı.");

            Assert.NotNull(site!.Content);

            // DisplayMemberPath'in kapalı alana ulaşmasının TEK yolu budur.
            Assert.True(site.ContentTemplateSelector is not null,
                "ContentTemplateSelector bağlanmamış — DisplayMemberPath kapalı alanda çalışmaz " +
                "ve seçili öğe nesnenin ToString()'i olarak görünür.");
        });
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private static string RenderClosed(string themeName, Action<ComboBox> configure)
    {
        ThemeTestHost.ApplyTheme(themeName);

        return ThemeTestHost.Run(() =>
        {
            var cb = new ComboBox();
            configure(cb);
            Realize(cb);
            return ClosedBoxText(cb);
        });
    }

    /// <summary>Şablonu uygular ve düzeni kurar — görsel ağaç bunsuz oluşmaz.</summary>
    private static void Realize(ComboBox cb)
    {
        // Kapsayıcı bir Border, ComboBox'ın ölçülebilmesi için yeterli
        var host = new Border { Child = cb };
        host.Measure(new Size(400, 120));
        host.Arrange(new Rect(0, 0, 400, 120));
        cb.ApplyTemplate();
        cb.UpdateLayout();
    }

    /// <summary>
    /// Açılır listedeki kapsayıcıyı üretir ve şablonunu uygular.
    ///
    /// <c>IsDropDownOpen = true</c> kullanılamaz: Popup kendi penceresini açar ve
    /// test ortamında (görünür Window yok) kapsayıcılar gerçeklenmez. Bunun yerine
    /// WPF'in kendi kullandığı generator API'si çağrılır — <c>PrepareItemContainer</c>
    /// ItemsControl'ün Content / ContentTemplate / ContentTemplateSelector
    /// aktarımını yapan adımdır, yani test edilmek istenen yol.
    /// </summary>
    private static ComboBoxItem GenerateContainer(ComboBox cb, int index)
    {
        // StartAt / GenerateNext / PrepareItemContainer somut sınıfta değil,
        // IItemContainerGenerator arayüzünde tanımlıdır.
        var generator = (System.Windows.Controls.Primitives.IItemContainerGenerator)cb.ItemContainerGenerator;
        DependencyObject? container = null;

        using (generator.StartAt(new System.Windows.Controls.Primitives.GeneratorPosition(-1, 0),
                                 System.Windows.Controls.Primitives.GeneratorDirection.Forward))
        {
            for (var i = 0; i <= index; i++)
            {
                container = generator.GenerateNext(out var isNewlyRealized);
                if (isNewlyRealized) generator.PrepareItemContainer(container);
            }
        }

        var item = container as ComboBoxItem;
        Assert.True(item is not null, "ComboBoxItem kapsayıcısı üretilemedi.");

        // Popup gerçeklenmediği için kapsayıcının görsel ebeveyni yok; kendi
        // içeriğini üretmesi için bir kaba alınıp ölçülür.
        var host = new Border { Child = item };
        host.Measure(new Size(300, 40));
        host.Arrange(new Rect(0, 0, 300, 40));
        item!.ApplyTemplate();
        item.UpdateLayout();
        return item;
    }

    /// <summary>Kapalı seçim alanında kullanıcının GÖRDÜĞÜ metin.</summary>
    private static string ClosedBoxText(ComboBox cb)
    {
        var site = cb.Template.FindName("ContentSite", cb) as ContentPresenter;
        Assert.True(site is not null, "ContentSite ContentPresenter şablonda bulunamadı.");
        return VisibleText(site!);
    }

    private static string VisibleText(DependencyObject root) =>
        string.Concat(Descendants(root).OfType<TextBlock>().Select(t => t.Text)).Trim();

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    /// <summary>
    /// Kullanıcıya sınıf adı, namespace veya record ToString() çıktısı sızmamalı.
    /// </summary>
    private static void AssertNoTechnicalText(string text)
    {
        Assert.DoesNotContain("{", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Lookup", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ComboItem", text, StringComparison.Ordinal);
        Assert.DoesNotContain("YonetimFinansal", text, StringComparison.Ordinal);
    }

    private static DataTemplate BuildTemplate(string path)
    {
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(path));
        return new DataTemplate { VisualTree = factory };
    }

    private sealed class LabelTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
            => BuildTemplate("Label");
    }
}
