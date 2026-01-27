namespace FarmazonDemo.Models.Enums;

public enum PaymentMethod
{
    CashOnDelivery = 1,   // Kapıda ödeme
    BankTransfer = 2,     // Havale / EFT
    CreditCard = 3,       // Kredi kartı
    DebitCard = 4,        // Banka kartı
    Wallet = 5,           // Dijital cüzdan (PayPal, Apple Pay vb.)
    Installment = 6,      // Taksitli ödeme
    BuyNowPayLater = 7    // Şimdi al sonra öde
}
