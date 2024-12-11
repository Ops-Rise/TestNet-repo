using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Shared.Batteries;
using Rise.Shared.Bookings;
using Rise.Shared.Emails;
using Rise.Shared.Emails.Models;

namespace Rise.Services.Batteries
{
    /// <summary>
    /// Implementeert de logica voor het toewijzen van beschikbare batterijen aan boekingen.
    /// </summary>
    public class BatteryAndBoatAssignmentProcessor(
        ILogger<BatteryAndBoatAssignmentProcessor> logger,
        ApplicationDbContext dbContext,
        IBatteryService batteryService,
        IBookingService bookingService,
        IEmailService emailService,
        IEmailTemplateService templateService
    ) : IBatteryAndBoatAssignmentProcessor
    {
        private readonly ILogger<BatteryAndBoatAssignmentProcessor> _logger = logger;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly IBatteryService _batteryService = batteryService;
        private readonly IBookingService _bookingService = bookingService;

        private readonly IEmailTemplateService _templateService = templateService;

        private readonly IEmailService _emailService = emailService;

        /// <inheritdoc />
        public async Task ProcessBatteryAndBoatAssignmentsAsync()
        {
            _logger.LogInformation("Processing battery assignments");
            try
            {
                await AssignBatteriesAndBoatsToBookingsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing battery assignments.");
            }
        }

        /// <summary>
        /// Haalt boekingen op die binnen drie dagen plaatsvinden en waarvoor geen batterij is toegewezen.
        /// </summary>
        /// <returns>Een lijst met boekingen zonder toegewezen batterij.</returns>
        private async Task<IEnumerable<Booking>> GetBookingsInThreeDaysWithoutBatteryAsync()
        {
            var today = DateTime.Today;
            var threeDaysFromNow = today.AddDays(3);
            try
            {
                var query = _dbContext.Bookings.Where(b =>
                    b.RentalDateTime >= today
                    && b.RentalDateTime <= threeDaysFromNow
                    && b.Status == BookingStatus.Active
                    && b.Battery == null
                    && !b.IsDeleted
                );

                var bookings = await query.ToListAsync();
                return bookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while fetching bookings due in 3 days without a battery."
                );
                return new List<Booking>();
            }
        }

