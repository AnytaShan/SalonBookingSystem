using Microsoft.EntityFrameworkCore;
using SalonBookingSystem.Models;
using System;
using System.Linq;

namespace SalonBookingSystem.Data
{
    public class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Добавление администратора
            if (!context.Users.Any(u => u.Role == UserRole.Admin))
            {
                var admin = new User
                {
                    FirstName = "Админ",
                    LastName = "Админов",
                    Phone = "admin",
                    PasswordHash = "admin123", // в реальном проекте хэшируйте!
                    Email = "admin@salon.ru",
                    Role = UserRole.Admin,
                    RegisteredAt = DateTime.Now
                };
                context.Users.Add(admin);
            }

            // Добавление услуг
            if (!context.Services.Any())
            {
                var services = new Service[]
                {
                    new Service { Name = "Женская стрижка", Description = "Модельная стрижка с укладкой", Price = 1500, DurationMinutes = 60 },
                    new Service { Name = "Мужская стрижка", Description = "Стрижка машинкой и ножницами", Price = 800, DurationMinutes = 30 },
                    new Service { Name = "Окрашивание", Description = "Окрашивание волос (длина средняя)", Price = 3500, DurationMinutes = 120 },
                    new Service { Name = "Маникюр", Description = "Классический маникюр с покрытием", Price = 1200, DurationMinutes = 60 },
                    new Service { Name = "Педикюр", Description = "Аппаратный педикюр", Price = 1800, DurationMinutes = 90 }
                };
                context.Services.AddRange(services);
                context.SaveChanges();
            }

            // Добавление специалистов
            if (!context.Specialists.Any())
            {
                var specialists = new Specialist[]
                {
                    new Specialist { FirstName = "Елена", LastName = "Иванова", Bio = "Парикмахер-стилист с 10-летним стажем", Phone = "+79161234567" },
                    new Specialist { FirstName = "Ольга", LastName = "Петрова", Bio = "Мастер маникюра и педикюра", Phone = "+79161234568" },
                    new Specialist { FirstName = "Андрей", LastName = "Сидоров", Bio = "Мужской мастер", Phone = "+79161234569" }
                };
                context.Specialists.AddRange(specialists);
                context.SaveChanges();
            }

            // Связи специалист-услуга
            if (!context.SpecialistServices.Any())
            {
                var elena = context.Specialists.First(s => s.FirstName == "Елена");
                var olga = context.Specialists.First(s => s.FirstName == "Ольга");
                var andrey = context.Specialists.First(s => s.FirstName == "Андрей");

                var womenHair = context.Services.First(s => s.Name == "Женская стрижка");
                var menHair = context.Services.First(s => s.Name == "Мужская стрижка");
                var coloring = context.Services.First(s => s.Name == "Окрашивание");
                var manicure = context.Services.First(s => s.Name == "Маникюр");
                var pedicure = context.Services.First(s => s.Name == "Педикюр");

                context.SpecialistServices.AddRange(
                    new SpecialistService { SpecialistId = elena.Id, ServiceId = womenHair.Id },
                    new SpecialistService { SpecialistId = elena.Id, ServiceId = coloring.Id },
                    new SpecialistService { SpecialistId = andrey.Id, ServiceId = menHair.Id },
                    new SpecialistService { SpecialistId = olga.Id, ServiceId = manicure.Id },
                    new SpecialistService { SpecialistId = olga.Id, ServiceId = pedicure.Id }
                );
                context.SaveChanges();
            }

            // Расписание (рабочие дни)
            if (!context.Schedules.Any())
            {
                var specialists = context.Specialists.ToList();
                foreach (var spec in specialists)
                {
                    // Пн-Пт с 10 до 20
                    for (int i = 1; i <= 5; i++)
                    {
                        context.Schedules.Add(new Schedule
                        {
                            SpecialistId = spec.Id,
                            DayOfWeek = (DayOfWeek)i,
                            StartTime = new TimeSpan(10, 0, 0),
                            EndTime = new TimeSpan(20, 0, 0),
                            IsDayOff = false
                        });
                    }
                    // Сб, Вс выходные
                    context.Schedules.Add(new Schedule
                    {
                        SpecialistId = spec.Id,
                        DayOfWeek = DayOfWeek.Saturday,
                        IsDayOff = true
                    });
                    context.Schedules.Add(new Schedule
                    {
                        SpecialistId = spec.Id,
                        DayOfWeek = DayOfWeek.Sunday,
                        IsDayOff = true
                    });
                }
                context.SaveChanges();
            }

