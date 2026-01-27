namespace FarmazonDemo.Services.TwoFactor
{
    public interface ITwoFactorService
    {
        string GenerateSecretKey();
        string GenerateQrCodeUri(string email, string secretKey);
        bool ValidateCode(string secretKey, string code);
        string GenerateBackupCodes(int count = 10);
    }
}
