# ADR-002: ClickOnce Dağıtım Stratejisi

**Tarih:** 2026-06-18  
**Durum:** Kabul Edildi

---

## Karar

V1 dağıtımı için ClickOnce + UNC ağ paylaşımı kullanılır. Publish scripti `dotnet-mage` tabanlıdır; Visual Studio gerektirmez.

---

## Bağlam

- Uygulama 5-20 çalışan bilgisayarına kurulacak.
- Merkezi bir IT departmanı yok; güncellemeler kolay olmalı.
- .NET 9 WPF uygulaması Windows'a özgü.
- Geliştirici ortamı: VS Code + .NET SDK (Visual Studio yok).

---

## Alternatifler

### A: Manuel Kurulum (xcopy, installer)

Her güncelleme için her bilgisayara git, dosyaları kopyala.

**Sorun:** Zaman alıcı. Sürüm tutarsızlığı riski. Tamamen manuel.

### B: Inno Setup veya WiX

MSI installer tabanlı dağıtım.

**Sorun:** Her sürümde installer hazırlanması gerekir. Otomatik güncelleme yok veya ayrıca implement edilmeli.

### C: MSIX / AppX

Modern Windows paketi.

**Sorun:** Kod imzalama maliyeti veya sideloading konfigürasyonu. .NET 9 WPF MSIX desteği kısıtlı.

### D: ClickOnce + UNC (Seçilen)

Uygulama bir kez kurulur; sonraki güncellemeler UNC paylaşımından otomatik gelir.

---

## Neden Seçildi

- WPF uygulamaları için olgun, kanıtlanmış teknoloji.
- Startup güncelleme sıfır kod: `UpdateMode=Foreground` pubxml ayarı yeterli.
- Kullanıcı müdahalesi gerektirmez — açılışta güncelleme otomatik kontrol edilir.
- Self-signed sertifika ile ücretsiz imzalama.
- VS Code + .NET SDK ortamında `microsoft.dotnet.mage` tool ile VS bağımlılığı ortadan kalktı.

---

## Artılar

- Otomatik güncelleme mekanizması built-in.
- İstemci kurulumu basit: `.application` dosyasına çift tık.
- Rollback: önceki sürümü sunucuya geri yükle → kullanıcılar eski sürümü alır.
- UNC klasörü güncelleme noktası olarak merkezi.

---

## Eksiler

- Self-signed sertifika her istemcide kurulumu gerektirir (bir kerelik, admin).
- UNC paylaşımı sunucu gerektiriyor.
- `System.Deployment.Application` .NET 9'da yok → update API'si kullanılamaz.
- Masaüstü kısayol ikonu özelleştirilemez (runtime kısıtı).

---

## Riskleri

- **dotnet-mage ASCII Kısıtı:** Türkçe karakter geçilirse mojibake → "hatalı biçimlendirilmiş". Tüm mage parametreleri ASCII-only olmalı.
- **VS Bağımlılığı:** `dotnet publish /p:PublishProfile=ClickOnce` Visual Studio olmadan çalışmaz. `Publish-ClickOnce.ps1` bu riski ortadan kaldırır.

---

## Sonuç

ClickOnce V1 ihtiyaçlarını karşılıyor. Gelecekte daha büyük bir dağıtım ihtiyacı oluşursa (50+ bilgisayar, domain policy, otomatik rollout) WSUS veya diğer enterprise çözümler değerlendirilebilir.
