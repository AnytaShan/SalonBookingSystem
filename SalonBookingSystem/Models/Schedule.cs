using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonBookingSystem.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SpecialistId { get; set; }

        [ForeignKey("SpecialistId")]
        public Specialist Specialist { get; set; } = null!;

        [Required]
        [Display(Name = "День недели")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [Display(Name = "Время начала работы")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "Время окончания работы")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Выходной")]
        public bool IsDayOff { get; set; } = false;
    }
}