            // Тестовые пользователи
            if (!context.Users.Any(u => u.Role == UserRole.Client))
            {
                var client1 = new User
                {
                    FirstName = "Анна",
                    LastName = "Глебова",
                    Phone = "89161112233",
                    Email = "anna@example.com",
                    PasswordHash = "client123",
                    Role = UserRole.Client
                };
                context.Users.Add(client1);
                context.SaveChanges();
            }

            // Тестовые записи
            if (!context.Appointments.Any())
            {
                var client = context.Users.First(u => u.Role == UserRole.Client);
                var service = context.Services.First(s => s.Name == "Женская стрижка");
                var specialist = context.Specialists.First(s => s.FirstName == "Елена");

                var start = DateTime.Today.AddDays(1).AddHours(11);
                context.Appointments.Add(new Appointment
                {
                    UserId = client.Id,
                    ServiceId = service.Id,
                    SpecialistId = specialist.Id,
                    StartTime = start,
                    EndTime = start.AddMinutes(service.DurationMinutes),
                    ClientNote = "Пожелания: хочу каре",
                    Status = AppointmentStatus.Confirmed
                });
                context.SaveChanges();
            }

            // Вопросы-ответы для чат-бота
            if (!context.BotQuestions.Any())
            {
                var qa = new BotQuestion[]
                {
                    new BotQuestion { Keywords = "привет,здравствуйте,добрый день", Answer = "Здравствуйте! Я виртуальный помощник салона красоты. Чем могу помочь?" },
                    new BotQuestion { Keywords = "адрес,где находитесь,как добраться", Answer = "Наш адрес: г. Москва, ул. Примерная, д. 10. Мы рядом с метро 'Примерная'." },
                    new BotQuestion { Keywords = "телефон,контакты,позвонить", Answer = "Вы можете позвонить нам по телефону: +7 (495) 123-45-67." },
                    new BotQuestion { Keywords = "услуги,прайс,цены", Answer = "У нас большой выбор услуг: стрижки, окрашивание, маникюр, педикюр. Актуальные цены смотрите на странице 'Услуги'." },
                    new BotQuestion { Keywords = "запись,записаться,онлайн", Answer = "Вы можете записаться онлайн на нашем сайте. Выберите услугу, специалиста и удобное время." },
                    new BotQuestion { Keywords = "отмена,отменить запись", Answer = "Для отмены записи войдите в личный кабинет и перейдите в раздел 'Мои записи'." },
                    new BotQuestion { Keywords = "время работы,график", Answer = "Мы работаем с понедельника по пятницу с 10:00 до 20:00. Суббота и воскресенье — выходные." },
                    new BotQuestion { Keywords = "спасибо", Answer = "Пожалуйста! Рады помочь. Хорошего дня!" }
                };
                context.BotQuestions.AddRange(qa);
                context.SaveChanges();
            }

            // Проверка наличия мастера-пользователя
            if (!context.Users.Any(u => u.Role == UserRole.Specialist))
            {
                var specialist = context.Specialists.First(s => s.FirstName == "Елена"); // предполагаем, что Елена существует
                var specialistUser = new User
                {
                    FirstName = specialist.FirstName,
                    LastName = specialist.LastName,
                    Phone = "master",  // для входа
                    PasswordHash = "master123",
                    Role = UserRole.Specialist,
                    SpecialistId = specialist.Id,
                    RegisteredAt = DateTime.UtcNow
                };
                context.Users.Add(specialistUser);
                context.SaveChanges();
            }
        }
    }
}
