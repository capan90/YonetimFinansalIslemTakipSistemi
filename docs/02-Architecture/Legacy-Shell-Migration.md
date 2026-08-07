# Legacy — Shell Migration

**Durum:** Dondurulmuş (Faz E3, "Legacy Freeze")
**Kaldırma:** Sonraki sprint ("Legacy Removal"), kabuk gerçek kullanımda birkaç gün sınandıktan sonra

## Karar

Faz D tüm ekranları `UserControl`'e taşıdı ve `ShellWindow` tek başlangıç penceresi oldu.
Eski pencereler bu geçişte **silinmedi**; geri dönüş yolu olarak bırakıldı.

Faz E3'te durum ölçüldü: geri dönüş yolu **zaten kapalı**. `App` doğrudan `ShellWindow`
açıyor, `MainWindow` hiçbir yerden örneklenmiyor, ince barındırıcı pencereleri de yalnızca
`MainWindow` ve ekranlardaki "kabuk yoksa pencere aç" yedek dalları açıyordu.

Buna rağmen **silinmedi**: kabuk henüz gerçek kullanımda birkaç gün geçirmedi ve silmek
geri dönüşü pahalı bir karardır. Bu sprint yalnızca **dondurur**.

## Donma kuralları

- Bu sınıflara **yeni kod yazılmaz**.
- Yeni özellikler **yalnızca kabuk mimarisine** eklenir.
- **Testler yalnızca kabuk üzerinden** çalışır; donmuş tiplere derleme bağı kurmaz.
- Her sınıf `[Obsolete(LegacyShellMigration.Reason)]` işaretlidir ve
  `#region Legacy - Shell Migration` bölgesindedir.
- Donmaya uyulduğunu `tests/…/LegacyFreezeTests.cs` bekçiler.

## Envanter — 16 sınıf

### Eski ana pencere (1)

| Sınıf | Yerini alan |
|---|---|
| `MainWindow` | `ShellWindow` (navigasyon rayı + sekmeler) |

### İnce barındırıcı pencereler (15)

İçerikleri ilgili `*Screen` kontrolündedir; bu pencereler yalnızca başlık/boyut/ikon taşır.

| Sınıf | İçeriği taşıyan ekran |
|---|---|
| `AnalysisWindow` | `AnalysisScreen` |
| `AuditLogWindow` | `AuditLogScreen` |
| `CargoCompanyListWindow` | `CargoCompanyListScreen` |
| `CargoDashboardWindow` | `CargoDashboardScreen` |
| `CargoOperationCenterWindow` | `CargoOperationCenterScreen` |
| `CargoShipmentListWindow` | `CargoShipmentListScreen` |
| `CompanyDirectoryListWindow` | `CompanyDirectoryListScreen` |
| `ExchangeRateWindow` | `ExchangeRateScreen` |
| `SystemHealthWindow` | `SystemHealthScreen` |
| `MailContactListWindow` | `MailContactListScreen` |
| `UserPermissionWindow` | `UserPermissionScreen` |
| `ReportWindow` | `ReportScreen` |
| `SystemLogsWindow` | `SystemLogsScreen` |
| `UserManagementWindow` | `UserManagementScreen` |
| `WhatsAppContactListWindow` | `WhatsAppContactListScreen` |

### Dondurulmayanlar

Bunlar **gerçek pencere olarak kullanılmaya devam ediyor** ve kapsam dışıdır:
düzenleme formları (`*EditWindow`, `UserFormWindow`, `CashTransactionFormWindow`),
içe aktarma sihirbazları (`*ImportWindow`), `ReportPreviewWindow`,
`CargoNotificationPreviewWindow`, `MailContactPickerWindow` ve ayar pencereleri
(`MailSettingsWindow`, `TextCaseSettingsWindow`, `AppearanceSettingsWindow`).

## Canlı koddan kalan bağlar (6)

Kaldırma sprintinde bu dallar da gidecek. Hepsi `Navigator is null` yedek dalıdır ve
kabukta `Navigator` her zaman atandığı için **pratikte ölüdür**.

| Dosya | Bağ sayısı | Ne yapıyor |
|---|---|---|
| `Views/Cargo/CargoDashboardScreen.xaml.cs` | 5 | Şerit düğmeleri: gelen/giden kargo, firma rehberi, kargo firmaları, WhatsApp rehberi |
| `Views/Cargo/CargoShipmentListScreen.xaml.cs` | 1 | "Operasyon" düğmesi → operasyon merkezi |

Bağlar `#pragma warning disable CS0618` ile dar kapsamda bastırıldı: **yeni** bir legacy
kullanımı sızarsa derleme yine uyarır.

## Kaldırma sprintinde yapılacaklar

1. `MainWindow` + 15 ince barındırıcı (`.xaml` + `.xaml.cs`) silinir.
2. Ekranlardaki `Navigator is null` yedek dalları ve `Func<Window> asWindow`
   parametreleri silinir; `OpenScreen` yalnızca gezgin üzerinden çalışır.
3. `IShellCloseSource` / `HostWindow` gibi yalnızca pencere yolu için duran
   üyeler gözden geçirilir.
4. `LegacyShellMigration` ve `LegacyFreezeTests` silinir.
5. `ShellPilotTests` / `ShellFullMigrationTests` içinde pencere yoluna değinen
   sözleşmeler sadeleşir.

## Karar sahibi

Kullanıcı, 2026-08-07: *"Bu sprintte MainWindow ve ince barındırıcı pencereleri SİLME.
Ancak onları artık aktif çalışma yolunun dışında bırak."*
