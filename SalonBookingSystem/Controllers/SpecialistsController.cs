using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonBookingSystem.Data;
using SalonBookingSystem.Models;

namespace SalonBookingSystem.Controllers
{
    public class SpecialistsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SpecialistsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var specialists = await _context.Specialists
                .Where(s => s.IsActive)
                .Include(s => s.SpecialistServices)
                    .ThenInclude(ss => ss.Service)
                .OrderBy(s => s.LastName)
                .ToListAsync();
            return View(specialists);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var specialist = await _context.Specialists
                .Include(s => s.SpecialistServices)
                    .ThenInclude(ss => ss.Service)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (specialist == null) return NotFound();

            return View(specialist);
        }
    }
}
