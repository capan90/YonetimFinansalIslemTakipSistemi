# Mimari

## Genel Bakış

Yönetim Finansal İşlem Takip Sistemi, şirket içi kullanım için geliştirilen Windows masaüstü uygulamasıdır.

## Sistem Yapısı

- İstemci uygulama: WPF
- Veritabanı: PostgreSQL
- Dağıtım: ClickOnce
- Kod editörü: VS Code
- AI destek: Claude Code

## Dağıtım Modeli

- Uygulama çalışan bilgisayarlarına kurulacaktır.
- Veritabanı tek merkezde tutulacaktır.
- Tüm istemciler merkezi PostgreSQL veritabanına bağlanacaktır.

## Katmanlar

### UI
WPF ekranları, view modeller ve kullanıcı etkileşimleri.

### Application
İş kuralları, servisler, kullanım senaryoları ve uygulama akışları.

### Domain
Entity yapıları, enumlar, sabitler ve temel iş modeli.

### Infrastructure
Database erişimi, repository implementasyonları, audit log, update ve dış servis bağlantıları.

## Mimari Kurallar

- UI katmanında SQL yazılmayacak.
- İş kuralları UI içinde tutulmayacak.
- Database erişimi Infrastructure katmanında olacak.
- Audit log kritik işlemlerde zorunlu olacak.
- Özel dialog sistemi merkezi servis yaklaşımıyla geliştirilecek.