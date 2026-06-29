namespace YonetimFinansalIslemTakipSistemi.Domain.Enums;

public enum CargoShipmentStatus
{
    Draft              = 1, // eski kayıt uyumluluğu; yeni kayıtta kullanılmaz
    Prepared           = 2, // Gönderime Hazır (giden)
    Shipped            = 3, // Gönderildi (giden)
    Received           = 4, // Teslim Alındı (gelen)
    Delivered          = 5, // Teslim Edildi
    Cancelled          = 6, // İptal
    Waiting            = 7, // Bekleniyor (gelen)
    HandedToCargo      = 8, // Kargoya Teslim Edildi (giden)
    PersonnelDelivered = 9  // Personele Teslim Edildi (gelen)
}
