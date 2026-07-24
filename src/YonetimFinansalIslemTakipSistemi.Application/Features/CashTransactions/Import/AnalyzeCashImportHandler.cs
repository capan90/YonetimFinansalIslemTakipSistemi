using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using static YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Import.CashImportColumnMap;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Import;

/// <summary>
/// Finans içe aktarma analizi: GİREN/ÇIKAN kolonlarından tür+tutar türetilir,
/// CreateCashTransactionHandler'ın validasyon kuralları birebir uygulanır
/// (tutar &gt; 0, ileri tarih yok, açıklama zorunlu). DB'ye yazmaz.
/// Mükerrer: tarih+tür+para birimi+tutar+açıklama (olası — dahil edilebilir).
/// </summary>
public class AnalyzeCashImportHandler
{
    private readonly ICargoImportFileReader     _reader;
    private readonly ICashTransactionRepository _repository;
    private readonly IUserContext               _userContext;
    private readonly ISystemLogService          _systemLog;
    private readonly IUserTextNormalizationService _textNormalization;

    public AnalyzeCashImportHandler(
        ICargoImportFileReader     reader,
        ICashTransactionRepository repository,
        IUserContext               userContext,
        ISystemLogService          systemLog,
        IUserTextNormalizationService textNormalization)
    {
        _reader            = reader;
        _repository        = repository;
        _userContext       = userContext;
        _systemLog         = systemLog;
        _textNormalization = textNormalization;
    }

