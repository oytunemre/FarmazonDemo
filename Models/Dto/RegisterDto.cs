using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto
{
    public class RegisterDto
    {
        public required string Name { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public required string Username { get; set; } = string.Empty;
        public required string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Customer;
    }
}
