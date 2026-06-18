# Veritabanı Notları

## Veritabanı Türü

- PostgreSQL

## Çalışma Modeli

- Geliştirme aşamasında local PostgreSQL kullanılacak.
- Canlı kullanımda merkezi PostgreSQL kullanılacak.
- Tüm kullanıcılar aynı veriyi görecek.

## Planlanan Ana Tablolar

- users
- roles
- user_permissions
- cash_accounts
- cash_transactions
- exchange_rates
- audit_logs

## Temel Kurallar

- Kullanıcılar yönetici tarafından eklenecek.
- İşlem sırasında kullanıcı kur girmeyecek.
- TL, USD ve EUR bakiyeleri ayrı tutulacak.
- Silme işlemleri izlenecek.
- Audit log tüm kritik hareketleri kaydedecek.

## Not

SQL scriptleri ilerleyen aşamada `database/` klasörü altında oluşturulacaktır.