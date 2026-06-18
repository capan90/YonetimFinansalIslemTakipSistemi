namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserRequest
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    // Boş bırakılırsa mevcut hash korunur; doluysa yeniden hashlenir
    public string? NewPassword { get; set; }
}
