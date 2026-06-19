using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SalonBookingSystem.Data;
using SalonBookingSystem.Models;

namespace SalonBookingSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Выбор услуги
        public async Task<IActionResult> SelectService()
        {
            var services = await _context.Services.Where(s => s.IsActive).ToListAsync();
            return View(services);
        }

        // GET: Выбор специалиста по услуге
        public async Task<IActionResult> SelectSpecialist(int serviceId)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return NotFound();

            var specialists = await _context.SpecialistServices
                .Where(ss => ss.ServiceId == serviceId)
                .Include(ss => ss.Specialist)
                .Select(ss => ss.Specialist)
                .Where(s => s.IsActive)
                .ToListAsync();

            ViewBag.ServiceId = serviceId;
            ViewBag.ServiceName = service.Name;
            ViewBag.Duration = service.DurationMinutes;
            return View(specialists);
        }

        // GET: Выбор даты и времени
        public async Task<IActionResult> SelectDateTime(int serviceId, int specialistId)
        {
            var service = await _context.Services.FindAsync(serviceId);
            var specialist = await _context.Specialists.FindAsync(specialistId);
            if (service == null || specialist == null) return NotFound();

            ViewBag.Service = service;
            ViewBag.Specialist = specialist;

            // Получаем расписание специалиста
            var schedule = await _context.Schedules
                .Where(s => s.SpecialistId == specialistId && !s.IsDayOff)
                .ToListAsync();

            // Формируем доступные слоты на ближайшие 14 дней
            var availableSlots = new List<DateTime>();
            var startDate = DateTime.Today;
            for (int d = 0; d < 14; d++)
            {
                var date = startDate.AddDays(d);
                var daySchedule = schedule.FirstOrDefault(s => s.DayOfWeek == date.DayOfWeek);
                if (daySchedule == null) continue;

                var workStart = date.Date + daySchedule.StartTime;
                var workEnd = date.Date + daySchedule.EndTime;

                // Генерируем слоты с интервалом 30 минут
                for (var slot = workStart; slot.AddMinutes(service.DurationMinutes) <= workEnd; slot = slot.AddMinutes(30))
                {
                    // Проверяем, не занято ли это время
                    bool isBusy = await _context.Appointments.AnyAsync(a =>
                        a.SpecialistId == specialistId &&
                        a.Status != AppointmentStatus.Canceled &&
                        ((a.StartTime <= slot && a.EndTime > slot) ||
                         (a.StartTime < slot.AddMinutes(service.DurationMinutes) && a.EndTime >= slot.AddMinutes(service.DurationMinutes))));

                    if (!isBusy)
                        availableSlots.Add(slot);
                }
            }

            return View(availableSlots);
        }

        // POST: Создание записи
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int serviceId, int specialistId, DateTime startTime, string? note)
        {
            // 1. Проверка авторизации
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            // 2. Проверка существования услуги и вычисление времени окончания
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null)
                return NotFound();

            var endTime = startTime.AddMinutes(service.DurationMinutes);

            // 3. Транзакция с высоким уровнем изоляции
            await using var transaction = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                // 4. Проверка занятости с блокировкой строк специалиста на нужную дату
                bool isSlotBusy = await _context.Appointments
                    .Where(a => a.SpecialistId == specialistId
                                && a.Status != AppointmentStatus.Canceled
                                && a.StartTime < endTime
                                && a.EndTime > startTime)
                    .AnyAsync();

                if (isSlotBusy)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "К сожалению, выбранное время уже занято. Пожалуйста, выберите другой слот.";
                    return RedirectToAction("SelectDateTime", new { serviceId, specialistId });
                }

                // 5. Создание записи
                var appointment = new Appointment
                {
                    UserId = userId.Value,
                    ServiceId = serviceId,
                    SpecialistId = specialistId,
                    StartTime = startTime,
                    EndTime = endTime,
                    ClientNote = note,
                    Status = AppointmentStatus.Confirmed,
                    CreatedAt = DateTime.Now
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // 6. Фиксация транзакции
                await transaction.CommitAsync();

                return RedirectToAction("MyAppointments");
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Произошла ошибка при бронировании. Попробуйте ещё раз.";
                return RedirectToAction("SelectDateTime", new { serviceId, specialistId });
            }
        }

        // GET: Мои записи
        public async Task<IActionResult> MyAppointments()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var appointments = await _context.Appointments
                .Where(a => a.UserId == userId)
                .Include(a => a.Service)
                .Include(a => a.Specialist)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            return View(appointments);
        }

        // POST: Отмена записи
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (appointment == null) return NotFound();

            appointment.Status = AppointmentStatus.Canceled;
            await _context.SaveChangesAsync();

            return RedirectToAction("MyAppointments");
        }
    }
}