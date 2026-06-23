using System.ComponentModel.DataAnnotations;

namespace MeteoTracker.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; } // Hash paswsword for security reasons

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } // "Admin" or "User"
    }
}