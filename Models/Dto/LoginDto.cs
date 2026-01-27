namespace FarmazonDemo.Models.Dto
{
    public class LoginDto
    {
        public required string EmailOrUsername { get; set; } = string.Empty;
        public required string Password { get; set; } = string.Empty;
    }
}
