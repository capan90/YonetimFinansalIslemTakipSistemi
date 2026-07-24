using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using static YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.Import.WhatsAppImportColumnMap;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.Import;

/// <summary>
/// WhatsApp rehberi içe aktarma analizi. Telefon doğal anahtardır:
/// aktif kayıtta mevcut numara = kesin mükerrer (aktarılamaz),
/// soft-delete kayıtta mevcut numara = uyarı (import geri yükler — create akışıyla aynı).
/// Yetki: ortak rehber yazma kuralı (WhatsAppContactPermissions).
/// </summary>
public class AnalyzeWhatsAppImportHandler
{
    private readonly ICargoImportFileReader     _reader;
    private readonly IWhatsAppContactRepository _repository;
    private readonly IUserContext               _userContext;
    private readonly ISystemLogService          _systemLog;

    public AnalyzeWhatsAppImportHandler(
        ICargoImportFileReader     reader,
        IWhatsAppContactRepository repository,
        IUserContext               userContext,
        ISystemLogService          systemLog)
    {
        _reader      = reader;
        _repository  = repository;
        _userContext = userContext;
        _systemLog   = systemLog;
    }

    public async Task<OperationResult<WhatsAppImportAnalysisResult>> HandleAsync(AnalyzeWhatsAppImportRequest request)
    {
        if (!WhatsAppContactPermissions.CanModify(_userContext))
            return OperationResult<WhatsAppImportAnalysisResult>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        ImportDocument document;
        try
        {
            document = await _reader.ReadAsync(request.FilePath);
        }
        catch (ImportFileException ex)
        {
            return OperationResult<WhatsAppImportAnalysisResult>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync("WhatsAppImport", "WhatsApp içe aktarma dosyası okunamadı.", ex,
                source: nameof(AnalyzeWhatsAppImportHandler));
            return OperationResult<WhatsAppImportAnalysisResult>.Fail(
                "Dosya okunamadı. Teknik ayrıntı Sistem Loglarına kaydedildi.");
        }

        var match = MatchHeaders(document.Headers);
        if (match.MissingRequired.Count > 0)
            return OperationResult<WhatsAppImportAnalysisResult>.Fail(
                $"Dosyada zorunlu kolonlar eksik: {string.Join(", ", match.MissingRequired)}. " +
                "\"Şablon İndir\" ile doğru başlıklı şablonu kullanabilirsiniz.");

        // Mevcut rehber (soft delete dahil) bir kez yüklenir
        var existing = await _repository.GetAllForImportAsync();
        var existingByPhone = new Dictionary<string, Domain.Entities.WhatsAppContact>();
        foreach (var contact in existing)
            existingByPhone.TryAdd(contact.Phone, contact);

        var rows       = new List<WhatsAppImportRowDto>();
        var seenPhones = new Dictionary<string, int>();
        var skipped    = 0;
        var processed  = 0;

        foreach (var docRow in document.Rows)
        {
            processed++;
            request.Progress?.Report(new ImportProgress("Satırlar doğrulanıyor", processed, document.Rows.Count));

            if (docRow.IsEmpty) { skipped++; continue; }

            var row = BuildRow(docRow, match.Indexes);

            if (row.Messages.All(m => m.IsWarning) && row.NormalizedPhone is not null)
            {
                if (seenPhones.TryGetValue(row.NormalizedPhone, out var firstRow))
                {
                    row.DuplicateReason = new DuplicateReason
                    {
                        Kind             = DuplicateKind.ExactKeyInFile, // kesin anahtar: telefon
                        MatchedRowNumber = firstRow,
                        Description      = $"Aynı telefon numarası dosyada {firstRow}. satırda da var."
                    };
                }
                else if (existingByPhone.TryGetValue(row.NormalizedPhone, out var existingContact))
                {
                    if (!existingContact.IsDeleted)
                    {
                        row.DuplicateReason = new DuplicateReason
                        {
                            Kind        = DuplicateKind.ExactKeyInDatabase, // kesin anahtar: telefon
                            Description = $"Numara rehberde zaten kayıtlı: '{existingContact.FullName}'."
                        };
                    }
                    else
                    {
                        // Silinmiş kayıt: create akışıyla aynı davranış — geri yüklenir
                        row.ResurrectContactId = existingContact.Id;
                        row.AddWarning(Definition(Column.Telefon).Header,
                            $"Numara daha önce silinmiş bir kayıtta mevcut ('{existingContact.FullName}') — içe aktarılırsa geri yüklenir.");
                        seenPhones[row.NormalizedPhone] = row.RowNumber;
                    }
                }
                else
                {
                    seenPhones[row.NormalizedPhone] = row.RowNumber;
                }
            }

            row.ResolveStatus();
            rows.Add(row);
        }

        return OperationResult<WhatsAppImportAnalysisResult>.Ok(new WhatsAppImportAnalysisResult
        {
            SourceName       = document.SourceName,
            Rows             = rows,
            SkippedEmptyRows = skipped,
            IgnoredColumns   = match.ExtraHeaders
        });
    }

    private static WhatsAppImportRowDto BuildRow(
        ImportDocumentRow docRow, IReadOnlyDictionary<Column, int> indexes)
    {
        var row = new WhatsAppImportRowDto { RowNumber = docRow.RowNumber };

        string? Cell(Column column)
            => indexes.TryGetValue(column, out var i) && i < docRow.Cells.Count
                ? TextNormalizer.CollapseOrNull(docRow.Cells[i])
                : null;

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

        row.FullName = Text(Column.AdSoyad);
        if (row.FullName is null)
            row.AddError(Definition(Column.AdSoyad).Header, "Ad Soyad boş olamaz.");

        var phoneText = Cell(Column.Telefon);
        if (phoneText is null)
        {
            row.AddError(Definition(Column.Telefon).Header, "Telefon boş olamaz.");
        }
        else
        {
            row.NormalizedPhone = PhoneNumberNormalizer.NormalizeTr(phoneText);
            if (row.NormalizedPhone is null)
                row.AddError(Definition(Column.Telefon).Header,
                    $"Geçerli bir Türkiye cep numarası değil: '{phoneText}' (örn: 0532 123 45 67).");
        }

        row.Company     = Text(Column.Firma);
        row.Description = Text(Column.Aciklama);
        return row;
    }
}
