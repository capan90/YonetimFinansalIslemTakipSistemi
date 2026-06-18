namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.CreateUser;

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    // Plaintext — handler IPasswordHasher ile hash'ler; DB'ye düz metin yazılmaz
    public string Password { get; set; } = string.Empty;
}
