using System.Globalization;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Queries.GetExchangeRates;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Commands.CreateOrUpdateExchangeRate;

public class CreateOrUpdateExchangeRateHandler
{
    private readonly IExchangeRateRepository _repository;
    private readonly IAuditLogService        _auditLogService;
    private readonly IUserContext            _userContext;

    public CreateOrUpdateExchangeRateHandler(
        IExchangeRateRepository repository,
        IAuditLogService        auditLogService,
        IUserContext            userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<ExchangeRateDto>> HandleAsync(
        CreateOrUpdateExchangeRateCommand command)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageExchangeRates))
            return OperationResult<ExchangeRateDto>.Fail(
                "Bu işlem için yetkiniz bulunmamaktadır.");

        var validationError = Validate(command);
        if (validationError is not null)
            return OperationResult<ExchangeRateDto>.Fail(validationError);

        var rateDateUtc = DateTime.SpecifyKind(command.RateDate.Date, DateTimeKind.Utc);
        var existing    = await _repository.GetByDateAndCurrencyAsync(rateDateUtc, command.CurrencyType);

        ExchangeRate entity;
        AuditAction  auditAction;
        string?      oldValues = null;

        if (existing is not null)
        {
            // Upsert: mevcut kaydı güncelle
            oldValues = FormatRate(existing.ForexBuying, existing.ForexSelling);

            existing.ForexBuying     = command.ForexBuying;
            existing.ForexSelling    = command.ForexSelling;
            existing.UpdatedAt       = DateTime.UtcNow;
            existing.UpdatedByUserId = _userContext.UserId;

            await _repository.UpdateAsync(existing);
            entity      = existing;
            auditAction = AuditAction.ExchangeRateUpdated;
        }
        else
        {
            entity = new ExchangeRate
            {
                Id              = Guid.NewGuid(),
                RateDate        = rateDateUtc,
                CurrencyType    = command.CurrencyType,
                ForexBuying     = command.ForexBuying,
                ForexSelling    = command.ForexSelling,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = _userContext.UserId,
            };

            await _repository.AddAsync(entity);
            auditAction = AuditAction.ExchangeRateCreated;
        }

        var newValues = FormatRate(entity.ForexBuying, entity.ForexSelling);
        await _auditLogService.WriteAsync(
            auditAction,
            _userContext.UserId,
            _userContext.FullName,
            "ExchangeRate", entity.Id,
            oldValues, newValues);

        return OperationResult<ExchangeRateDto>.Ok(MapToDto(entity));
    }

    private static string? Validate(CreateOrUpdateExchangeRateCommand command)
    {
        if (command.RateDate == default)
            return "Kur tarihi geçersiz.";

        if (command.RateDate.Date > DateTime.Today)
            return "Gelecek tarih için kur girilemez.";

        // TRY kabul edilmez — yalnızca yabancı para birimleri
        if (command.CurrencyType != CurrencyType.USD && command.CurrencyType != CurrencyType.EUR)
            return "Yalnızca USD ve EUR kurları girilebilir.";

        if (command.ForexBuying <= 0)
            return "Alış kuru sıfırdan büyük olmalıdır.";

        if (command.ForexSelling <= 0)
            return "Satış kuru sıfırdan büyük olmalıdır.";

        return null;
    }

    private static string FormatRate(decimal buying, decimal selling)
    {
        var culture = new CultureInfo("tr-TR");
        return $"Alış: {buying.ToString("N4", culture)} | Satış: {selling.ToString("N4", culture)}";
    }

    private static ExchangeRateDto MapToDto(ExchangeRate e) => new()
    {
        Id              = e.Id,
        RateDate        = e.RateDate,
        CurrencyType    = e.CurrencyType,
        CurrencyDisplay = e.CurrencyType == CurrencyType.USD ? "USD" : "EUR",
        ForexBuying     = e.ForexBuying,
        ForexSelling    = e.ForexSelling,
        CreatedAt       = e.CreatedAt,
        UpdatedAt       = e.UpdatedAt
    };
}
