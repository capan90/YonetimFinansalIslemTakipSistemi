namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.CreateUser;

public class CreateUserResponse
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
