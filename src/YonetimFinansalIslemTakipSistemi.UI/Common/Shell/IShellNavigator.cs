namespace YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

/// <summary>
/// Bir ekranın BAŞKA bir ekranı açmasının tek yolu.
///
/// NEDEN VAR: Kargo panosu kargo listelerini, kargo listesi de operasyon
/// merkezini açıyor. Pencere dünyasında bu <c>new XWindow(...).ShowDialog()</c>
/// idi; kabukta sekme açmaya dönüşmesi gerekiyor.
///
/// Kapsam bilinçli olarak DAR: yalnızca "şu ekranı aç". Ekran ne kabuğu, ne
/// sekme listesini, ne de kayıt tablosunu görür — yetki kontrolü ve tekillik
/// kabuğun tarafında kalır (bkz. ShellViewModel.Resolve). Bu yüzden bir ekran
/// yetkisi olmayan bir ekranı açamaz; çağrı sessizce reddedilir.
///
/// SERVICE LOCATOR DEĞİL: ekran buradan servis çözmez, yalnızca navigasyon
/// ister. Bağımlılıkları hâlâ kendi kurucusundan gelir.
/// </summary>
public interface IShellNavigator
{
    /// <summary>Tekil ekranı açar veya zaten açıksa ona odaklanır.</summary>
    /// <returns>Açıldıysa <c>true</c>; yetki yoksa veya ekran taşınmamışsa <c>false</c>.</returns>
    bool OpenScreen(ScreenKey key);

    /// <summary>
    /// Bir KAYIT üzerinde çalışan ekranı açar (ör. Kargo Operasyon Merkezi).
    /// Aynı kayıt ikinci kez açılırsa mevcut sekmeye odaklanılır.
    /// </summary>
    bool OpenScreen(ScreenKey key, object parameter);

    /// <summary>
    /// Yukarıdakiyle aynı; ek olarak AÇILAN GÖRÜNÜMÜ döndürür.
    ///
    /// NEDEN VAR (Faz F5): açan ekran, açtığı ekranda bir değişiklik olup
    /// olmadığını bilmek isteyebilir. Kargo listesi operasyon merkezini ayrı
    /// bir sekmede açıyor; merkez kaydı değiştirdiyse liste tazelenmeli,
    /// DEĞİŞTİRMEDİYSE tazelenmemeli. Örneğe erişmeden bu ayrım yapılamıyordu
    /// ve liste sekmeye her dönüldüğünde gereksiz sorgu atıyordu.
    ///
    /// Uygulama geneli bir "değişiklik yayını" KURULMADI: bağ yalnızca açan
    /// ile açılan arasında, yereldir.
    /// </summary>
    /// <returns>Açılan (veya zaten açık olan) görünüm; açılamadıysa <c>null</c>.</returns>
    System.Windows.FrameworkElement? OpenScreenView(ScreenKey key, object parameter);
}

/// <summary>
/// Başka ekran açması gereken ekranlar bunu uygular; kabuk sekme üretirken
/// gezgini verir.
///
/// Uygulaması ZORUNLU DEĞİL — çoğu ekran kendi başına çalışır ve bu arayüzü
/// uygulamaz. Gezgin atanmadan kullanılmaya çalışılırsa hata yerine sessiz
/// bir hiçlik olmasın diye ekranlar <c>Navigator</c>'ı null kontrolüyle
/// kullanmalı (barındıran pencere kabuk olmayabilir; MainWindow hâlâ mevcut).
/// </summary>
public interface IShellNavigationAware
{
    /// <summary>Kabuk sekme oluştururken atar. Kabuk dışında barınırken null kalır.</summary>
    IShellNavigator? Navigator { get; set; }
}
