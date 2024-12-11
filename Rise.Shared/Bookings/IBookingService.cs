using System.Threading;
using Rise.Shared.TimeSlots;

namespace Rise.Shared.Bookings;

public interface IBookingService
{
    Task<IEnumerable<BookingDto.Index>?> GetAllBookingsAsync();
    Task<IEnumerable<BookingDto.Index>?> GetBookingsByUserIdAsync(int userId);
    Task<(int bookingId, string paymentUrl)> CreateBookingAsync(BookingDto.Mutate model);
    Task<BookingDto.Detail?> GetBookingByIdAsync(int bookingId);
    Task<IEnumerable<BookingDto.Index>?> GetAllCurrentBookingsAsync();
    Task<IEnumerable<DateTime>?> GetCurrentFullyBookedSlots();
    Task<bool> UpdateBookingAsync(int bookingId, BookingDto.Mutate model);
    Task<bool> CancelBookingAsync(int bookingId);
    Task<bool> DeleteBookingAsync(int bookingId);
    Task<decimal> CalculateTotalAmountForBooking(BookingDto.Mutate booking);
}
