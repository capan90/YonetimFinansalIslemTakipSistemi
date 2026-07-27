namespace YonetimFinansalIslemTakipSistemi.Tests.Integration;

/// <summary>
/// Canlı PostgreSQL'e karşı çalışan integration testleri aynı collection'da toplar.
/// xUnit collection'ları varsayılan olarak PARALEL çalıştırır; iki cargo integration
/// sınıfı aynı tabloları (cargo_shipments, cargo_number_counters) eşzamanlı kullanınca
/// sayaç/sayım assert'leri ara sıra karışıyordu (flaky). Aynı collection + paralelsizleştirme
/// ile bu sınıflar seri çalışır; birbirlerinin DB durumunu bozmaz.
/// </summary>
[CollectionDefinition("LiveDatabase", DisableParallelization = true)]
public class LiveDatabaseCollection { }
