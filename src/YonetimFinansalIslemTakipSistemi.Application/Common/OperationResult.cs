namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// Use case sonucunu taşır. Validation hatalarını exception yerine
/// dönüş değeriyle iletir; UI dialog tipini (Success/Error) buna göre belirler.
/// </summary>
public class OperationResult<T>
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public T? Data { get; }

    private OperationResult(bool success, T? data, string? errorMessage)
    {
        Success = success;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static OperationResult<T> Ok(T data) => new(true, data, null);

    public static OperationResult<T> Fail(string errorMessage) => new(false, default, errorMessage);
}
