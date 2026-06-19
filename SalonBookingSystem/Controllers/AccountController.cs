using Microsoft.AspNetCore.Mvc;
using SalonBookingSystem.Data;
using SalonBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace SalonBookingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string phone, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == phone && u.PasswordHash == password);
            if (user == null)
            {
                ModelState.AddModelError("", "Неверный телефон или пароль");
                return View();
            }

            // Сохраняем данные в сессию
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserFullName", $"{user.FirstName} {user.LastName}");
            HttpContext.Session.SetString("UserRole", user.Role.ToString());

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model)
        {
            if (await _context.Users.AnyAsync(u => u.Phone == model.Phone))
            {
                ModelState.AddModelError("Phone", "Пользователь с таким телефоном уже существует");
                return View(model);
            }

            model.Role = UserRole.Client;
            model.RegisteredAt = DateTime.Now;
            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            // Автоматический вход после регистрации
            HttpContext.Session.SetInt32("UserId", model.Id);
            HttpContext.Session.SetString("UserFullName", $"{model.FirstName} {model.LastName}");
            HttpContext.Session.SetString("UserRole", model.Role.ToString());

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = _context.Users.Find(userId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkTelegram(long chatId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();
            if (await _context.Users.AnyAsync(u => u.TelegramChatId == chatId && u.Id != userId))
            {
                ModelState.AddModelError("", "Этот Telegram уже привязан к другому аккаунту.");
                return View("Profile", user);
            }
            user.TelegramChatId = chatId;
            await _context.SaveChangesAsync();
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlinkTelegram()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var user = await _context.Users.FindAsync(userId.Value);
            user.TelegramChatId = null;
            await _context.SaveChangesAsync();
            return RedirectToAction("Profile");
        }
    }
}
