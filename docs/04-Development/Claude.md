# Claude Code Geliştirici Rehberi

Bu doküman Claude Code'un bu projede nasıl çalışması gerektiğini açıklar.

---

## Görev Başlangıç Protokolü

Her göreve başlamadan önce:

1. `README.md` oku
2. `CLAUDE.md` oku
3. `docs/README.md` oku
4. İlgili `docs/` dosyasını oku
5. Etkilenen kaynak dosyaları incele
6. Önemsiz olmayan görevler için önce plan sun

---

## Önemli Kurallar

### Kod Yazarken

- SQL UI katmanında olmamalı.
- İş mantığı XAML code-behind'a ait değil (saf UI dışında).
- Domain modelleri saf ve temiz kalmalı.
- Infrastructure katmanı dışında DB erişimi olmamalı.

### Yorum Yazarken

- NEDEN açıkla, NE değil.
- Özellikle şunlarda yorum gereklidir:
  - İş kuralları
  - Audit log tetikleyicileri
  - Bakiye hesaplamaları
  - Güncelleme akışı kararları
  - Dialog tipi seçimi
  - DB erişim sınırları

### Planlama

- Birden fazla dosyayı etkileyen değişikliklerde:
  1. Yaklaşımı açıkla.
  2. Değişecek dosyaları listele.
  3. Sonra uygula.

### Yasaklar

- Gereksiz yorum ekleme (önemsiz kodlara)
- Önceden uyarmadan büyük mimari değişiklik
- Önemli klasör/dosya/servis adını sessizce değiştirme

---

## Mimari Pattern Referansları

### Handler Pattern

```csharp
// Application/Features/X/Commands/CreateX/
public class CreateXHandler
{
    public async Task<OperationResult<XDto>> Handle(CreateXCommand command)
    {
        // 1. Yetki
        if (!_userContext.HasPermission(PermissionType.CanCreate))
            return OperationResult<XDto>.Fail("Yetki yok.");
        
        // 2. Validasyon
        if (string.IsNullOrWhiteSpace(command.Name))
            return OperationResult<XDto>.Fail("İsim zorunlu.");
        
        // 3. Entity
        var entity = new X { ... };
        
        // 4. Persist
        await _repository.AddAsync(entity);
        
        // 5. Audit
        await _auditLogService.LogAsync(AuditAction.XCreated, entity.Id.ToString(), newValues: ...);
        
        return OperationResult<XDto>.Success(new XDto { ... });
    }
}
```

### UI'dan Handler'a Çağrı

```csharp
// ViewModel veya code-behind
var result = await _handler.Handle(command);
if (!result.IsSuccess)
{
    _dialogService.ShowError("Hata", result.ErrorMessage);
    return;
}
_dialogService.ShowSuccess("Başarılı", "Kayıt oluşturuldu.");
await LoadDataAsync(); // Grid refresh
```

### Async Kural

Her async metod zincirinde `await` kullanılır. `.Result` ve `.Wait()` WPF UI thread'inde deadlock yaratır. Bkz. `docs/04-Development/LessonsLearned.md`.

---

## Namespace Çakışması Uyarısı

`System.Windows.Application` ile `YonetimFinansalIslemTakipSistemi.Application` namespace'leri çakışır.

WPF kodunda tam niteleme zorunludur:
```csharp
// YANLIŞ
Application.Current.MainWindow...

// DOĞRU
System.Windows.Application.Current.MainWindow...
```

---

## Dialog Kullanım Kuralı

MessageBox asla kullanılmaz. Her zaman `IDialogService`:

```csharp
// Başarı
_dialogService.ShowSuccess("Başlık", "Mesaj");

// Hata
_dialogService.ShowError("Başlık", result.ErrorMessage);

// Onay (bool döner)
if (!_dialogService.ShowConfirmation("Silme Onayı", "Emin misiniz?"))
    return;
```

---

## Yetki Kontrolü Sırası

1. Handler seviyesinde kontrol (zorunlu).
2. UI seviyesinde görünürlük kontrolü (kullanıcı deneyimi için).

İkisi aynı anda uygulanmalıdır.

---

## DI Kayıt Yeri

- Handler'lar: `App.xaml.cs` (Scoped)
- Repository'ler: `Infrastructure/ServiceRegistration.cs` (Scoped)
- WPF-specific servisler: `App.xaml.cs` (Singleton)
- Domain/Application servisleri: `App.xaml.cs` (uygun lifetime)

---

## Commit Kuralı

CLAUDE.md'ye göre: commit yapmadan önce kullanıcı onayı istenir.

Build hedefi her commit öncesinde: **0 hata / 0 uyarı**.
