using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonBookingSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        [Required(ErrorMessage = "Телефон обязателен для входа")]
        [StringLength(20)]
        [Display(Name = "Телефон (логин)")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 4)]
        [Display(Name = "Пароль")]
        public string? PasswordHash { get; set; } // для простоты храним хэш

        [Display(Name = "Telegram Chat ID")]
        public long? TelegramChatId { get; set; }

        public int? SpecialistId { get; set; }
        [ForeignKey("SpecialistId")]
        public Specialist? Specialist { get; set; }

        [Display(Name = "Роль")]
        public UserRole Role { get; set; } = UserRole.Client;

        [Display(Name = "Дата регистрации")]
        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        // Навигационное свойство
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }

    public enum UserRole
    {
        Client = 0,
        Specialist = 1,  
        Admin = 2
    }
}
