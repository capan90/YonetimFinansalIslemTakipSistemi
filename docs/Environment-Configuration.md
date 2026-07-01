# Ortam Yapılandırması (Development / Production)

Bu belge, uygulamanın hangi ortamda hangi veritabanına bağlandığını ve production
yayını öncesi yapılması gerekenleri açıklar.

## Ortam Nasıl Belirlenir?

Aktif ortam adı şu öncelik sırasıyla çözülür (hem çalışan uygulama hem `dotnet ef`):

1. `YONETIM_ENVIRONMENT`
2. `DOTNET_ENVIRONMENT`
3. `ASPNETCORE_ENVIRONMENT`
4. `appsettings.json` içindeki `AppEnvironment` değeri
5. `Development` (varsayılan)

Yapılandırma katmanlı yüklenir:

```
appsettings.json  →  appsettings.{Environment}.json  →  ortam değişkenleri
```

Ortam değişkenleri en son eklenir → en yüksek önceliğe sahiptir.
`YONETIM_DB_CONNECTION` env var'ı, bağlantı dizesi için her zaman en öncelikli override'dır.

## Dosyalar

| Dosya | Git | İçerik |
|-------|-----|--------|
| `appsettings.json` | İzlenir | Ortak ayarlar + güvenli **dev** fallback. Repo'da `AppEnvironment=Development`. |
| `appsettings.Development.json` | İzlenir | Yerel geliştirme DB bağlantı dizesi. |
| `appsettings.Production.json` | **gitignore** | Gerçek prod bağlantı dizesi. **Yalnızca publish makinesinde** bulunur, repoya commit edilmez. |
| `docs/config-production-example.json` | İzlenir | Placeholder şablon. Yeni publish makinesinde `appsettings.Production.json` oluştururken kopyalanır. |

> ⚠️ **Production şifresi asla Git'e yazılmaz.** `appsettings.Production.json` ve
> `Publish-Production.local.ps1` `.gitignore` içindedir. Gerçek prod bağlantı dizesi
> sadece publish makinesinin yerel diskinde durur.

## Hangi Ortam Hangi DB'ye Bağlanır?

| Bağlam | Ortam | DB |
|--------|-------|-----|
| `dotnet run` / VS Code / normal build (env var yok) | Development | `yonetim_dev` (localhost) |
| `dotnet ef ...` (env var yok) | Development | `yonetim_dev` (localhost) |
| `YONETIM_ENVIRONMENT=Production` ile çalıştırma | Production | `yonetim_finansal` (10.0.0.169) |
| ClickOnce ile yayımlanan istemci | Production | `yonetim_finansal` (10.0.0.169) |

ClickOnce istemcilerinde ortam değişkeni taşınmaz. Bu yüzden `Publish-Production.ps1`,
yayımlanan `appsettings.json` içine `AppEnvironment=Production` gömer ve
`appsettings.Production.json`'ı ClickOnce paketine dahil eder.

## Migration Komutları

**Development (varsayılan — yerel dev DB):**

```powershell
dotnet ef database update `
  --project src/YonetimFinansalIslemTakipSistemi.Infrastructure `
  --startup-project src/YonetimFinansalIslemTakipSistemi.UI
```

Bu komut ortam değişkeni verilmediği için Development'a gider ve `yonetim_dev`'i günceller.

**Production (bilinçli olarak canlı DB):**

```powershell
$env:YONETIM_ENVIRONMENT = "Production"
# veya doğrudan override:
$env:YONETIM_DB_CONNECTION = "Host=10.0.0.169;Port=5432;Database=yonetim_finansal;Username=yonetim_app;Password=<PROD_SIFRESI>"

dotnet ef database update `
  --project src/YonetimFinansalIslemTakipSistemi.Infrastructure `
  --startup-project src/YonetimFinansalIslemTakipSistemi.UI

# İşiniz bitince temizleyin:
Remove-Item Env:\YONETIM_ENVIRONMENT
Remove-Item Env:\YONETIM_DB_CONNECTION
```

`YONETIM_ENVIRONMENT=Production` verildiğinde `appsettings.Production.json` yüklenir
(publish makinesinde mevcut olmalı). Şifreyi komuta yazmak yerine bu dosyayı kullanmak
tercih edilir.

## Production Publish Öncesi Gereksinimler

1. Publish makinesinde `src/YonetimFinansalIslemTakipSistemi.UI/appsettings.Production.json`
   dosyası gerçek prod bağlantı dizesiyle mevcut olmalı (gitignore'da, commit edilmez).
   Şablon: `docs/config-production-example.json`.
2. (Opsiyonel) Gizli değerleri script'e girmeden vermek için `Publish-Production.local.ps1`
   oluşturun (gitignore'da):

   ```powershell
   # Publish-Production.local.ps1  (repoya girmez)
   $env:YONETIM_DB_CONNECTION = "Host=10.0.0.169;Port=5432;Database=yonetim_finansal;Username=yonetim_app;Password=<PROD_SIFRESI>"
   $env:YONETIM_SMTP_PASSWORD = "<SMTP_SIFRESI>"
   ```

3. Yayın:

   ```powershell
   .\Publish-Production.ps1 -Version "1.0.0.x"
   ```

   `Publish-Production.ps1`:
   - `YONETIM_ENVIRONMENT=Production` ayarlar,
   - varsa `Publish-Production.local.ps1`'i yükler,
   - `appsettings.Production.json` yoksa hata verir,
   - `Publish-ClickOnce.ps1 -Environment Production` çağırarak ortamı pakete gömer.

> **Güvenlik hatırlatması:** `Publish-Production.ps1` içine gerçek şifre yazmayın.
> Şifreler `appsettings.Production.json` (yerel) veya `Publish-Production.local.ps1`
> (yerel) üzerinden verilir; her ikisi de `.gitignore` kapsamındadır.
