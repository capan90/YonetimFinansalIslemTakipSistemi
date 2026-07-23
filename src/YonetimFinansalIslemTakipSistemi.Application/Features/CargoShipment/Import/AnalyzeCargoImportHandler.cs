using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using static YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import.CargoImportColumnMap;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

/// <summary>
/// İçe aktarma analiz aşaması: belgeyi okur, kolonları eşler, satırları doğrular,
/// firmaları çözümler ve mükerrerleri işaretler. VERİTABANINA YAZMAZ — çıktısı
/// önizleme ekranının veri kaynağıdır. Satır hataları akışı kesmez; kullanıcı
/// tüm sorunları tek seferde görür.
/// </summary>
public class AnalyzeCargoImportHandler
{
    private readonly ICargoImportFileReader      _reader;
    private readonly ICompanyDirectoryRepository _directoryRepository;
    private readonly ICargoCompanyRepository     _cargoCompanyRepository;
    private readonly ICargoShipmentRepository    _shipmentRepository;
    private readonly IUserContext                _userContext;
    private readonly ISystemLogService           _systemLog;

    public AnalyzeCargoImportHandler(
        ICargoImportFileReader      reader,
        ICompanyDirectoryRepository directoryRepository,
        ICargoCompanyRepository     cargoCompanyRepository,
        ICargoShipmentRepository    shipmentRepository,
        IUserContext                userContext,
        ISystemLogService           systemLog)
    {
        _reader                 = reader;
        _directoryRepository    = directoryRepository;
        _cargoCompanyRepository = cargoCompanyRepository;
        _shipmentRepository     = shipmentRepository;
        _userContext            = userContext;
        _systemLog              = systemLog;
    }

    public async Task<OperationResult<CargoImportAnalysisResult>> HandleAsync(AnalyzeCargoImportRequest request)
    {
        // Analiz de rehber/kayıt verisi okur — create ile aynı izinle korunur
        var requiredPermission = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(requiredPermission))
            return OperationResult<CargoImportAnalysisResult>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        // 1) Dosya seviyesi — hata varsa önizleme açılmaz
        ImportDocument document;
        try
        {
            document = await _reader.ReadAsync(request.FilePath);
        }
        catch (ImportFileException ex)
        {
            return OperationResult<CargoImportAnalysisResult>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync("CargoImport", "İçe aktarma dosyası okunamadı.", ex,
                source: nameof(AnalyzeCargoImportHandler));
            return OperationResult<CargoImportAnalysisResult>.Fail(
                "Dosya okunamadı. Teknik ayrıntı Sistem Loglarına kaydedildi.");
        }

        // 2) Kolon eşleme — eksik zorunlu kolon varsa süreç durur
        var match = MatchHeaders(document.Headers);
        if (match.MissingRequired.Count > 0)
            return OperationResult<CargoImportAnalysisResult>.Fail(
                $"Dosyada zorunlu kolonlar eksik: {string.Join(", ", match.MissingRequired)}. " +
                "\"Şablon İndir\" ile doğru başlıklı şablonu kullanabilirsiniz.");

        // 3) Firma sözlükleri bir kez yüklenir — satır başına DB sorgusu yapılmaz
        var directories    = await _directoryRepository.GetAllAsync();
        var cargoCompanies = await _cargoCompanyRepository.GetAllAsync();

        var directoryResolver = new CompanyNameResolver(
            directories.Select(d => new CompanyNameResolver.Entry(d.Id, d.CompanyName, d.IsActive)));
        var cargoResolver = new CompanyNameResolver(
            cargoCompanies.Select(c => new CompanyNameResolver.Entry(c.Id, c.Name, c.IsActive)));
        var directoryById = directories.ToDictionary(d => d.Id);

        // 4) Satır seviyesi doğrulama
        var rows    = new List<CargoImportRowDto>();
        var skipped = 0;
        var total   = document.Rows.Count;
        var processed = 0;

        foreach (var docRow in document.Rows)
        {
            processed++;
            request.Progress?.Report(new ImportProgress("Satırlar doğrulanıyor", processed, total));

            if (docRow.IsEmpty) { skipped++; continue; }

            rows.Add(BuildRow(docRow, match.Indexes, directoryResolver, cargoResolver, directoryById));
        }

