using Microsoft.EntityFrameworkCore;
using SalonBookingSystem.Data;
using SalonBookingSystem.Models;
using SalonBookingSystem.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SalonBookingSystem.Tests
{
    public class SlotServiceTests
    {
        private ApplicationDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        // ================== GetAvailableSlots ==================

        [Fact]
        public async Task GetAvailableSlots_ExcludesBusySlots()
        {
            var context = GetInMemoryContext();
            var specialist = new Specialist { Id = 1, FirstName = "Elena", IsActive = true };
            var service = new Service { Id = 1, Name = "Haircut", DurationMinutes = 60, IsActive = true };
            context.Specialists.Add(specialist);
            context.Services.Add(service);
            context.Schedules.Add(new Schedule
            {
                SpecialistId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(12)
            });
            var busyDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Local);
            context.Appointments.Add(new Appointment
            {
                SpecialistId = 1,
                ServiceId = 1,
                StartTime = busyDate,
                EndTime = busyDate.AddHours(1),
                Status = AppointmentStatus.Confirmed,
                UserId = 1
            });
            await context.SaveChangesAsync();

            var slotService = new SlotService(context);
            var fromDate = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Local); // воскресенье, захватит 1 и 8 июн€

            var slots = await slotService.GetAvailableSlots(1, 1, fromDate);

            Assert.DoesNotContain(busyDate, slots);               // зан€той слот 1 июн€ удалЄн
            Assert.Contains(new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Local), slots); // свободный слот 1 июн€
        }

        [Fact]
        public async Task GetAvailableSlots_IncludesCanceledAppointmentsSlots()
        {
            var context = GetInMemoryContext();
            context.Specialists.Add(new Specialist { Id = 1, FirstName = "Elena", IsActive = true });
            context.Services.Add(new Service { Id = 1, Name = "Haircut", DurationMinutes = 60, IsActive = true });
            context.Schedules.Add(new Schedule
            {
                SpecialistId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(12)
            });
            var canceledDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Local);
            context.Appointments.Add(new Appointment
            {
                SpecialistId = 1,
                ServiceId = 1,
                StartTime = canceledDate,
                EndTime = canceledDate.AddHours(1),
                Status = AppointmentStatus.Canceled,
                UserId = 1
            });
            await context.SaveChangesAsync();

            var slotService = new SlotService(context);
            var fromDate = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Local);

            var slots = await slotService.GetAvailableSlots(1, 1, fromDate);
            Assert.Contains(canceledDate, slots);
        }

        [Fact]
        public async Task GetAvailableSlots_ReturnsEmptyForDayOff()
        {
            var context = GetInMemoryContext();
            context.Specialists.Add(new Specialist { Id = 1, FirstName = "Elena", IsActive = true });
            context.Services.Add(new Service { Id = 1, Name = "Haircut", DurationMinutes = 60, IsActive = true });
            context.Schedules.Add(new Schedule
            {
                SpecialistId = 1,
                DayOfWeek = DayOfWeek.Sunday,
                IsDayOff = true
            });
            await context.SaveChangesAsync();

            var slotService = new SlotService(context);
            var fromDate = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Local); // суббота, следующий день Ц выходной
            var slots = await slotService.GetAvailableSlots(1, 1, fromDate);
            Assert.Empty(slots);
        }

        [Fact]
        public async Task GetAvailableSlots_DoesNotGenerateSlotsOutsideWorkingHours()
        {
            var context = GetInMemoryContext();
            context.Specialists.Add(new Specialist { Id = 1, FirstName = "Elena", IsActive = true });
            context.Services.Add(new Service { Id = 1, Name = "Haircut", DurationMinutes = 60, IsActive = true });
            context.Schedules.Add(new Schedule
            {
                SpecialistId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(11)  // только 1 час работы
            });
            await context.SaveChangesAsync();

            var slotService = new SlotService(context);
            // Ќачинаем с понедельника Ц в диапазоне два понедельника: 1 и 8 июн€
            var fromDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local);

            var slots = await slotService.GetAvailableSlots(1, 1, fromDate);

            // ќжидаем ровно два слота: 1 июн€ 10:00 и 8 июн€ 10:00
            Assert.Equal(2, slots.Count);
            Assert.Contains(new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Local), slots);
            Assert.Contains(new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Local), slots);
        }

        [Fact]
        public async Task GetAvailableSlots_GeneratesSlotsWith30MinInterval()
        {
            var context = GetInMemoryContext();
            context.Specialists.Add(new Specialist { Id = 1, FirstName = "Elena", IsActive = true });
            context.Services.Add(new Service { Id = 1, Name = "Haircut", DurationMinutes = 30, IsActive = true });
            context.Schedules.Add(new Schedule
            {
                SpecialistId = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(12)
            });
            await context.SaveChangesAsync();

            var slotService = new SlotService(context);
            var fromDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local);

            var slots = await slotService.GetAvailableSlots(1, 1, fromDate);

            // ƒл€ 30-минутной услуги в интервале 10Ц12 генерируютс€ слоты:
            // 10:00, 10:30, 11:00, 11:30 на каждый понедельник (1 и 8 июн€) ? 8 слотов
            Assert.Equal(8, slots.Count);
            Assert.Contains(new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Local), slots);
            Assert.Contains(new DateTime(2026, 6, 1, 11, 30, 0, DateTimeKind.Local), slots);
            Assert.Contains(new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Local), slots);
            Assert.Contains(new DateTime(2026, 6, 8, 11, 30, 0, DateTimeKind.Local), slots);
        }
        // ================== TryCreateAppointment ==================

        [Fact]
        public async Task TryCreateAppointment_CreatesRecordWhenSlotIsFree()
        {
            var context = GetInMemoryContext();
            context.Services.Add(new Service { Id = 1, DurationMinutes = 60 });
            await context.SaveChangesAsync();

            var slotService = new SlotService(context);
            var startTime = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Local);

            bool result = await slotService.TryCreateAppointment(1, 1, 1, startTime, null);

            Assert.True(result);
            Assert.Single(context.Appointments);
            var appt = context.Appointments.First();
            Assert.Equal(startTime, appt.StartTime);
            Assert.Equal(AppointmentStatus.Confirmed, appt.Status);
        }

        [Fact]
        public async Task TryCreateAppointment_ReturnsFalseWhenTimeIsBusy()
        {
            var context = GetInMemoryContext();
            var busyDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Local);
            context.Appointments.Add(new Appointment
            {
                SpecialistId = 1,
                ServiceId = 1,
                StartTime = busyDate,
                EndTime = busyDate.AddHours(1),
                Status = AppointmentStatus.Confirmed,
                UserId = 1
            });
            context.Services.Add(new Service { Id = 1, DurationMinutes = 60 });
            await context.SaveChangesAsync();

            var slotService = new SlotService(context);
            bool result = await slotService.TryCreateAppointment(2, 1, 1, busyDate.AddMinutes(30), null);

            Assert.False(result);
            Assert.Single(context.Appointments);
        }

        [Fact]
        public async Task TryCreateAppointment_ReturnsTrueWhenOverlappingWithCanceled()
        {
            var context = GetInMemoryContext();
            var canceledDate = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Local);
            context.Appointments.Add(new Appointment
            {
                SpecialistId = 1,
                ServiceId = 1,
                StartTime = canceledDate,
                EndTime = canceledDate.AddHours(1),
                Status = AppointmentStatus.Canceled,
                UserId = 1
            });
            context.Services.Add(new Service { Id = 1, DurationMinutes = 60 });
            await context.SaveChangesAsync();

            var slotService = new SlotService(context);
            bool result = await slotService.TryCreateAppointment(2, 1, 1, canceledDate, null);

            Assert.True(result);
            Assert.Equal(2, context.Appointments.Count());
        }

        [Fact]
        public async Task TryCreateAppointment_ReturnsFalseWhenExactSameTime()
        {
            var context = GetInMemoryContext();
            var start = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Local);
            context.Appointments.Add(new Appointment
            {
                SpecialistId = 1,
                ServiceId = 1,
                StartTime = start,
                EndTime = start.AddHours(1),
                Status = AppointmentStatus.Confirmed,
                UserId = 1
            });
            context.Services.Add(new Service { Id = 1, DurationMinutes = 60 });
            await context.SaveChangesAsync();

            var slotService = new SlotService(context);
            bool result = await slotService.TryCreateAppointment(2, 1, 1, start, null);

            Assert.False(result);
            Assert.Single(context.Appointments);
        }
    }
}