using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Dto
{
    public class EmailRequestDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}
