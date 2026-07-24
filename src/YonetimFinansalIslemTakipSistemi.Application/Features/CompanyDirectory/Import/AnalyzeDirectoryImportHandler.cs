using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using static YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Import.DirectoryImportColumnMap;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Import;

/// <summary>
/// Firma rehberi içe aktarma analizi: kolon eşleme, satır doğrulama ve mükerrer
/// tespiti (normalize firma adı — dosya içi + veritabanı). DB'ye yazmaz.
/// Adres bilinçli olarak zorunlu DEĞİLDİR: mevcut telefon rehberi taşımasında
/// adres verisi yoktur; boş adres import'ta "-" olarak kaydedilir.
/// </summary>
public class AnalyzeDirectoryImportHandler
{
    private readonly ICargoImportFileReader      _reader;
    private readonly ICompanyDirectoryRepository _repository;
    private readonly IUserContext                _userContext;
    private readonly ISystemLogService           _systemLog;
    private readonly IUserTextNormalizationService _textNormalization;

    public AnalyzeDirectoryImportHandler(
        ICargoImportFileReader      reader,
        ICompanyDirectoryRepository repository,
        IUserContext                userContext,
        ISystemLogService           systemLog,
        IUserTextNormalizationService textNormalization)
    {
        _reader            = reader;
        _repository        = repository;
        _userContext       = userContext;
        _systemLog         = systemLog;
        _textNormalization = textNormalization;
    }

    public async Task<OperationResult<DirectoryImportAnalysisResult>> HandleAsync(AnalyzeDirectoryImportRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageCompanyDirectory))
            return OperationResult<DirectoryImportAnalysisResult>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        ImportDocument document;
        try
        {
            document = await _reader.ReadAsync(request.FilePath);
        }
        catch (ImportFileException ex)
        {
            return OperationResult<DirectoryImportAnalysisResult>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync("DirectoryImport", "Rehber içe aktarma dosyası okunamadı.", ex,
                source: nameof(AnalyzeDirectoryImportHandler));
            return OperationResult<DirectoryImportAnalysisResult>.Fail(
                "Dosya okunamadı. Teknik ayrıntı Sistem Loglarına kaydedildi.");
        }

        var match = MatchHeaders(document.Headers);
        if (match.MissingRequired.Count > 0)
            return OperationResult<DirectoryImportAnalysisResult>.Fail(
                $"Dosyada zorunlu kolonlar eksik: {string.Join(", ", match.MissingRequired)}. " +
                "\"Şablon İndir\" ile doğru başlıklı şablonu kullanabilirsiniz.");

        // Mevcut rehber bir kez yüklenir — mükerrer kontrolü bellekte yapılır.
        // Anahtar: ad + telefon (aynı firmanın farklı numaraları ayrı kayıt olabilir)
        var existing = await _repository.GetAllAsync();
        var existingByKey = new Dictionary<string, string>();
        foreach (var dir in existing)
        {
            var key = DirectoryDuplicateKey.Build(dir.CompanyName, dir.Phone);
            if (!key.StartsWith('|')) existingByKey.TryAdd(key, dir.CompanyName);
        }

        var rows      = new List<DirectoryImportRowDto>();
        var seenNames = new Dictionary<string, int>();
        var skipped   = 0;
        var processed = 0;

        foreach (var docRow in document.Rows)
        {
            processed++;
            request.Progress?.Report(new ImportProgress("Satırlar doğrulanıyor", processed, document.Rows.Count));

            if (docRow.IsEmpty) { skipped++; continue; }

            var row = BuildRow(docRow, match.Indexes);

            // Mükerrer: firma adı + telefon (yalnızca hatasız satırlar için).
            // Aynı ad + FARKLI numara geçerlidir — bir firmanın birden çok hattı olabilir.
            if (row.Messages.All(m => m.IsWarning) && row.CompanyName is not null)
            {
                var key = DirectoryDuplicateKey.Build(row.CompanyName, row.Phone);
                if (seenNames.TryGetValue(key, out var firstRow))
                {
                    row.DuplicateReason = new DuplicateReason
                    {
                        Kind             = DuplicateKind.SimilarInFile,
                        MatchedRowNumber = firstRow,
                        Description      = $"Aynı firma adı ve telefon dosyada {firstRow}. satırda da var."
                    };
                }
                else if (existingByKey.TryGetValue(key, out var existingName))
                {
                    row.DuplicateReason = new DuplicateReason
                    {
                        Kind        = DuplicateKind.SimilarInDatabase,
                        Description = $"Firma aynı telefonla rehberde zaten kayıtlı: '{existingName}'."
                    };
                }
                else
                {
                    seenNames[key] = row.RowNumber;
                }
            }

            row.ResolveStatus();
            rows.Add(row);
        }

        return OperationResult<DirectoryImportAnalysisResult>.Ok(new DirectoryImportAnalysisResult
        {
            SourceName       = document.SourceName,
            Rows             = rows,
            SkippedEmptyRows = skipped,
            IgnoredColumns   = match.ExtraHeaders
        });
    }

    private DirectoryImportRowDto BuildRow(
        ImportDocumentRow docRow, IReadOnlyDictionary<Column, int> indexes)
    {
        var row = new DirectoryImportRowDto { RowNumber = docRow.RowNumber };

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

        // Kullanıcının harf duyarlılığı tercihi analizde uygulanır — önizleme,
        // verinin KAYDEDİLECEK halini gösterir (telefon/e-posta/posta kodu muaf)
        string? CaseText(Column column)
        {
            var value = _textNormalization.Normalize(Cell(column));
            var def   = Definition(column);
            if (value is not null && value.Length > def.MaxLength)
            {
                row.AddWarning(def.Header, $"Değer {def.MaxLength} karakterle sınırlandırıldı.");
                value = value[..def.MaxLength];
            }
            return value;
        }

        row.CompanyName = CaseText(Column.FirmaAdi);
        if (row.CompanyName is null)
            row.AddError(Definition(Column.FirmaAdi).Header, "Firma adı boş olamaz.");

        row.ContactPerson = CaseText(Column.YetkiliKisi);
        row.AttentionTo   = CaseText(Column.Dikkatine);
        row.AddressLine   = CaseText(Column.Adres);
        row.District      = CaseText(Column.Ilce);
        row.City          = CaseText(Column.Il);
        row.PostalCode    = Text(Column.PostaKodu);
        row.Email         = Text(Column.Eposta);
        row.Notes         = CaseText(Column.Not);

        // Telefon: 50 karakteri aşan karışık içerik (çoklu numara + açıklama) kaybedilmez —
        // Not alanına taşınır, telefon boş bırakılır (mevcut rehber dosyası bu durumu üretiyor)
        var phone = Cell(Column.Telefon);
        if (phone is not null && phone.Length > Definition(Column.Telefon).MaxLength)
        {
            row.AddWarning(Definition(Column.Telefon).Header,
                "Telefon 50 karakteri aştığı için Not alanına taşındı.");
            row.Notes = row.Notes is null ? $"Tel: {phone}" : $"{row.Notes} | Tel: {phone}";
            if (row.Notes.Length > 1000) row.Notes = row.Notes[..1000];
        }
        else
        {
            row.Phone = phone;
        }

        // E-posta: kaba biçim kontrolü — hatalıysa uyarı, alan yine de aktarılır
        if (row.Email is not null && !row.Email.Contains('@'))
            row.AddWarning(Definition(Column.Eposta).Header, "E-posta biçimi şüpheli görünüyor.");

        return row;
    }
}
