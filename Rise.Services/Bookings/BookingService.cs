using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Services.Weather;
using Rise.Shared.Addresses;
using Rise.Shared.Batteries;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;
using Rise.Shared.Emails;
using Rise.Shared.Emails.Models;
using Rise.Shared.Payments;
using Rise.Shared.Payments;
using Rise.Shared.Prices;
using Rise.Shared.Users;
using Rise.Shared.Weather;

namespace Rise.Services.Bookings;

public class BookingService(
    ApplicationDbContext dbContext,
    IBoatService boatService,
    IEmailTemplateService templateService,
    IEmailService emailService,
    IWeatherService weatherService
) : IBookingService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IBoatService _boatService = boatService;

    private readonly IEmailTemplateService _templateService = templateService;

    private readonly IEmailService _emailService = emailService;

    private readonly IWeatherService _weatherService = weatherService;

    private readonly ILogger<BookingService> _Logger;

    public async Task<IEnumerable<BookingDto.Index>?> GetAllBookingsAsync()
    {
        // Update weather data before fetching bookings
        await _weatherService.FetchAndStoreWeatherDataAsync();

        var query =
            from booking in _dbContext.Bookings
            where !booking.IsDeleted
            orderby booking.RentalDateTime
            join payment in _dbContext.Payments on booking.Id equals payment.BookingId into payments
            from payment in payments.DefaultIfEmpty()
            join weather in _dbContext.Weather
                on booking.RentalDateTime equals weather.Date
                into weatherGroup
            from weather in weatherGroup.DefaultIfEmpty() // Zorg ervoor dat er altijd een waarde is, zelfs als er geen weer is
            select new BookingDto.Index
            {
                Id = booking.Id,
                RentalDateTime = booking.RentalDateTime,
                Boat =
                    booking.Boat == null
                        ? null
                        : new BoatDto.BoatIndex { Id = booking.Boat.Id, Name = booking.Boat.Name },
                Battery =
                    booking.Battery == null
                        ? null
                        : new BatteryDto.BatteryIndex
                        {
                            Id = booking.Battery.Id,
                            Name = booking.Battery.Name,
                            Status = booking.Battery.Status,
                        },
                Status = booking.Status,
                User = new UserDto.Index
                {
                    Id = booking.User.Id,
                    Firstname = booking.User.Firstname ?? null,
                    Lastname = booking.User.Lastname ?? null,
                },
                Payment =
                    payment == null
                        ? null
                        : new PaymentResponseDto
                        {
                            Id = payment.PaymentId,
                            Status = payment.Status,
                            Amount = new AmountDto(payment.Amount),
                            CreatedAt = payment.Timestamp,
                        },
                Weather =
                    weather == null
                        ? null
                        : new WeatherDto
                        {
                            Date = weather.Date,
                            Temperature = weather.Temperature,
                            WeatherCode = weather.WeatherCode,
                        },
            };

        var bookings = await query.ToListAsync();

        return bookings;
    }

    public async Task<IEnumerable<BookingDto.Index>?> GetAllCurrentBookingsAsync()
    {
        await _weatherService.FetchAndStoreWeatherDataAsync();
        IQueryable<BookingDto.Index> query = _dbContext
            .Bookings.Where(x =>
                !x.IsDeleted
                && x.RentalDateTime >= DateTime.Today
                && x.Status == BookingStatus.Active
            )
            .Select(booking => new BookingDto.Index
            {
                //No User!!
                Id = booking.Id,
                RentalDateTime = booking.RentalDateTime,
                Status = booking.Status,
                User = new UserDto.Index { Id = booking.User.Id },
            });

        var bookings = await query.ToListAsync();

        return bookings;
    }

    public async Task<BookingDto.Detail?> GetBookingByIdAsync(int bookingId)
    {
        BookingDto.Detail? booking =
            await _dbContext
                .Bookings.Where(b => !b.IsDeleted)
                .Select(b => new BookingDto.Detail
                {
                    Id = b.Id,
                    RentalDateTime = b.RentalDateTime,
                    Remark = b.Remark,
                    Boat =
                        b.Boat == null
                            ? null
                            : new BoatDto.BoatIndex { Id = b.Boat.Id, Name = b.Boat.Name },

                    Battery =
                        b.Battery == null
                            ? null
                            : new BatteryDto.BatteryDetail
                            {
                                Id = b.Battery.Id,
                                Name = b.Battery.Name,
                                Status = b.Battery.Status,
                                User = new UserDto.Index
                                {
                                    Id = b.Battery.Id,
                                    Auth0UserId = b.Battery.User.Auth0UserId,
                                    Email = b.Battery.User.Email,
                                    Firstname = b.Battery.User.Firstname ?? null,
                                    Lastname = b.Battery.User.Lastname ?? null,
                                    PhoneNumber = b.Battery.User.PhoneNumber ?? null,
                                },
                            },
                    User = new UserDto.Index
                    {
                        Id = b.User.Id,
                        Firstname = b.User.Firstname,
                        Lastname = b.User.Lastname,
                        Auth0UserId = b.User.Auth0UserId,
                        Email = b.User.Email,
                        PhoneNumber = b.User.PhoneNumber,
                        BirthDay = b.User.BirthDay,
                        Address =
                            b.User.Address != null
                                ? new AddressDto
                                {
                                    Id = b.User.Address.Id,
                                    Street = b.User.Address.Street,
                                    HouseNumber = b.User.Address.HouseNumber,
                                    UnitNumber = b.User.Address.UnitNumber,
                                    City = b.User.Address.City,
                                    PostalCode = b.User.Address.PostalCode,
                                }
                                : null,
                        IsRegistrationComplete = b.User.IsRegistrationComplete,
                    },
                    Status = b.Status,
                    Price =
                        b.Price != null
                            ? new PriceDto.Index { Id = b.Price.Id, Amount = b.Price.Amount }
                            : null,
                })
                .SingleOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new KeyNotFoundException($"Booking with ID {bookingId} was not found.");

        var payment = await _dbContext
            .Payments.Where(p => p.BookingId == bookingId)
            .Select(p => new PaymentResponseDto(
                p.Id.ToString(),
                p.Status,
                new AmountDto(p.Amount),
                p.CreatedAt
            ))
            .FirstOrDefaultAsync();

        if (payment != null)
        {
            booking.Payment = payment;
        }

        return booking;
    }

    public async Task<IEnumerable<BookingDto.Index>?> GetBookingsByUserIdAsync(int userId)
    {
        IQueryable<BookingDto.Index> query = _dbContext
            .Bookings.Where(b => b.User.Id == userId && !b.IsDeleted)
            .OrderBy(x => x.RentalDateTime)
            .Select(x => new
            {
                Booking = x,
                Weather = _dbContext.Weather.FirstOrDefault(w => w.Date == x.RentalDateTime),
            })
            .Select(x => new BookingDto.Index
            {
                Id = x.Booking.Id,
                RentalDateTime = x.Booking.RentalDateTime,
                Boat =
                    x.Booking.Boat == null
                        ? null
                        : new BoatDto.BoatIndex
                        {
                            Id = x.Booking.Boat.Id,
                            Name = x.Booking.Boat.Name,
                        },
                Battery =
                    x.Booking.Battery == null
                        ? null
                        : new BatteryDto.BatteryIndex
                        {
                            Id = x.Booking.Battery.Id,
                            Name = x.Booking.Battery.Name,
                            Status = x.Booking.Battery.Status,
                        },
                Status = x.Booking.Status,
                User = new UserDto.Index
                {
                    Id = x.Booking.User.Id,
                    Firstname = x.Booking.User.Firstname ?? null,
                    Lastname = x.Booking.User.Lastname ?? null,
                },
                Weather =
                    x.Weather == null
                        ? null
                        : new WeatherDto
                        {
                            Date = x.Weather.Date,
                            Temperature = x.Weather.Temperature,
                            WeatherCode = x.Weather.WeatherCode,
                        },
            });

        var bookings = await query.ToListAsync();

        return bookings;
    }

    public async Task<(int bookingId, string paymentUrl)> CreateBookingAsync(
        BookingDto.Mutate model
    )
    {
        await ValidateNoExistingUserBooking(model.UserId, model.RentalDateTime);

        int existingBookingsCount = await _dbContext.Bookings.CountAsync(x =>
            !x.IsDeleted
            && x.Status == BookingStatus.Active
            && x.RentalDateTime == model.RentalDateTime
        );

        var blockedTimeSlot = await _dbContext.TimeSlots.FirstOrDefaultAsync(x =>
            x.Date == model.RentalDateTime
        );

        if (blockedTimeSlot != null)
        {
            throw new InvalidOperationException("The specified rental date and time are blocked.");
        }

        if (existingBookingsCount >= await _boatService.GetAvailableBoatsCountAsync())
        {
            throw new InvalidOperationException(
                "The specified rental date and time are fully booked."
            );
        }
        Boat? boat = null;

        if (model.BoatId != null)
        {
            boat =
                await _dbContext.Boats.FirstOrDefaultAsync(b =>
                    !b.IsDeleted && b.Id == model.BoatId
                ) ?? throw new KeyNotFoundException($"Boat with ID {model.BoatId} not found.");

            if (boat.Status != BoatStatus.Available)
            {
                throw new InvalidOperationException("The specified boat is not available.");
            }
            int existingBookingsCountForBoat = await _dbContext.Bookings.CountAsync(x =>
                !x.IsDeleted
                && x.Status != BookingStatus.Canceled
                && x.RentalDateTime == model.RentalDateTime
                && x.BoatId == model.BoatId
            );
            if (existingBookingsCountForBoat >= 1)
            {
                throw new InvalidOperationException(
                    "The specified boat is already booked for the specified rental date and time."
                );
            }
        }
        Battery? battery = null;
        if (model.BatteryId != null)
        {
            battery =
                await _dbContext.Batteries.FirstOrDefaultAsync(b =>
                    !b.IsDeleted && b.Id == model.BatteryId
                )
                ?? throw new KeyNotFoundException($"Battery with ID {model.BatteryId} not found.");

            if (battery.Status != BatteryStatus.Available)
            {
                throw new InvalidOperationException("The specified battery is not available.");
            }
            int existingBookingsCountForBatteryOnSameDay = await _dbContext.Bookings.CountAsync(x =>
                !x.IsDeleted
                && x.Status != BookingStatus.Canceled
                && x.RentalDateTime.Date == model.RentalDateTime.Date
                && x.BatteryId == model.BatteryId
            );
            if (existingBookingsCountForBatteryOnSameDay >= 1)
            {
                throw new InvalidOperationException(
                    "The specified battery is already booked for the specified rental date."
                );
            }
        }

        var user =
            await _dbContext.Users.FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == model.UserId)
            ?? throw new KeyNotFoundException($"User with ID {model.UserId} not found.");

        var price =
            await _dbContext.Prices.FirstOrDefaultAsync(p => !p.IsDeleted && p.Id == model.PriceId)
            ?? throw new KeyNotFoundException($"Price with ID {model.PriceId} not found.");

        Booking booking =
            new(boat, battery, model.RentalDateTime, model.Status, user, price)
            {
                BoatId = boat?.Id,
                BatteryId = battery?.Id,
                Remark = model.Remark,
            };
        _dbContext.Bookings.Add(booking);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to save the booking to the database.", ex);
        }

        try
        {
            await SendBookingConfirmedEmailAsync(booking);
        }
        catch { }

        return (booking.Id, "");
    }

    public async Task<bool> UpdateBookingAsync(int bookingId, BookingDto.Mutate model)
    {
        if (model.Status == BookingStatus.Canceled)
        {
            return await CancelBookingAsync(bookingId);
        }

        var booking = await _dbContext.Bookings.FindAsync(bookingId);
        if (booking is null || booking.IsDeleted)
        {
            return false;
        }
        if (model.BoatId is not null)
        {
            var boat = await _dbContext.Boats.FindAsync(model.BoatId);
            if (boat is null || booking.IsDeleted)
            {
                throw new KeyNotFoundException($"Boat with ID {model.BoatId} not found.");
            }
            booking.BoatId = model.BoatId;
        }
        else
        {
            booking.BoatId = null;
        }

        if (model.BatteryId is not null)
        {
            var battery = await _dbContext.Batteries.FindAsync(model.BatteryId);
            if (battery is null || battery.IsDeleted)
            {
                throw new KeyNotFoundException($"Battery with ID {model.BatteryId} not found.");
            }
            booking.BatteryId = model.BatteryId;
        }
        else
        {
            booking.BatteryId = null;
        }

        booking.RentalDateTime = model.RentalDateTime;
        booking.Status = model.Status;
        booking.Remark = model.Remark;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelBookingAsync(int bookingId)
    {
        var bookingDto = await GetBookingByIdAsync(bookingId);

        var booking =
            await _dbContext
                .Bookings.Include(b => b.User)
                .SingleOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new KeyNotFoundException($"Booking with ID {bookingId} was not found.");

        if (booking.Status == BookingStatus.Canceled)
        {
            throw new InvalidOperationException("The booking has already been canceled.");
        }
        booking.Status = BookingStatus.Canceled;
        booking.Remark = bookingDto.Remark;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteBookingAsync(int bookingId)
    {
        var booking = await _dbContext.Bookings.FindAsync(bookingId);
        if (booking is null)
        {
            return false;
        }

        booking.IsDeleted = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    //private helper functions

    private async Task ValidateNoExistingUserBooking(int userId, DateTime rentalDateTime)
    {
        var existingUserBooking = await _dbContext.Bookings.FirstOrDefaultAsync(x =>
            !x.IsDeleted
            && x.Status != BookingStatus.Canceled
            && x.RentalDateTime == rentalDateTime
            && x.UserId == userId
        );

        if (existingUserBooking != null)
        {
            throw new InvalidOperationException(
                "You already have a booking for the specified rental date and time."
            );
        }
    }

    public async Task<decimal> CalculateTotalAmountForBooking(BookingDto.Mutate booking)
    {
        var price = await _dbContext.Prices.FirstOrDefaultAsync();

        if (price == null)
        {
            throw new InvalidOperationException("No price found.");
        }

        return price.Amount;
    }

    public static Task<string> CreateBookingAndPaymentAsync(BookingDto.Mutate booking)
    {
        throw new NotImplementedException();
    }

    public async Task SendBookingConfirmedEmailAsync(Booking booking)
    {
        var model = new BookingConfirmedOrCanceledEmailModel
        {
            FirstName = booking.User?.Firstname,
            RentalDate = booking.RentalDateTime,
            BookingId = booking.Id.ToString(),
        };

        string emailContent = await _templateService.RenderTemplateAsync("BookingConfirmed", model);

        var email = await _dbContext
            .Users.Where(u => u.Id == booking.UserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        await _emailService.SendTemplatedEmailAsync(email!, "Boeking bevestigd", emailContent);
    }

    public async Task<IEnumerable<DateTime>> GetCurrentFullyBookedSlots()
    {
        var activeBoatCount = await _boatService.GetAvailableBoatsCountAsync();
        var bookings = await GetAllCurrentBookingsAsync();

        // Since we already have the bookings in memory, use LINQ to Objects
        var fullyBookedSlots = bookings
            .GroupBy(b => b.RentalDateTime)
            .Where(g => g.Count() >= activeBoatCount)
            .Select(g => g.Key)
            .OrderBy(dt => dt)
            .ToList(); // Use regular ToList() since data is in memory

        return fullyBookedSlots;
    }
}
