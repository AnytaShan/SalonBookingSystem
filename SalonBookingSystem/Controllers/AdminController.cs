using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SalonBookingSystem.Data;
using SalonBookingSystem.Models;


namespace SalonBookingSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        // ---------- УСЛУГИ ----------
        public async Task<IActionResult> Services()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var services = await _context.Services.OrderBy(s => s.Name).ToListAsync();
            return View(services);
        }

        public IActionResult CreateService()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateService(Service service)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                _context.Services.Add(service);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Services));
            }
            return View(service);
        }

        public async Task<IActionResult> EditService(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();
            return View(service);
        }

        [HttpPost]
        public async Task<IActionResult> EditService(int id, Service service)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id != service.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(service);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Services));
            }
            return View(service);
        }

        public async Task<IActionResult> DeleteService(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Services");
        }

        // ---------- СПЕЦИАЛИСТЫ ----------
        public async Task<IActionResult> Specialists()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var specialists = await _context.Specialists
                .Include(s => s.SpecialistServices)
                .OrderBy(s => s.LastName)
                .ToListAsync();
            return View(specialists);
        }

        public IActionResult CreateSpecialist()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSpecialist(Specialist specialist)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                _context.Specialists.Add(specialist);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Specialists));
            }
            return View(specialist);
        }

        public async Task<IActionResult> EditSpecialist(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var specialist = await _context.Specialists.FindAsync(id);
            if (specialist == null) return NotFound();
            return View(specialist);
        }

        [HttpPost]
        public async Task<IActionResult> EditSpecialist(int id, Specialist specialist)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id != specialist.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(specialist);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Specialists));
            }
            return View(specialist);
        }

        public async Task<IActionResult> DeleteSpecialist(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var specialist = await _context.Specialists
                .FirstOrDefaultAsync(m => m.Id == id);
            if (specialist == null) return NotFound();
            return View(specialist);
        }

        [HttpPost, ActionName("DeleteSpecialist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpecialistConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var specialist = await _context.Specialists
                .Include(s => s.Appointments)
                .Include(s => s.SpecialistServices)
                .Include(s => s.Schedules)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (specialist != null)
            {
                _context.Appointments.RemoveRange(specialist.Appointments);
                _context.SpecialistServices.RemoveRange(specialist.SpecialistServices);
                _context.Schedules.RemoveRange(specialist.Schedules);
                _context.Specialists.Remove(specialist);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Specialists));
        }

        // ---------- ЧАТ-БОТ ----------
        public async Task<IActionResult> BotQuestions()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var questions = await _context.BotQuestions.OrderBy(q => q.Id).ToListAsync();
            return View(questions);
        }

        public IActionResult CreateBotQuestion()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBotQuestion(BotQuestion botQuestion)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                _context.Add(botQuestion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(BotQuestions));
            }
            return View(botQuestion);
        }

        public async Task<IActionResult> EditBotQuestion(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var botQuestion = await _context.BotQuestions.FindAsync(id);
            if (botQuestion == null) return NotFound();
            return View(botQuestion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBotQuestion(int id, BotQuestion botQuestion)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id != botQuestion.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(botQuestion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(BotQuestions));
            }
            return View(botQuestion);
        }

        public async Task<IActionResult> DeleteBotQuestion(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var botQuestion = await _context.BotQuestions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (botQuestion == null) return NotFound();
            return View(botQuestion);
        }

        [HttpPost, ActionName("DeleteBotQuestion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBotQuestionConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var botQuestion = await _context.BotQuestions.FindAsync(id);
            if (botQuestion != null)
            {
                _context.BotQuestions.Remove(botQuestion);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(BotQuestions));
        }

        // ---------- ЗАПИСИ ----------
        public async Task<IActionResult> Appointments(DateTime? dateFrom, DateTime? dateTo, int? serviceId, int? specialistId, string clientName)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var query = _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Service)
                .Include(a => a.Specialist)
                .AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(a => a.StartTime.Date >= dateFrom.Value.Date);
            if (dateTo.HasValue)
                query = query.Where(a => a.StartTime.Date <= dateTo.Value.Date);
            if (serviceId.HasValue)
                query = query.Where(a => a.ServiceId == serviceId.Value);
            if (specialistId.HasValue)
                query = query.Where(a => a.SpecialistId == specialistId.Value);
            if (!string.IsNullOrEmpty(clientName))
                query = query.Where(a => (a.User.FirstName + " " + a.User.LastName).Contains(clientName));

            var appointments = await query.OrderByDescending(a => a.StartTime).ToListAsync();

            ViewBag.Services = new SelectList(_context.Services, "Id", "Name");
            ViewBag.Specialists = new SelectList(_context.Specialists, "Id", "LastName");
            return View(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var app = await _context.Appointments.FindAsync(id);
            if (app != null)
            {
                app.Status = AppointmentStatus.Canceled;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Appointments");
        }

        [HttpPost]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var app = await _context.Appointments.FindAsync(id);
            if (app != null)
            {
                app.Status = AppointmentStatus.Completed;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Appointments");
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var users = await _context.Users.Include(u => u.Specialist).ToListAsync();
            return View(users);
        }

        // GET: Admin/CreateUser
        public IActionResult CreateUser()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.Specialists = new SelectList(_context.Specialists, "Id", "LastName");
            return View();
        }

        // GET: Admin/EditUser/5
        public async Task<IActionResult> EditUser(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var user = await _context.Users.Include(u => u.Specialist).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            ViewBag.Specialists = new SelectList(_context.Specialists, "Id", "LastName", user.SpecialistId);
            return View(user);
        }

        // POST: Admin/EditUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, User user)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Чтобы избежать затирания пароля, если он не изменялся
                    var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                    if (existingUser != null && string.IsNullOrEmpty(user.PasswordHash))
                    {
                        user.PasswordHash = existingUser.PasswordHash; // оставляем старый пароль
                    }
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Users));
            }
            ViewBag.Specialists = new SelectList(_context.Specialists, "Id", "LastName", user.SpecialistId);
            return View(user);
        }

        // GET: Admin/DeleteUser/5
        public async Task<IActionResult> DeleteUser(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                user.RegisteredAt = DateTime.UtcNow;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Users");
            }
            ViewBag.Specialists = new SelectList(_context.Specialists, "Id", "LastName");
            return View(user);
        }
    }
}