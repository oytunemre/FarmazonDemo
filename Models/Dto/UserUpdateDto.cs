namespace FarmazonDemo.Models.Dto
{
    public class UserUpdateDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; } 
        public required string Password { get; set; } 
        public required string Username { get; set; } 
    }

}
