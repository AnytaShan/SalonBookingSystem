using Microsoft.EntityFrameworkCore;
using SalonBookingSystem.Data;
using SalonBookingSystem.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UserModel = SalonBookingSystem.Models.User;  

namespace SalonBookingSystem.Services
{
    public class TelegramBotService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private TelegramBotClient _botClient;
        private static readonly Dictionary<long, BookingState> _userStates = new();

        public TelegramBotService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var token = _configuration["TelegramBot:Token"];
            if (string.IsNullOrWhiteSpace(token)) throw new Exception("Токен бота не задан");
            _botClient = new TelegramBotClient(token);
            var me = await _botClient.GetMeAsync(cancellationToken);
            Console.WriteLine($"Бот {me.Username} запущен");

            _botClient.StartReceiving(UpdateHandler, ErrorHandler, cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                if (update.Type == UpdateType.Message && update.Message?.Text != null)
                {
                    await HandleMessage(client, update.Message, db);
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    await HandleCallback(client, update.CallbackQuery, db);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка бота: {ex.Message}");
            }
        }

        // Получение специалистов для услуги через таблицу SpecialistServices
        private async Task<List<UserModel>> GetSpecialistsForService(ApplicationDbContext db, int serviceId)
        {
            // 1. ID специалистов из связующей таблицы (ссылаются на Specialists.Id)
            var specialistIds = await db.SpecialistServices
                .Where(ss => ss.ServiceId == serviceId)
                .Select(ss => ss.SpecialistId)   // это Specialists.Id
                .ToListAsync();

            if (!specialistIds.Any())
                return new List<UserModel>();

            // 2. Находим пользователей, у которых роль = Specialist и SpecialistId (из Users) входит в список
            var users = await db.Users
                .Where(u => u.Role == UserRole.Specialist && u.SpecialistId != null && specialistIds.Contains(u.SpecialistId.Value))
                .ToListAsync();

            return users;
        }
        private async Task HandleMessage(ITelegramBotClient client, Message message, ApplicationDbContext db)
        {
            var chatId = message.Chat.Id;
            var text = message.Text.ToLower();

            if (_userStates.ContainsKey(chatId) && _userStates[chatId].Step > 0)
            {
                await client.SendTextMessageAsync(chatId, "Пожалуйста, используйте кнопки для записи.");
                return;
            }

            switch (text)
            {
                case "/start": await HandleStart(client, message, db); break;
                case "/services": await HandleServices(client, message, db); break;
                case "/price": await HandlePrice(client, message, db); break;
                case "/address":
                case "/hours": await HandleInfo(client, message, db); break;
                case "/record": await StartBooking(client, message, db); break;
                case "/myrecords": await HandleMyRecords(client, message, db); break;
                default: await HandleFreeText(client, message, db); break;
            }
        }

        private async Task StartBooking(ITelegramBotClient client, Message message, ApplicationDbContext db)
        {
            var chatId = message.Chat.Id;
            var user = await db.Users.FirstOrDefaultAsync(u => u.TelegramChatId == chatId);
            if (user == null)
            {
                await client.SendTextMessageAsync(chatId, "Сначала привяжите Telegram в личном кабинете на сайте (раздел «Профиль»).");
                return;
            }

            _userStates[chatId] = new BookingState { ChatId = chatId, Step = 0 };
            await ShowServicesForBooking(client, chatId, db);
        }

        private async Task ShowServicesForBooking(ITelegramBotClient client, long chatId, ApplicationDbContext db)
        {
            var services = await db.Services.Where(s => s.IsActive).ToListAsync();
            if (!services.Any())
            {
                await client.SendTextMessageAsync(chatId, "Нет доступных услуг.");
                _userStates.Remove(chatId);
                return;
            }

            var buttons = services.Select(s => InlineKeyboardButton.WithCallbackData(s.Name, $"service_{s.Id}")).ToList();
            var keyboard = new InlineKeyboardMarkup(buttons.Chunk(2).ToArray());
            await client.SendTextMessageAsync(chatId, "Выберите услугу:", replyMarkup: keyboard);
            _userStates[chatId].Step = 1;
        }

        // Единственный метод показа специалистов – использует GetSpecialistsForService
        private async Task ShowSpecialistsForService(ITelegramBotClient client, long chatId, ApplicationDbContext db, int serviceId)
        {
            var users = await GetSpecialistsForService(db, serviceId);
            if (!users.Any())
            {
                await client.SendTextMessageAsync(chatId, "Для этой услуги нет специалистов.");
                return;
            }

            var buttons = users.Select(u =>
                InlineKeyboardButton.WithCallbackData(
                    $"{u.FirstName} {u.LastName}",
                    $"specialist_{u.SpecialistId.Value}"   // передаём SpecialistId (из Specialists)
                )
            ).ToList();

            var keyboard = new InlineKeyboardMarkup(buttons.Chunk(2).ToArray());
            await client.SendTextMessageAsync(chatId, "Выберите специалиста:", replyMarkup: keyboard);
        }

