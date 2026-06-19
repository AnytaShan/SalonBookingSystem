namespace SalonBookingSystem.Services
{
    public class BookingState
    {
        public long ChatId { get; set; }
        public int Step { get; set; } // 0=начало, 1=выбрана услуга, 2=выбран специалист, 3=выбрано время
        public int? ServiceId { get; set; }
        public int? SpecialistId { get; set; }
        public DateTime? SelectedTime { get; set; }
    }
}