# Kimlik Doğrulama

## Genel Akış

```
LoginWindow → LoginViewModel.LoginCommand
  → ExecuteLoginAsync()
    → IAuthenticationService.AuthenticateAsync(username, password)
      → DatabaseAuthenticationService
        → UserRepository.GetByUsernameAsync()
        → BCrypt.Verify(password, hash)
        → AuthResult { Success, UserId, FullName, Permissions }
    → IUserSession.SetUser(userId, fullName, permissions)
    → LoginCompleted() [Func<Task>]
      → LocalUserPreferencesService.SaveLastUsernameAsync()
      → DialogResult = true
  → App.xaml.cs → MainWindow açılır
```

---

## IAuthenticationService

```csharp
public interface IAuthenticationService
{
    Task<AuthResult> AuthenticateAsync(string username, string password);
}
```

`DatabaseAuthenticationService` implementasyonu:
1. `UserRepository.GetByUsernameAsync(username)` → kullanıcı bulunamazsa fail
2. `user.IsActive == false` → fail
3. `BCrypt.Verify(password, user.PasswordHash)` → false ise fail
4. Kullanıcının tüm `UserPermission`'ları yüklenir
5. `AuthResult.Success` döner

---

## IUserContext ve IUserSession

İki ayrı interface, tek implementasyon:

```csharp
// Okuma — handler'lar ve view model'ler kullanır
public interface IUserContext
{
    int UserId { get; }
    string FullName { get; }
    bool HasPermission(PermissionType permission);
    bool IsAuthenticated { get; }
}

// Yazma — yalnızca login ve logout akışında kullanılır
public interface IUserSession
{
    void SetUser(int userId, string fullName, IEnumerable<PermissionType> permissions);
    void ClearUser();
}
```

`UserContext` her iki interface'i implement eder ve **singleton** olarak kayıt edilir.  
Giriş sırasında izinler DB'den yüklenir ve bellekte tutulur.

---

## BCrypt Parola Hash

- Kayıt: `BCrypt.HashPassword(plainPassword)`
- Doğrulama: `BCrypt.Verify(plainPassword, hash)`
- Hash veritabanında saklanır; plain-text şifre asla kaydedilmez

---

## Son Kullanıcı Adı Hatırlama

`ILocalUserPreferencesService` — şifre kesinlikle kaydedilmez, yalnızca kullanıcı adı.

**Dosya:** `%AppData%\YonetimFinansalIslemTakipSistemi\user-preferences.json`

```json
{ "LastUsername": "murat" }
```

`LoginWindow.Loaded` → `RestoreLastUsernameAsync()` → kullanıcı adı varsa doldurulur, odak şifre kutusuna verilir.

---

## Çoklu Oturum — Logout

`App.xaml.cs`'te `while` döngüsü: kullanıcı çıkış yaptığında `LoginWindow` tekrar gösterilir, `MainWindow` kapatılır. `ShutdownMode.OnExplicitShutdown` kullanılır.

DI scope: `LoginWindow` kapandığında eski scope dispose edilir; yeni login için yeni scope açılır. Bu sayede `DbContext` bir önceki kullanıcının verisi ile kirletilmez.

---

## Login Ekranı Özellikleri

- Logo (`LoginIcon.png`, pack URI)
- "Hoş geldiniz" başlığı
- Versiyon: ClickOnce ortamında assembly versiyonu (`v1.0.0.21`), dev ortamında "Versiyon: Development"
- Kullanıcı adı ve şifre alanları (emoji ikon + placeholder)
- Son kullanıcı adı otomatik doldurma
- Hata mesajı gösterimi (ViewModel.ErrorMessage)
