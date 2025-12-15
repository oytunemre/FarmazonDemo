using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Entities

{

    // Model sınıfı     
    public class Users : BaseEntity
    {
        [Key]
        public int UserId { get; set; }
        public required string Name { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public  required string Password { get; set; } = string.Empty;        
        public required string Username { get; set; } = string.Empty;
       

    }
}
