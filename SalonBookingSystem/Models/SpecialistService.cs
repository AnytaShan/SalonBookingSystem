using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonBookingSystem.Models
{
    public class SpecialistService
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SpecialistId { get; set; }

        [ForeignKey("SpecialistId")]
        public Specialist Specialist { get; set; } = null!;

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public Service Service { get; set; } = null!;
    }
}
