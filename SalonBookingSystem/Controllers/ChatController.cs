using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonBookingSystem.Data;
using SalonBookingSystem.Models;

namespace SalonBookingSystem.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Страница чата
        public IActionResult Index()
        {
            return View();
        }

        // Обработчик сообщений чата (AJAX)
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage message)
        {
            if (string.IsNullOrWhiteSpace(message?.Text))
                return Json(new { reply = "Пожалуйста, введите вопрос." });

            var reply = await GetBotReplyAsync(message.Text);
            return Json(new { reply });
        }

        // API для интеграции внешних чат-ботов
        [HttpPost("api/chatbot/query")]
        public async Task<IActionResult> ApiQuery([FromBody] ChatQuery query)
        {
            if (string.IsNullOrWhiteSpace(query?.Question))
                return BadRequest("Question is required.");

            var answer = await GetBotReplyAsync(query.Question);
            return Ok(new { answer });
        }

        private async Task<string> GetBotReplyAsync(string userMessage)
        {
            var keywords = userMessage.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var questions = await _context.BotQuestions
                .Where(q => q.IsActive)
                .ToListAsync();

            // Поиск наиболее подходящего ответа по совпадению ключевых слов
            BotQuestion? bestMatch = null;
            int maxMatches = 0;

            foreach (var q in questions)
            {
                var qKeywords = q.Keywords.ToLower().Split(',', StringSplitOptions.TrimEntries);
                int matches = qKeywords.Count(k => userMessage.Contains(k));
                if (matches > maxMatches)
                {
                    maxMatches = matches;
                    bestMatch = q;
                }
            }

            if (bestMatch != null && maxMatches > 0)
                return bestMatch.Answer;

            return "Извините, я не совсем понял вопрос. Попробуйте переформулировать или позвоните администратору по телефону +7 (495) 123-45-67.";
        }

        public class ChatMessage
        {
            public string Text { get; set; } = string.Empty;
        }

        public class ChatQuery
        {
            public string Question { get; set; } = string.Empty;
        }
    }
}
