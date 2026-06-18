namespace YonetimFinansalIslemTakipSistemi.Domain.Common;

/// <summary>
/// Tüm temel entity sınıfları için ortak alanları içerir.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Kayıt benzersiz kimliği.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Kaydı oluşturan kullanıcı kimliği.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// Kaydın oluşturulma zamanı.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Kaydı son güncelleyen kullanıcı kimliği.
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>
    /// Kaydın son güncellenme zamanı.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Kaydı silen kullanıcı kimliği.
    /// </summary>
    public Guid? DeletedByUserId { get; set; }

    /// <summary>
    /// Kaydın silinme zamanı.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Kaydın aktif mi silinmiş mi olduğunu gösterir.
    /// </summary>
    public bool IsDeleted { get; set; }
}
