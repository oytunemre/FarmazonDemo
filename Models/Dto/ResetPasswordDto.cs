using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Dto
{
    public class ResetPasswordDto
    {
        [Required]
        public required string Token { get; set; }

        [Required]
        [MinLength(6)]
        public required string NewPassword { get; set; }
    }
}
