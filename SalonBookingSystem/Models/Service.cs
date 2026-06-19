using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SalonBookingSystem.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название услуги обязательно")]
        [StringLength(100)]
        [Display(Name = "Название услуги")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Цена (₽)")]
        [Range(0, 9999999.99, ErrorMessage = "Цена должна быть положительным числом")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Длительность (минут)")]
        [Range(5, 480, ErrorMessage = "Длительность должна быть от 5 до 480 минут")]
        public int DurationMinutes { get; set; }

        [Display(Name = "Активна")]
        public bool IsActive { get; set; } = true;

        // Навигационное свойство
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<SpecialistService> SpecialistServices { get; set; } = new List<SpecialistService>();
    }
}
