using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonBookingSystem.Data;
using SalonBookingSystem.Models;

namespace SalonBookingSystem.Controllers
{
    public class SpecialistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SpecialistController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsSpecialist()
        {
            return HttpContext.Session.GetString("UserRole") == "Specialist";
        }

        public async Task<IActionResult> Schedule()
        {
            if (!IsSpecialist()) return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await _context.Users.FindAsync(userId.Value);
            if (user?.SpecialistId == null) return NotFound("Специалист не привязан к учётной записи.");

            var specialistId = user.SpecialistId.Value;
            var today = DateTime.Today;
            var appointments = await _context.Appointments
                .Where(a => a.SpecialistId == specialistId && a.StartTime.Date >= today)
                .Include(a => a.Service)
                .Include(a => a.User)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            // Группируем по дате прямо в контроллере (можно и во вьюхе, но так удобнее)
            var grouped = appointments
                .GroupBy(a => a.StartTime.Date)
                .OrderBy(g => g.Key)
                .ToList();

            return View(grouped); // передаём список групп (IGrouping<DateTime, Appointment>)
        }

        [HttpPost]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            if (!IsSpecialist()) return RedirectToAction("Login", "Account");
            var app = await _context.Appointments.FindAsync(id);
            if (app != null && app.Status == AppointmentStatus.Confirmed)
            {
                app.Status = AppointmentStatus.Completed;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Schedule");
        }
    }
}