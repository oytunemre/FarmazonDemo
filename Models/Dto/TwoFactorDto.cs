using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Dto
{
    public class TwoFactorSetupResponseDto
    {
        public required string SecretKey { get; set; }
        public required string QrCodeUri { get; set; }
        public required string[] BackupCodes { get; set; }
    }

    public class TwoFactorVerifyDto
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public required string Code { get; set; }
    }

    public class TwoFactorLoginDto
    {
        [Required]
        public required string EmailOrUsername { get; set; }

        [Required]
        public required string Password { get; set; }

        [Required]
        [StringLength(8, MinimumLength = 6)]
        public required string TwoFactorCode { get; set; }
    }

    public class TwoFactorStatusDto
    {
        public bool IsEnabled { get; set; }
        public DateTime? EnabledAt { get; set; }
    }
}
