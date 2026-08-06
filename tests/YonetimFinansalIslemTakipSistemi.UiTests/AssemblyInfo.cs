// Tema testleri TEK bir WPF Application örneğini ve onun kaynak sözlüğünü
// paylaşır. xUnit varsayılan olarak test SINIFLARINI paralel çalıştırır;
// paralelken bir sınıf koyu temayı uygularken diğeri açık tema bekleyebilir
// ve testler rastgele kırılır.
//
// Faz C'de ChartPaletteTests eklenince bu yarış görünür hâle geldi
// (ThemeTestHost.ApplyTheme kullanan sınıf sayısı arttı).
//
// Paralellik kapatıldı: bu paketin darboğazı CPU değil, paylaşılan UI durumu.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
