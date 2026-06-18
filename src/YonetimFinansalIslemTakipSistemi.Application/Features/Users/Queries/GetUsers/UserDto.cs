namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Kullanıcı listesi için düz read model. PasswordHash asla taşınmaz.
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
