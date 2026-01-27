namespace FarmazonDemo.Models.Enums;

public enum PaymentStatus
{
    Created = 1,           // Intent oluşturuldu
    Pending = 2,           // Ödeme bekleniyor (3D Secure vb.)
    Authorized = 3,        // Yetkilendirildi (henüz çekilmedi)
    Captured = 4,          // Ödeme alındı (kesinleşti)
    Failed = 5,            // Ödeme başarısız
    Cancelled = 6,         // İptal edildi
    Refunded = 7,          // Tam iade yapıldı
    PartiallyRefunded = 8, // Kısmi iade yapıldı
    Expired = 9            // Süresi doldu
}
