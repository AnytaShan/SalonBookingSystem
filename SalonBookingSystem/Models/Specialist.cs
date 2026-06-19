using System.ComponentModel.DataAnnotations;

namespace SalonBookingSystem.Models
{
    public class Specialist
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

        [StringLength(50)]
        [Display(Name = "Отчество")]
        public string? MiddleName { get; set; }

        [Phone]
        [StringLength(20)]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(500)]
        [Display(Name = "О себе")]
        public string? Bio { get; set; }

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<SpecialistService> SpecialistServices { get; set; } = new List<SpecialistService>();
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}