        private async Task HandleCallback(ITelegramBotClient client, CallbackQuery callback, ApplicationDbContext db)
        {
            var chatId = callback.Message.Chat.Id;
            var data = callback.Data;

            if (!_userStates.ContainsKey(chatId))
            {
                await client.AnswerCallbackQueryAsync(callback.Id, "Сначала начните запись с помощью /record");
                return;
            }

            var state = _userStates[chatId];

            if (data.StartsWith("service_"))
            {
                var serviceId = int.Parse(data.Split('_')[1]);
                Console.WriteLine($"[LOG] Callback service_ распарсил serviceId = {serviceId}");
                state.ServiceId = serviceId;
                state.Step = 2;
                await ShowSpecialistsForService(client, chatId, db, serviceId);
            }
            else if (data.StartsWith("specialist_"))
            {
                var specialistId = int.Parse(data.Split('_')[1]);
                state.SpecialistId = specialistId;
                state.Step = 3;
                await ShowTimeSlots(client, chatId, db, state);
            }
            else if (data.StartsWith("time_"))
            {
                var selectedTimeStr = data.Substring(5);
                if (DateTime.TryParse(selectedTimeStr, out var selectedTime))
                {
                    state.SelectedTime = selectedTime;
                    await ConfirmBooking(client, chatId, db, state);
                }
                else
                {
                    await client.AnswerCallbackQueryAsync(callback.Id, "Ошибка формата времени");
                }
            }
            else if (data == "confirm_yes")
            {
                await CreateBooking(client, chatId, db, state);
                _userStates.Remove(chatId);
            }
            else if (data == "confirm_no")
            {
                await client.SendTextMessageAsync(chatId, "Запись отменена.");
                _userStates.Remove(chatId);
            }

            await client.AnswerCallbackQueryAsync(callback.Id);
        }

        private async Task ShowTimeSlots(ITelegramBotClient client, long chatId, ApplicationDbContext db, BookingState state)
        {
            var specialistId = state.SpecialistId.Value;
            var service = await db.Services.FindAsync(state.ServiceId.Value);
            var duration = service.DurationMinutes;
            var startDate = DateTime.Today.AddDays(1);
            var endDate = startDate.AddDays(7);

            var slots = new List<DateTime>();
            var current = startDate.Date.AddHours(9);
            while (current < endDate && current.TimeOfDay < TimeSpan.FromHours(20))
            {
                slots.Add(current);
                current = current.AddMinutes(30);
            }

            var booked = await db.Appointments
                .Where(a => a.SpecialistId == specialistId && a.StartTime >= startDate && a.StartTime < endDate && a.Status != AppointmentStatus.Canceled)
                .Select(a => a.StartTime)
                .ToListAsync();

            var freeSlots = slots.Where(s => !booked.Contains(s) && !booked.Contains(s.AddMinutes(-duration))).ToList();

            if (!freeSlots.Any())
            {
                await client.SendTextMessageAsync(chatId, "Нет свободных слотов на ближайшую неделю.");
                _userStates.Remove(chatId);
                return;
            }

            var buttons = freeSlots.Select(slot => InlineKeyboardButton.WithCallbackData($"{slot:dd.MM HH:mm}", $"time_{slot:yyyy-MM-dd HH:mm:ss}")).ToList();
            var keyboard = new InlineKeyboardMarkup(buttons.Chunk(2).ToArray());
            await client.SendTextMessageAsync(chatId, "Выберите удобное время:", replyMarkup: keyboard);
        }

