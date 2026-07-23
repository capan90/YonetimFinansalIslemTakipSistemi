namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

/// <summary>
/// Format bağımsız ham içe aktarma belgesi: başlık satırı + veri satırları.
/// Excel, CSV, XML gibi kaynakların hepsi bu modele indirgenir; analiz ve
/// doğrulama katmanı kaynağın formatını hiçbir zaman bilmez.
/// </summary>
public sealed class ImportDocument
{
    /// <summary>Kaynak adı (örn. dosya adı) — raporlama ve audit için.</summary>
    public required string SourceName { get; init; }

    /// <summary>Başlık hücreleri, kaynaktaki sırayla.</summary>
    public required IReadOnlyList<string> Headers { get; init; }

    /// <summary>Veri satırları; boş satırlar da dahildir (analiz aşaması atlar).</summary>
    public required IReadOnlyList<ImportDocumentRow> Rows { get; init; }
}

/// <summary>Tek veri satırı. RowNumber kaynaktaki gerçek satır numarasıdır (hata raporları için).</summary>
public sealed class ImportDocumentRow
{
    public required int RowNumber { get; init; }

    /// <summary>Hücre metinleri, Headers ile aynı sırada. Eksik hücreler null.</summary>
    public required IReadOnlyList<string?> Cells { get; init; }

    public bool IsEmpty => Cells.All(string.IsNullOrWhiteSpace);
}

/// <summary>
/// Kaynak dosya okunamadığında (uzantı, boyut, bozuk içerik, satır limiti) fırlatılır.
/// Message kullanıcıya doğrudan gösterilebilir Türkçe metindir.
/// </summary>
public sealed class ImportFileException : Exception
{
    public ImportFileException(string message) : base(message) { }
    public ImportFileException(string message, Exception inner) : base(message, inner) { }
}
