namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Hassas değerleri şifreleme / çözme sözleşmesi.
/// Infrastructure'da Windows DPAPI ile implemente edilir.
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