        // 5) Mükerrer tespiti — dosya içi + veritabanı
        await DetectDuplicatesAsync(rows, request.Direction);

        foreach (var row in rows)
            row.ResolveStatus();

        return OperationResult<CargoImportAnalysisResult>.Ok(new CargoImportAnalysisResult
        {
            SourceName       = document.SourceName,
            Direction        = request.Direction,
            Rows             = rows,
            SkippedEmptyRows = skipped,
            IgnoredColumns   = match.ExtraHeaders
        });
    }

    private static CargoImportRowDto BuildRow(
        ImportDocumentRow docRow,
        IReadOnlyDictionary<Column, int> indexes,
        CompanyNameResolver directoryResolver,
        CompanyNameResolver cargoResolver,
        IReadOnlyDictionary<Guid, Domain.Entities.CompanyDirectory> directoryById)
    {
        var row = new CargoImportRowDto { RowNumber = docRow.RowNumber };

        string? Cell(Column column)
            => indexes.TryGetValue(column, out var i) && i < docRow.Cells.Count
                ? TextNormalizer.CollapseOrNull(docRow.Cells[i])
                : null;

        // Metin alanı: max uzunluk aşılırsa kesilir + uyarı (kayıt kaybı yerine bilinçli kısaltma)
        string? Text(Column column)
        {
            var value = Cell(column);
            var def   = Definition(column);
            if (value is not null && value.Length > def.MaxLength)
            {
                row.AddWarning(def.Header, $"Değer {def.MaxLength} karakterle sınırlandırıldı.");
                value = value[..def.MaxLength];
            }
            return value;
        }

        // Tarih — zorunlu
        var dateText = Cell(Column.Tarih);
        var date     = ParseDate(dateText);
        if (date is null)
            row.AddError(Definition(Column.Tarih).Header, dateText is null
                ? "Tarih boş olamaz."
                : $"Tarih anlaşılamadı: '{dateText}'. Beklenen biçim: 31.12.2026");
        else
        {
            row.ShipmentDate = date.Value;
            // Manuel akışta ileri tarih engeli yok — içe aktarmada yazım hatası olasılığına karşı uyarı
            if (date.Value.Date > DateTime.Today)
                row.AddWarning(Definition(Column.Tarih).Header, "Tarih bugünden ileri — kontrol edin.");
        }

        // Firma — zorunlu, rehberden çözümlenir
        var companyText = Cell(Column.Firma);
        if (companyText is null)
            row.AddError(Definition(Column.Firma).Header, "Firma boş olamaz.");
        else
        {
            var result = directoryResolver.Resolve(companyText);
            switch (result.Kind)
            {
                case CompanyNameResolver.MatchKind.Single:
                    var dir = directoryById[result.Match!.Id];
                    row.CompanyDirectoryId = dir.Id;
                    row.CompanyName        = dir.CompanyName;
                    // Snapshot alanları manuel akıştaki gibi rehberden kopyalanır
                    row.ReceiverCompanyNameSnapshot = dir.CompanyName;
                    row.ReceiverAddressSnapshot     = dir.AddressLine;
                    row.ReceiverCitySnapshot        = dir.City;
                    row.ReceiverDistrictSnapshot    = dir.District;
                    row.ReceiverPhoneSnapshot       = dir.Phone;
                    row.ReceiverEmailSnapshot       = dir.Email;
                    row.ReceiverAttentionSnapshot   = dir.AttentionTo;
                    break;
                case CompanyNameResolver.MatchKind.Ambiguous:
                    row.AddError(Definition(Column.Firma).Header,
                        $"'{companyText}' rehberde birden fazla firmayla eşleşti — rehberde adları ayrıştırın.");
                    break;
                case CompanyNameResolver.MatchKind.InactiveOnly:
                    row.AddError(Definition(Column.Firma).Header,
                        $"'{companyText}' rehberde pasif durumda — önce firmayı aktifleştirin.");
                    break;
                default:
                    row.AddError(Definition(Column.Firma).Header,
                        result.Suggestion is null
                            ? $"Firma rehberde bulunamadı: '{companyText}'."
                            : $"Firma rehberde bulunamadı: '{companyText}'. Bunu mu demek istediniz: '{result.Suggestion}'?");
                    break;
            }
        }

        // Kargo firması — opsiyonel; yazıldıysa doğru olmalı (sessizce düşürülmez)
        var cargoText = Cell(Column.KargoFirmasi);
        if (cargoText is not null)
        {
            var result = cargoResolver.Resolve(cargoText);
            switch (result.Kind)
            {
                case CompanyNameResolver.MatchKind.Single:
                    row.CargoCompanyId   = result.Match!.Id;
                    row.CargoCompanyName = result.Match.Name;
                    break;
                case CompanyNameResolver.MatchKind.Ambiguous:
                    row.AddError(Definition(Column.KargoFirmasi).Header,
                        $"'{cargoText}' birden fazla kargo firmasıyla eşleşti.");
                    break;
                case CompanyNameResolver.MatchKind.InactiveOnly:
                    row.AddError(Definition(Column.KargoFirmasi).Header,
                        $"Kargo firması '{cargoText}' pasif durumda.");
                    break;
                default:
                    row.AddError(Definition(Column.KargoFirmasi).Header,
                        result.Suggestion is null
                            ? $"Kargo firması bulunamadı: '{cargoText}'."
                            : $"Kargo firması bulunamadı: '{cargoText}'. Bunu mu demek istediniz: '{result.Suggestion}'?");
                    break;
            }
        }

        // Gönderi türü — opsiyonel; tanınmayan etiket uyarı + boş
        var typeText = Cell(Column.GonderiTuru);
        if (typeText is not null)
        {
            row.ShipmentType = ParseShipmentType(typeText);
            if (row.ShipmentType is null)
                row.AddWarning(Definition(Column.GonderiTuru).Header,
                    $"Gönderi türü tanınmadı: '{typeText}' — boş bırakıldı. Geçerli değerler: Evrak, Numune, Fatura, Sözleşme, Yedek Parça, Diğer.");
        }

        // Öncelik — opsiyonel; tanınmayan etiket uyarı + Normal
        var priorityText = Cell(Column.Oncelik);
        if (priorityText is not null)
        {
            var priority = ParsePriority(priorityText);
            if (priority is null)
                row.AddWarning(Definition(Column.Oncelik).Header,
                    $"Öncelik tanınmadı: '{priorityText}' — Normal kabul edildi. Geçerli değerler: Normal, Orta, Acil, Çok Acil.");
            row.Priority = priority ?? CargoShipmentPriority.Normal;
        }

        row.SenderName     = Text(Column.Gonderen);
        row.ReceiverName   = Text(Column.Alici);
        row.TrackingNumber = Text(Column.TakipNo);
        row.VehiclePlate   = Text(Column.AracPlakasi);
        row.Notes          = Text(Column.Not);

        // Dikkatine kolonu doluysa rehberdeki varsayılanın önüne geçer
        var attention = Text(Column.Dikkatine);
        if (attention is not null)
            row.ReceiverAttentionSnapshot = attention;

        return row;
    }

    /// <summary>
    /// Mükerrer anahtarları:
    ///  - Kesin:  Yön + Takip No (normalize) — dosya içi ve veritabanı
    ///  - Olası:  Yön + Tarih + Firma + Kargo Firması — dosya içi ve veritabanı
    /// Hatalı satırlar mükerrer kontrolüne girmez (Error durumu baskındır).
    /// </summary>
    private async Task DetectDuplicatesAsync(List<CargoImportRowDto> rows, CargoShipmentDirection direction)
    {
        var candidates = rows.Where(r => r.Messages.All(m => m.IsWarning)).ToList();
        if (candidates.Count == 0) return;

        static string? TrackingKey(string? trackingNumber)
            => string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim().ToUpperInvariant();

        static string SimilarKey(DateTime date, Guid? directoryId, string? companyName, Guid? cargoCompanyId)
            => $"{date:yyyyMMdd}|{directoryId?.ToString() ?? CompanyNameResolver.Normalize(companyName)}|{cargoCompanyId}";

        // ── Dosya içi ──
        var seenTracking = new Dictionary<string, int>();
        var seenSimilar  = new Dictionary<string, int>();

        foreach (var row in candidates)
        {
            var tracking = TrackingKey(row.TrackingNumber);
            if (tracking is not null)
            {
                if (seenTracking.TryGetValue(tracking, out var firstRow))
                {
                    row.DuplicateReason = new DuplicateReason
                    {
                        Kind             = DuplicateKind.TrackingNumberInFile,
                        MatchedRowNumber = firstRow,
                        Description      = $"Aynı takip numarası dosyada {firstRow}. satırda da var."
                    };
                    continue; // kesin mükerrer — benzerlik kontrolüne gerek yok
                }
                seenTracking[tracking] = row.RowNumber;
            }

            var similar = SimilarKey(row.ShipmentDate, row.CompanyDirectoryId, row.CompanyName, row.CargoCompanyId);
            if (seenSimilar.TryGetValue(similar, out var firstSimilarRow))
            {
                row.DuplicateReason = new DuplicateReason
                {
                    Kind             = DuplicateKind.SimilarInFile,
                    MatchedRowNumber = firstSimilarRow,
                    Description      = $"Aynı tarih + firma + kargo firması dosyada {firstSimilarRow}. satırda da var."
                };
            }
            else
            {
                seenSimilar[similar] = row.RowNumber;
            }
        }

        // ── Veritabanı ──
        var pending = candidates.Where(r => r.DuplicateReason is null).ToList();
        if (pending.Count == 0) return;

        // Kesin: takip numarası eşleşmesi
        var trackingNumbers = pending
            .Select(r => TrackingKey(r.TrackingNumber))
            .Where(t => t is not null)
            .Cast<string>()
            .Distinct()
            .ToList();

        if (trackingNumbers.Count > 0)
        {
            var dbByTracking = (await _shipmentRepository.GetByTrackingNumbersAsync(direction, trackingNumbers))
                .Where(s => TrackingKey(s.TrackingNumber) is not null)
                .GroupBy(s => TrackingKey(s.TrackingNumber)!)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var row in pending)
            {
                var tracking = TrackingKey(row.TrackingNumber);
                if (tracking is not null && dbByTracking.TryGetValue(tracking, out var existing))
                    row.DuplicateReason = new DuplicateReason
                    {
                        Kind                  = DuplicateKind.TrackingNumberInDatabase,
                        MatchedShipmentNumber = existing.ShipmentNumber,
                        Description           = $"Aynı takip numarası {existing.ShipmentNumber ?? "kayıtlı bir gönderi"}de mevcut."
                    };
            }
        }

        // Olası: tarih aralığı sorgusu — tüm tablo değil, yalnızca dosyanın kapsadığı aralık
        pending = pending.Where(r => r.DuplicateReason is null && r.ShipmentDate != default).ToList();
        if (pending.Count == 0) return;

        var minDate = DateTime.SpecifyKind(pending.Min(r => r.ShipmentDate).Date, DateTimeKind.Utc);
        var maxDate = DateTime.SpecifyKind(pending.Max(r => r.ShipmentDate).Date, DateTimeKind.Utc);
        var dbRows  = await _shipmentRepository.GetActiveForImportCheckAsync(direction, minDate, maxDate);

        var dbSimilar = new Dictionary<string, Domain.Entities.CargoShipment>();
        foreach (var db in dbRows)
        {
            var key = SimilarKey(db.ShipmentDate, db.CompanyDirectoryId, db.ReceiverCompanyNameSnapshot, db.CargoCompanyId);
            dbSimilar.TryAdd(key, db);
        }

        foreach (var row in pending)
        {
            var key = SimilarKey(row.ShipmentDate, row.CompanyDirectoryId, row.CompanyName, row.CargoCompanyId);
            if (dbSimilar.TryGetValue(key, out var existing))
                row.DuplicateReason = new DuplicateReason
                {
                    Kind                  = DuplicateKind.SimilarInDatabase,
                    MatchedShipmentNumber = existing.ShipmentNumber,
                    Description           = $"Aynı tarih + firma + kargo firması {existing.ShipmentNumber ?? "kayıtlı bir gönderi"}de mevcut."
                };
        }
    }
}
