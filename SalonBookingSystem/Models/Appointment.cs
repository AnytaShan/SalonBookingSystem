using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonBookingSystem.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public Service Service { get; set; } = null!;

        [Required]
        public int SpecialistId { get; set; }

        [ForeignKey("SpecialistId")]
        public Specialist Specialist { get; set; } = null!;

        [Required]
        [Display(Name = "Дата и время начала")]
        public DateTime StartTime { get; set; }

        [Display(Name = "Дата и время окончания")]
        public DateTime EndTime { get; set; }

        [Display(Name = "Статус")]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;

        [StringLength(500)]
        [Display(Name = "Примечание клиента")]
        public string? ClientNote { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum AppointmentStatus
    {
        Confirmed,  // Подтверждена
        Canceled,   // Отменена
        Completed   // Выполнена
    }
}