        /// <summary>
        /// Haalt boekingen op die binnen drie dagen plaatsvinden en waarvoor een batterij, maar geen boot is toegewezen.
        /// </summary>
        /// <returns>Een lijst met boekingen met batterij zonder toegewezen boot.</returns>
        private async Task<IEnumerable<Booking>> GetBookingsInThreeDaysWithBatteryWithoutBoatAsync()
        {
            var today = DateTime.Today;
            var threeDaysFromNow = today.AddDays(3);
            try
            {
                var query = _dbContext.Bookings.Where(b =>
                    b.RentalDateTime >= today
                    && b.RentalDateTime <= threeDaysFromNow
                    && b.Status == BookingStatus.Active
                    && b.Battery != null
                    && b.Boat == null
                    && !b.IsDeleted
                );

                var bookings = await query.ToListAsync();
                return bookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while fetching bookings due in 3 days with a battery but without a boat."
                );
                return new List<Booking>();
            }
        }

        /// <summary>
        /// Haalt beschikbare en reservebatterijen op, gesorteerd op laatste gebruiksdatum en gebruikscycli.
        /// </summary>
        /// <returns>Een lijst met beschikbare en reservebatterijen.</returns>
        private async Task<IEnumerable<BatteryDto.BatteryDetail>> GetAvailableBatteriesAsync()
        {
            try
            {
                var batteries = await _batteryService.GetAllBatteriesWithDetailsAsync();
                var availableOrReserveBatteries = batteries
                    .Where(b =>
                        (
                            b.Status == BatteryStatus.Available
                            || b.Status == BatteryStatus.Reserve
                        )
                    )
                    .OrderBy(b => b.DateLastUsed)
                    .ThenBy(b => b.UseCycles)
                    .ToList();

                return availableOrReserveBatteries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching available batteries.");
                return new List<BatteryDto.BatteryDetail>();
            }
        }

        /// <summary>
        /// Verwijst beschikbare en reservebatterijen toe aan boekingen die binnen drie dagen plaatsvinden.
        /// </summary>
        private async Task AssignBatteriesAndBoatsToBookingsAsync()
        {
            try
            {
                var bookings1 = await GetBookingsInThreeDaysWithoutBatteryAsync();

                var bookings2 = await GetBookingsInThreeDaysWithBatteryWithoutBoatAsync();

                if (!bookings1.Any() && !bookings2.Any())
                {
                    _logger.LogInformation(
                        "No bookings due in 3 days without a battery/boat found."
                    );
                    return;
                }
                _logger.LogInformation(
                    "Found {Bookings1Count} bookings due in 3 days without a battery.",
                    bookings1.Count()
                );
                _logger.LogInformation(
                    "Found {Bookings2Count} bookings due in 3 days with a battery without a boat.",
                    bookings2.Count()
                );

                if (bookings1.Any())
                {
                    var availableOrReserveBatteries = await GetAvailableBatteriesAsync();
                    _logger.LogInformation(
                        "Found {AvailableOrReserveBatteriesCount} available and reserve batteries.",
                        availableOrReserveBatteries.Count()
                    );

                    var reserveBatteries = availableOrReserveBatteries
                        .Where(b => b.Status == BatteryStatus.Reserve)
                        .ToList();

                    var availableBatteries = availableOrReserveBatteries
                        .Where(b => b.Status == BatteryStatus.Available)
                        .ToList();

                    foreach (var booking in bookings1)
                    {
                        await ProcessBookingAsync(booking, availableBatteries, reserveBatteries);
                    }
                }
                if (bookings2.Any())
                {
                    foreach (var booking in bookings2)
                    {
                        await AssignBoatToBooking(booking);
                        await SendBatteryAndBoatAssignedEmailAsync(
                            booking,
                            new BatteryDto.BatteryDetail
                            {
                                Id = booking.BatteryId!.Value,
                                Name = booking.Battery!.Name,
                                Status = booking.Battery!.Status,
                            }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while assigning batteries to bookings.");
            }
        }

        /// <summary>
        /// Wijst een batterij toe aan een boeking, of annuleert de boeking als er geen batterijen beschikbaar zijn. Indien geen boot toegewezen is wordt een boot toegewezen
        /// </summary>
        /// <param name="booking">De boeking die moet worden verwerkt.</param>
        /// <param name="availableBatteries">De lijst met beschikbare batterijen.</param>
        /// <param name="reserveBatteries">De lijst met reservebatterijen.</param>
        private async Task ProcessBookingAsync(
            Booking booking,
            List<BatteryDto.BatteryDetail> availableBatteries,
            List<BatteryDto.BatteryDetail> reserveBatteries
        )
        {
            var battery = FindBattery(booking, availableBatteries);
            if (battery != null)
            {
                await AssignBatteryAndBoatToBookingAsync(booking, battery);
                await SendBatteryAndBoatAssignedEmailAsync(booking, battery);
                availableBatteries.Remove(battery);
            }
            else
            {
                battery = FindBattery(booking, reserveBatteries);
                if (battery != null)
                {
                    await AssignBatteryAndBoatToBookingAsync(booking, battery);
                    await SendBatteryAndBoatAssignedEmailAsync(booking, battery);
                    reserveBatteries.Remove(battery);
                }
                else
                {
                    await CancelBookingAsync(booking, true);
                }
            }
        }

        /// <summary>
        /// Zoekt naar een batterij die beschikbaar is voor een specifieke boeking, gebaseerd op de datum.
        /// </summary>
        /// <param name="booking">De boeking waarvoor een batterij nodig is.</param>
        /// <param name="batteries">De lijst met batterijen om door te zoeken.</param>
        /// <returns>Een batterij die niet op dezelfde datum eerder is gebruikt, of null als geen gevonden.</returns>
        private static BatteryDto.BatteryDetail? FindBattery(
            Booking booking,
            List<BatteryDto.BatteryDetail> batteries
        )
        {
            return batteries.Find(b => b.DateLastUsed.Date != booking.RentalDateTime.Date);
        }

        /// <summary>
        /// Wijst een batterij toe aan een boeking en markeert de boeking als actief, indien nog geen boot toegewezen wordt er een boot toegewezen.
        /// </summary>
        /// <param name="booking">De boeking die een batterij krijgt toegewezen.</param>
        /// <param name="battery">De batterij die wordt toegewezen.</param>
        private async Task AssignBatteryAndBoatToBookingAsync(
            Booking booking,
            BatteryDto.BatteryDetail battery
        )
        {
            try
            {
                var isReserveBattery = battery.Status == BatteryStatus.Reserve;
                _logger.LogInformation(
                    "Assigning {BatteryType} battery {BatteryId} to booking {BookingId} ",
                    isReserveBattery ? "reserve" : "available",
                    battery.Id,
                    booking.Id
                );

                var updateModel = new BookingDto.Mutate
                {
                    RentalDateTime = booking.RentalDateTime,
                    Status = BookingStatus.Active,
                    BoatId = booking.Boat?.Id,
                    BatteryId = battery.Id,
                    UserId = booking.UserId,
                    PriceId = booking.PriceId,
                };

                await _bookingService.UpdateBookingAsync(booking.Id, updateModel);
                if (booking.BoatId == null)
                {
                    await AssignBoatToBooking(booking);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error assigning battery {BatteryId} to booking {BookingId} for user with id {BookingUserId}.",
                    battery.Id,
                    booking.Id,
                    booking.UserId
                );
            }
        }

        /// <summary>
        /// Annuleert een boeking omdat er geen batterijen of boten beschikbaar zijn.
        /// </summary>
        /// <param name="booking">De boeking die moet worden geannuleerd.</param>
        ///  <param name="noBattery">True als er geen batterij is, false als er geen boot is</param>
        private async Task CancelBookingAsync(Booking booking, bool noBattery)
        {
            try
            {
                string ResourceType = noBattery ? "battery" : "boat";
                _logger.LogInformation(
                    "Canceling booking {BookingId} on {RentalDate} for user {UserId} - no {ResourceType} available",
                    booking.Id,
                    booking.RentalDateTime,
                    booking.UserId,
                    ResourceType
                );

                var updateModel = new BookingDto.Mutate
                {
                    RentalDateTime = booking.RentalDateTime,
                    Status = (BookingStatus) BookingStatus.Canceled,
                    BoatId = booking.Boat?.Id,
                    BatteryId = booking.Battery?.Id,
                    UserId = booking.UserId,
                    PriceId = booking.PriceId,
                };

                await _bookingService.UpdateBookingAsync(booking.Id, updateModel);
                //cancel email send in booking service ✓
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while canceling booking {BookingId}.",
                    booking.Id
                );
            }
        }

        /// <summary>
        /// Wijst een boot toe aan een boeking.
        /// </summary>
        /// <param name="booking">De boeking die een boot krijgt toegewezen.</param>
        private async Task AssignBoatToBooking(Booking booking)
        {
            try
            {
                var boatId = await GetBoatIdFromBoatWithLongestIdleTime(booking);
                if (boatId == -1)
                {
                    await CancelBookingAsync(booking, false);
                }
                else
                {
                    var updateModel = new BookingDto.Mutate
                    {
                        RentalDateTime = booking.RentalDateTime,
                        BoatId = boatId,
                        BatteryId = booking.Battery?.Id,
                        UserId = booking.UserId,
                        PriceId = booking.PriceId,
                    };

                    await _bookingService.UpdateBookingAsync(booking.Id, updateModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error assigning boat to booking {BookingId} for user with id {BookingUserId}.",
                    booking.Id,
                    booking.UserId
                );
            }
        }

        /// <summary>
        /// Haalt het ID van de boot op die het langst niet is gebruikt, of nog nooit is gebruikt
        /// </summary>
        /// <returns>Id van de boot met de langste inactiviteit</returns>
        private async Task<int> GetBoatIdFromBoatWithLongestIdleTime(Booking booking)
        {
            var boatWithLongestIdleTime = await _dbContext
                .Boats.Where(boat => boat.Status == BoatStatus.Available)
                .GroupJoin(
                    _dbContext.Bookings,
                    boat => boat.Id,
                    booking => booking.BoatId,
                    (boat, bookings) =>
                        new
                        {
                            Boat = boat,
                            LastBookingDate = bookings.Max(b => (DateTime?)b.RentalDateTime),
                        }
                )
                .OrderBy(b => b.LastBookingDate ?? DateTime.MinValue) // => boten zonder boeking eerst
                .Where(b => b.LastBookingDate != booking.RentalDateTime) // geen boot die op hetzelfde moment is geboekt
                .FirstOrDefaultAsync();

            //no boat found
            if (boatWithLongestIdleTime == null)
            {
                return -1;
            }
            return boatWithLongestIdleTime.Boat.Id;
        }

        public async Task SendBatteryAndBoatAssignedEmailAsync(
            Booking booking,
            BatteryDto.BatteryDetail battery
        )
        {
            var model = new BatteryAssignedEmailModel
            {
                FirstName = booking.User?.Firstname,
                RentalDate = booking.RentalDateTime,
                BoatName = booking.Boat?.Name,
                BatteryName = battery?.Name,
                UserId = booking.UserId.ToString(),
                FirstnameBatteryUser = battery?.User?.Firstname,
                LastnameBatteryUser = battery?.User?.Lastname,
                PhoneNumberBatteryUser = battery?.User?.PhoneNumber,
            };

            string emailContent = await _templateService.RenderTemplateAsync(
                "BatteryAndBoatAssigned",
                model
            );
            var email =  await _dbContext
                .Users.Where(u => u.Id == booking.UserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            await _emailService.SendEmailAsync(email!, "Batterij en boot toegewezen", emailContent);
        }
    }
}
