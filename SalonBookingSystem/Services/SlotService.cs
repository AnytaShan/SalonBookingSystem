using Microsoft.EntityFrameworkCore;
using SalonBookingSystem.Data;
using SalonBookingSystem.Models;

namespace SalonBookingSystem.Services
{
    public class SlotService
    {
        private readonly ApplicationDbContext _context;

        public SlotService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DateTime>> GetAvailableSlots(int specialistId, int serviceId, DateTime fromDate)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return new List<DateTime>();

            var schedule = await _context.Schedules
                .Where(s => s.SpecialistId == specialistId && !s.IsDayOff)
                .ToListAsync();

            var appointments = await _context.Appointments
                .Where(a => a.SpecialistId == specialistId
                            && a.Status != AppointmentStatus.Canceled
                            && a.StartTime >= fromDate
                            && a.EndTime <= fromDate.AddDays(14))
                .ToListAsync();

            var availableSlots = new List<DateTime>();

            for (int d = 0; d < 14; d++)
            {
                var date = fromDate.AddDays(d);
                var daySchedule = schedule.FirstOrDefault(s => s.DayOfWeek == date.DayOfWeek);
                if (daySchedule == null) continue;

                var workStart = date.Date + daySchedule.StartTime;
                var workEnd = date.Date + daySchedule.EndTime;

                for (var slot = workStart; slot.AddMinutes(service.DurationMinutes) <= workEnd; slot = slot.AddMinutes(30))
                {
                    var slotEnd = slot.AddMinutes(service.DurationMinutes);

                    // Приводим все даты к Local для надёжного сравнения
                    bool isBusy = appointments.Any(a =>
                        DateTime.SpecifyKind(a.StartTime, DateTimeKind.Local) < slotEnd &&
                        DateTime.SpecifyKind(a.EndTime, DateTimeKind.Local) > slot);

                    if (!isBusy)
                        availableSlots.Add(slot);
                }
            }
            return availableSlots;
        }

        public async Task<bool> TryCreateAppointment(int userId, int serviceId, int specialistId, DateTime startTime, string? note)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return false;

            // Нормализуем переданное время и вычисляем конец
            startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Local);
            var endTime = DateTime.SpecifyKind(startTime.AddMinutes(service.DurationMinutes), DateTimeKind.Local);

            // Загружаем все активные записи специалиста
            var conflicting = await _context.Appointments
                .Where(a => a.SpecialistId == specialistId && a.Status != AppointmentStatus.Canceled)
                .ToListAsync();

            // Проверяем пересечения с нормализацией Kind
            bool isBusy = conflicting.Any(a =>
                DateTime.SpecifyKind(a.StartTime, DateTimeKind.Local) < endTime &&
                DateTime.SpecifyKind(a.EndTime, DateTimeKind.Local) > startTime);

            if (isBusy) return false;

            _context.Appointments.Add(new Appointment
            {
                UserId = userId,
                ServiceId = serviceId,
                SpecialistId = specialistId,
                StartTime = startTime,
                EndTime = endTime,
                ClientNote = note,
                Status = AppointmentStatus.Confirmed,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return true;
        }
    }
}