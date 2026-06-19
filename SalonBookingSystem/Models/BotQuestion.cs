using System.ComponentModel.DataAnnotations;

namespace SalonBookingSystem.Models
{
    public class BotQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Ключевые слова (через запятую)")]
        public string Keywords { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "Ответ")]
        public string Answer { get; set; } = string.Empty;

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;
    }
}