        private async Task ConfirmBooking(ITelegramBotClient client, long chatId, ApplicationDbContext db, BookingState state)
        {
            var service = await db.Services.FindAsync(state.ServiceId);
            var specialist = await db.Specialists.FindAsync(state.SpecialistId.Value); // берём из Specialists
            var time = state.SelectedTime.Value;

            var text = $"📝 Подтвердите запись:\n" +
                       $"Услуга: {service.Name}\n" +
                       $"Специалист: {specialist.FirstName} {specialist.LastName}\n" +
                       $"Дата и время: {time:dd.MM yyyy HH:mm}\n\n" +
                       $"Подтверждаете?";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("✅ Да", "confirm_yes"), InlineKeyboardButton.WithCallbackData("❌ Нет", "confirm_no") }
    });
            await client.SendTextMessageAsync(chatId, text, replyMarkup: keyboard);
        }

        private async Task CreateBooking(ITelegramBotClient client, long chatId, ApplicationDbContext db, BookingState state)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.TelegramChatId == chatId);
            if (user == null)
            {
                await client.SendTextMessageAsync(chatId, "Пользователь не найден. Привяжите аккаунт.");
                return;
            }

            var appointment = new Appointment
            {
                UserId = user.Id,
                ServiceId = state.ServiceId.Value,
                SpecialistId = state.SpecialistId.Value,
                StartTime = state.SelectedTime.Value,
                Status = AppointmentStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            await client.SendTextMessageAsync(chatId, $"✅ Запись успешно создана на {appointment.StartTime:dd.MM yyyy HH:mm}!");
        }

        private async Task HandleStart(ITelegramBotClient client, Message message, ApplicationDbContext db)
        {
            var chatId = message.Chat.Id;
            var user = await db.Users.FirstOrDefaultAsync(u => u.TelegramChatId == chatId);
            if (user != null)
            {
                await client.SendTextMessageAsync(chatId, $"С возвращением, {user.FirstName}!");
            }
            else
            {
                await client.SendTextMessageAsync(chatId,
                    "Добро пожаловать! Я виртуальный помощник салона красоты.\n" +
                    "Для доступа к вашим записям привяжите аккаунт на сайте в разделе «Профиль».\n" +
                    "Используйте команды:\n/services - список услуг\n/price - цены\n/address - адрес\n/record - записаться\n/myrecords - мои записи");
            }
        }

        private async Task HandleServices(ITelegramBotClient client, Message message, ApplicationDbContext db)
        {
            var services = await db.Services.Where(s => s.IsActive).ToListAsync();
            var text = "Наши услуги:\n" + string.Join("\n", services.Select(s => $"{s.Name} — {s.Price} ₽ ({s.DurationMinutes} мин)"));
            await client.SendTextMessageAsync(message.Chat.Id, text);
        }

        private async Task HandlePrice(ITelegramBotClient client, Message message, ApplicationDbContext db)
        {
            var services = await db.Services.Where(s => s.IsActive).ToListAsync();
            var text = "Прайс-лист:\n" + string.Join("\n", services.Select(s => $"{s.Name}: {s.Price} ₽"));
            await client.SendTextMessageAsync(message.Chat.Id, text);
        }

        private async Task HandleInfo(ITelegramBotClient client, Message message, ApplicationDbContext db)
        {
            var info = await db.BotQuestions.FirstOrDefaultAsync(q => q.Keywords.Contains("адрес"));
            var text = info?.Answer ?? "г. Москва, ул. Примерная, д.10";
            await client.SendTextMessageAsync(message.Chat.Id, text);
        }

        private async Task HandleMyRecords(ITelegramBotClient client, Message message, ApplicationDbContext db)
        {
            var chatId = message.Chat.Id;
            var user = await db.Users.FirstOrDefaultAsync(u => u.TelegramChatId == chatId);
            if (user == null)
            {
                await client.SendTextMessageAsync(chatId, "Сначала привяжите Telegram в личном кабинете на сайте.");
                return;
            }

            var appointments = await db.Appointments
                .Where(a => a.UserId == user.Id && a.Status != AppointmentStatus.Canceled)
                .Include(a => a.Service)
                .Include(a => a.Specialist)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            if (!appointments.Any())
                await client.SendTextMessageAsync(chatId, "У вас нет активных записей.");
            else
            {
                var text = "Ваши записи:\n" + string.Join("\n", appointments.Select(a =>
                    $"{a.StartTime:dd.MM HH:mm} — {a.Service.Name} ({a.Specialist.LastName} {a.Specialist.FirstName})"));
                await client.SendTextMessageAsync(chatId, text);
            }
        }

        private async Task HandleFreeText(ITelegramBotClient client, Message message, ApplicationDbContext db)
        {
            var userMessage = message.Text.ToLower();
            var allQa = await db.BotQuestions.Where(q => q.IsActive).ToListAsync();
            BotQuestion? bestMatch = null;
            int maxMatches = 0;
            foreach (var qa in allQa)
            {
                var keywords = qa.Keywords.ToLower().Split(',').Select(k => k.Trim());
                int matches = keywords.Count(k => userMessage.Contains(k));
                if (matches > maxMatches)
                {
                    maxMatches = matches;
                    bestMatch = qa;
                }
            }
            var reply = bestMatch?.Answer ?? "Извините, я не понял. Воспользуйтесь командами или позвоните +7(495)123-45-67.";
            await client.SendTextMessageAsync(message.Chat.Id, reply);
        }

        private Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка бота: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}