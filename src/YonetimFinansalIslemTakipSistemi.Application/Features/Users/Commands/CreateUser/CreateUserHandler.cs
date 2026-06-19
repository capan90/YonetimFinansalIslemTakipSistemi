using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.CreateUser;

public class CreateUserHandler
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public CreateUserHandler(
        IUserRepository repository,
        IPasswordHasher hasher,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _hasher          = hasher;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<CreateUserResponse>> HandleAsync(CreateUserRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return OperationResult<CreateUserResponse>.Fail(error);

        // UserName benzersiz olmalı — case-insensitive kontrol
        var existing = await _repository.GetByUserNameAsync(request.UserName);
        if (existing is not null)
            return OperationResult<CreateUserResponse>.Fail("Bu kullanıcı adı zaten kullanılıyor.");

        var user = new User
        {
            Id           = Guid.NewGuid(),
            FullName     = request.FullName.Trim(),
            UserName     = request.UserName.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        await _repository.AddAsync(user);

        // Audit: yeni kullanıcı oluşturuldu
        var newValues = $"Kullanıcı Adı: {user.UserName} | Ad Soyad: {user.FullName}";
        await _auditLogService.WriteAsync(
            AuditAction.UserCreated,
            _userContext.UserId,
            _userContext.FullName,
            "User", user.Id,
            null, newValues);

        return OperationResult<CreateUserResponse>.Ok(new CreateUserResponse
        {
            Id        = user.Id,
            UserName  = user.UserName,
            FullName  = user.FullName,
            CreatedAt = user.CreatedAt
        });
    }

    private static string? Validate(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))  return "Ad Soyad boş olamaz.";
        if (string.IsNullOrWhiteSpace(request.UserName))  return "Kullanıcı adı boş olamaz.";
        if (string.IsNullOrWhiteSpace(request.Password))  return "Şifre boş olamaz.";
        return null;
    }
}
