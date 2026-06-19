using System.ComponentModel.DataAnnotations;

namespace SalonBookingSystem.Models
{
    public class TelegramLinkCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = Guid.NewGuid().ToString("N")[..8];

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsUsed { get; set; } = false;
    }
}