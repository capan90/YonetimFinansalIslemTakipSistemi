# Legacy — Shell Migration

**Durum:** TAMAMLANDI (Faz F1, "Legacy Removal")
**Önceki durum:** Dondurulmuş (Faz E3, "Legacy Freeze")

## Özet

Faz D tüm ekranları `UserControl`'e taşıdı ve `ShellWindow` tek başlangıç penceresi oldu.
Eski pencereler geri dönüş yolu olarak bırakıldı, Faz E3'te donduruldu (`[Obsolete]` + bölge
+ bekçi test), Faz F1'de **kaldırıldı**.

Uygulamada artık ekranlara ulaşmanın tek yolu kabuk sekmeleridir.

## Silinenler (Faz F1)

### Eski ana pencere
- `MainWindow.xaml` / `.xaml.cs` — menü çubuklu eski ana pencere. Hiçbir yerden örneklenmiyordu;
  `App` doğrudan `ShellWindow` açıyor.

### İnce barındırıcı pencereler (15)
`AnalysisWindow`, `AuditLogWindow`, `CargoCompanyListWindow`, `CargoDashboardWindow`,
`CargoOperationCenterWindow`, `CargoShipmentListWindow`, `CompanyDirectoryListWindow`,
`ExchangeRateWindow`, `SystemHealthWindow`, `MailContactListWindow`, `UserPermissionWindow`,
`ReportWindow`, `SystemLogsWindow`, `UserManagementWindow`, `WhatsAppContactListWindow`

İçerikleri zaten ilgili `*Screen` kontrolündeydi; bu pencereler yalnızca başlık/boyut/ikon
taşıyordu. Onları yalnızca `MainWindow` ve ekranlardaki yedek dallar açıyordu.

### Yedek gezinme dalları
- `CargoDashboardScreen`: `OpenScreen(key, Func<Window>)` yardımcısı + 5 şerit düğmesi handler'ı
- `CargoShipmentListScreen`: operasyon merkezini modal pencerede açan dal

Hepsi `Navigator is null` koşuluna bağlıydı ve kabukta `Navigator` her zaman atandığı için
(bkz. `ShellViewModel.Attach`) pratikte ölüydü.

### Yalnızca legacy yol için duran öğeler
- `IShellLogoutSource` — ekranların kendi "Çıkış Yap" düğmesini kabuğa duyuran sözleşme.
  Düğmeler kabukta zaten gizleniyordu; kaldırılınca sözleşmenin uygulayıcısı kalmadı.
- `CargoDashboardScreen`'in navigasyon şeridi: Gelen/Giden/Rehber/Firmalar/WhatsApp düğmeleri,
  Yardım menüsü (Güncellemeleri Denetle, Mail Ayarlarım, Log Klasörü, Harf Duyarlılığı) ve
  Çıkış Yap. Hepsinin karşılığı kabuğun navigasyon rayında ve **Araçlar** bloğunda var.
- `CashTransactionsScreen`'in oturum şeridi: kullanıcı adı + Çıkış Yap. Karşılıkları kabuğun
  durum şeridinde ve rayında.
- `CargoDashboardScreen`'deki açılış güncelleme kontrolü (`!InShell` dalı). Kontrol artık tek
  yerde: `ShellWindow`.
- `LegacyShellMigration` (donma gerekçesi) ve `LegacyFreezeTests` (donma bekçisi).

## Korunanlar

Aşağıdakiler **pencere olarak kullanılmaya devam ediyor** — ekran değil, ekranların açtığı
modal adımlar:

| Rol | Pencereler |
|---|---|
| Oturum | `LoginWindow` |
| Kabuk | `ShellWindow` |
| Kayıt formu | `CashTransactionFormWindow`, `UserFormWindow`, `CargoShipmentEditWindow`, `CargoCompanyEditWindow`, `CompanyDirectoryEditWindow`, `MailContactEditWindow`, `WhatsAppContactEditWindow` |
| İçe aktarma | `CashImportWindow`, `CargoImportWindow`, `DirectoryImportWindow`, `WhatsAppImportWindow` |
| Önizleme / detay / seçici | `ReportPreviewWindow`, `CargoNotificationPreviewWindow`, `SystemLogDetailWindow`, `MailContactPickerWindow` |
| Ayar | `MailSettingsWindow`, `TextCaseSettingsWindow`, `AppearanceSettingsWindow` |

Bu liste `ShellOnlyNavigationTests.AllowedWindows` içinde gerekçeleriyle birlikte tutulur ve
testle doğrulanır: listede olmayan bir pencere eklenirse test düşer.

## Geri dönüş

Git geçmişi. Silinen dosyalar `feature/single-shell` ve öncesindeki commitlerde duruyor;
Faz E3 commit'i (`1768511`) hepsinin son çalışır hâlini içerir.

## Bekçi testler

| Test | Neyi tutuyor |
|---|---|
| `ShellOnlyNavigationTests.Pencere_olarak_kalan_tipler_yalnizca_diyaloglar` | Yeni bir ekran pencere olarak eklenemez |
| `ShellOnlyNavigationTests.Ekranlar_gezinmek_icin_pencere_acmiyor` | Ekran, gezinmek için pencere açamaz |
| `ShellOnlyNavigationTests.Ekranlarin_kendi_navigasyon_seridi_yok` | İkinci kabuk geri gelemez |
| `ShellOnlyNavigationTests.IShellNavigator_yalnizca_kabukta_uygulaniyor` | Tek yetki kapısı |
| `ShellOnlyNavigationTests.Baslangic_yalnizca_kabugu_aciyor` | Başlangıçta ikinci pencere yok |
| `ShellFullMigrationTests.Legacy_pencere_geri_gelmedi` | Silinen dosyalar geri gelmez |
| `ShellStartupContractTests.Baslangicta_yalnizca_kabuk_penceresi_var` | Eski kabuklar geri gelmez |

## Karar geçmişi

- **2026-08-07 (Faz E3):** *"Bu sprintte MainWindow ve ince barındırıcı pencereleri SİLME.
  Ancak onları artık aktif çalışma yolunun dışında bırak."* → dondurma.
- **Faz F1:** kabuk gerçek kullanımda doğrulandıktan sonra kaldırma.
