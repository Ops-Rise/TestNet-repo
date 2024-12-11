using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Domain.TimeSlots;
using Rise.Persistence;
using Rise.Shared.Bookings;
using Rise.Shared.Emails;
using Rise.Shared.Emails;
using Rise.Shared.Emails.Models;
using Rise.Shared.TimeSlots;

namespace Rise.Services.TimeSlots
{
    public class TimeSlotService : ITimeSlotService
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly IBookingService _bookingService;

        private readonly IEmailService _emailService;
        IEmailTemplateService _templateService;

        public TimeSlotService(
            ApplicationDbContext dbContext,
            IBookingService bookingService,
            IEmailService emailService,
            IEmailTemplateService templateService
        )
        {
            _dbContext = dbContext;
            _bookingService = bookingService;
            _emailService = emailService;
            _templateService = templateService;
        }

        public async Task<IEnumerable<TimeSlotDto>?> GetAllTimeSlotsAsync(
            DateTime startDate,
            DateTime endDate
        )
        {
            return (IEnumerable<TimeSlotDto>?)
                await _dbContext
                    .TimeSlots.Where(b =>
                        b.Date.Date >= startDate.Date && b.Date.Date <= endDate.Date
                    )
                    .Select(b => new TimeSlotDto
                    {
                        Date = b.Date,
                        TimeSlot = (int)b.Type,
                        CreatedByUserId = b.CreatedByUserId,
                        Reason = b.Reason,
                    })
                    .ToListAsync();
        }

        public async Task BlockTimeSlotAsync(TimeSlotDto model)
        {
            var isBlocked = await _dbContext.TimeSlots.AnyAsync(b =>
                b.Date.Date == model.Date && (int)b.Type == model.TimeSlot
            );

            if (isBlocked)
                return;

            var bookingsToBeCanceled = await _dbContext
                .Bookings.Where(b =>
                    b.RentalDateTime.Date == model.Date.Date
                    && b.RentalDateTime.Hour == model.Date.Hour
                    && !b.IsDeleted
                    && b.Status == BookingStatus.Active
                )
                .Include(b => b.User)
                .ToListAsync();

            foreach (var booking in bookingsToBeCanceled)
            {
                Console.WriteLine("Cancelling booking: " + booking.Id);

                var success = await _bookingService.CancelBookingAsync(booking.Id);
                if (success)
                {
                    booking.Remark =
                        "Boeking geannuleerd wegens het blokkeren van dit tijdslot met reden: "
                        + model.Reason;

                    await SendBookingCanceledEmail(booking);
                }
            }

            var blockedSlot = new TimeSlot
            {
                Date = model.Date,
                Type = (TimeSlot.TimeSlotType)model.TimeSlot,
                Reason = model.Reason,
                CreatedByUserId = model.CreatedByUserId,
                CreatedAt = DateTime.UtcNow,
            };

            _dbContext.TimeSlots.Add(blockedSlot);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SendBookingCanceledEmail(Booking booking)
        {
            var model = new BookingConfirmedOrCanceledEmailModel
            {
                FirstName = booking.User?.Firstname,
                RentalDate = booking.RentalDateTime,
                BookingId = booking.Id.ToString(),
                Remark = booking.Remark,
            };

            string emailContent = await _templateService.RenderTemplateAsync(
                "BookingCanceled",
                model
            );
            var email = await _dbContext
                .Users.Where(u => u.Id == booking.UserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            await _emailService.SendTemplatedEmailAsync(
                email!,
                "Boeking geannuleerd",
                emailContent
            );
        }

        public async Task<bool> UnblockTimeSlotAsync(DateTime date, int timeSlot)
        {
            try
            {
                date = date.Date;

                var blockedSlot = await _dbContext.TimeSlots.FirstOrDefaultAsync(b =>
                    b.Date.Date == date && (int)b.Type == timeSlot
                );

                if (blockedSlot != null)
                {
                    _dbContext.TimeSlots.Remove(blockedSlot);

                    await _dbContext.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