    public async Task<OperationResult<CashImportAnalysisResult>> HandleAsync(AnalyzeCashImportRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanCreateTransaction))
            return OperationResult<CashImportAnalysisResult>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        ImportDocument document;
        try
        {
            document = await _reader.ReadAsync(request.FilePath);
        }
        catch (ImportFileException ex)
        {
            return OperationResult<CashImportAnalysisResult>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync("CashImport", "Finans içe aktarma dosyası okunamadı.", ex,
                source: nameof(AnalyzeCashImportHandler));
            return OperationResult<CashImportAnalysisResult>.Fail(
                "Dosya okunamadı. Teknik ayrıntı Sistem Loglarına kaydedildi.");
        }

        var match = MatchHeaders(document.Headers);
        if (match.MissingRequired.Count > 0)
            return OperationResult<CashImportAnalysisResult>.Fail(
                $"Dosyada zorunlu kolonlar eksik: {string.Join(", ", match.MissingRequired)}. " +
                "\"Şablon İndir\" ile doğru başlıklı şablonu kullanabilirsiniz.");

        var rows      = new List<CashImportRowDto>();
        var seenKeys  = new Dictionary<string, int>();
        var skipped   = 0;
        var processed = 0;

        foreach (var docRow in document.Rows)
        {
            processed++;
            request.Progress?.Report(new ImportProgress("Satırlar doğrulanıyor", processed, document.Rows.Count));

            if (docRow.IsEmpty) { skipped++; continue; }

            var row = BuildRow(docRow, match.Indexes);

            if (row.Messages.All(m => m.IsWarning))
            {
                if (seenKeys.TryGetValue(row.DuplicateKey, out var firstRow))
                {
                    row.DuplicateReason = new DuplicateReason
                    {
                        Kind             = DuplicateKind.SimilarInFile,
                        MatchedRowNumber = firstRow,
                        Description      = $"Aynı tarih/tür/tutar/açıklama dosyada {firstRow}. satırda da var."
                    };
                }
                else
                {
                    seenKeys[row.DuplicateKey] = row.RowNumber;
                }
            }

            rows.Add(row);
        }

        // Veritabanı mükerrer kontrolü: yalnızca dosyanın kapsadığı tarih aralığı çekilir
        await DetectDatabaseDuplicatesAsync(rows);

        foreach (var row in rows)
            row.ResolveStatus();

        return OperationResult<CashImportAnalysisResult>.Ok(new CashImportAnalysisResult
        {
            SourceName       = document.SourceName,
            Rows             = rows,
            SkippedEmptyRows = skipped,
            IgnoredColumns   = match.ExtraHeaders
        });
    }

    private CashImportRowDto BuildRow(ImportDocumentRow docRow, IReadOnlyDictionary<Column, int> indexes)
    {
        var row = new CashImportRowDto { RowNumber = docRow.RowNumber };

        string? Cell(Column column)
            => indexes.TryGetValue(column, out var i) && i < docRow.Cells.Count
                ? TextNormalizer.CollapseOrNull(docRow.Cells[i])
                : null;

        // Tarih — zorunlu; ileri tarih finans kuralı gereği HATA (CreateCashTransactionHandler ile aynı)
        var dateText = Cell(Column.Tarih);
        var date     = CargoImportColumnMap.ParseDate(dateText);
        if (date is null)
            row.AddError(Definition(Column.Tarih).Header, dateText is null
                ? "Tarih boş olamaz."
                : $"Tarih anlaşılamadı: '{dateText}'. Beklenen biçim: 31.12.2026");
        else if (date.Value.Date > DateTime.Today)
            row.AddError(Definition(Column.Tarih).Header, "İşlem tarihi bugünden ileri olamaz.");
        else
            row.TransactionDate = date.Value.Date;

        // GİREN/ÇIKAN: tam biri dolu olmalı → tür + tutar
        var girenText = Cell(Column.Giren);
        var cikanText = Cell(Column.Cikan);

        if (girenText is null && cikanText is null)
        {
            row.AddError("Giren/Çıkan", "Giren veya Çıkan kolonlarından biri dolu olmalıdır.");
        }
        else if (girenText is not null && cikanText is not null)
        {
            row.AddError("Giren/Çıkan", "Giren ve Çıkan aynı satırda birlikte dolu olamaz.");
        }
        else
        {
            row.TransactionType = girenText is not null ? TransactionType.Giris : TransactionType.Cikis;
            var amountText = girenText ?? cikanText;
            var amount     = ParseAmount(amountText);

            if (amount is null)
                row.AddError(girenText is not null ? "Giren" : "Çıkan",
                    $"Tutar anlaşılamadı: '{amountText}'.");
            else if (amount <= 0)
                row.AddError(girenText is not null ? "Giren" : "Çıkan",
                    "Tutar sıfırdan büyük olmalıdır.");
            else
                row.Amount = amount.Value;
        }

        // Para birimi — boş/eksik kolon → TL; tanınmayan etiket → hata (para konusu sessiz varsayılmaz)
        var currencyText = Cell(Column.ParaBirimi);
        var currency     = ParseCurrency(currencyText);
        if (currency is null)
            row.AddError(Definition(Column.ParaBirimi).Header,
                $"Para birimi tanınmadı: '{currencyText}'. Geçerli değerler: TL, USD, EUR.");
        else
            row.CurrencyType = currency.Value;

        // Açıklama — zorunlu; İlgili Kişi doluysa açıklamaya eklenir; harf tercihi uygulanır
        var description = Cell(Column.Aciklama);
        var person      = Cell(Column.IlgiliKisi);
        if (description is null)
        {
            row.AddError(Definition(Column.Aciklama).Header,
                "Açıklama alanı zorunludur. Lütfen işlem açıklaması giriniz.");
        }
        else
        {
            var combined = person is null ? description : $"{description} — {person}";
            combined = _textNormalization.Normalize(combined);
            var max = Definition(Column.Aciklama).MaxLength;
            if (combined!.Length > max)
            {
                row.AddWarning(Definition(Column.Aciklama).Header, $"Açıklama {max} karakterle sınırlandırıldı.");
                combined = combined[..max];
            }
            row.Description = combined;
        }

        return row;
    }

    private async Task DetectDatabaseDuplicatesAsync(List<CashImportRowDto> rows)
    {
        var candidates = rows.Where(r => r.Messages.All(m => m.IsWarning) && r.DuplicateReason is null).ToList();
        if (candidates.Count == 0) return;

        var minDate = DateTime.SpecifyKind(candidates.Min(r => r.TransactionDate), DateTimeKind.Utc);
        var maxDate = DateTime.SpecifyKind(candidates.Max(r => r.TransactionDate), DateTimeKind.Utc);
        var existing = await _repository.GetFilteredAsync(minDate, maxDate, null, null);

        var dbKeys = new HashSet<string>();
        foreach (var t in existing)
        {
            var key = new CashImportRowDto
            {
                RowNumber = 0, TransactionDate = t.TransactionDate.Date,
                TransactionType = t.TransactionType, CurrencyType = t.CurrencyType,
                Amount = t.Amount, Description = t.Description
            }.DuplicateKey;
            dbKeys.Add(key);
        }

        foreach (var row in candidates)
            if (dbKeys.Contains(row.DuplicateKey))
                row.DuplicateReason = new DuplicateReason
                {
                    Kind        = DuplicateKind.SimilarInDatabase,
                    Description = "Aynı tarih/tür/tutar/açıklamalı işlem sistemde zaten kayıtlı."
                };
    }
}
