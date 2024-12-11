using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Rise.Domain.Bookings;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

public class BookingService(HttpClient httpClient, NavigationManager navigationManager)
    : IBookingService
{
    private const string endpoint = "booking";

    public async Task<IEnumerable<BookingDto.Index>?> GetAllBookingsAsync()
    {
        var response = await httpClient.GetAsync("booking");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();
    }

    public async Task<IEnumerable<BookingDto.Index>?> GetBookingsByUserIdAsync(int userId)
    {
        var response = await httpClient.GetAsync($"booking/user/{userId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();
    }

    public async Task<IEnumerable<BookingDto.Index>?> GetAllCurrentBookingsAsync()
    {
        var response = await httpClient.GetAsync($"{endpoint}/current");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();
    }

    public async Task<(int bookingId, string paymentUrl)> CreateBookingAsync(
        BookingDto.Mutate model
    )
    {
        model.Status = BookingStatus.Active;
        
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(model)
        };

        var frontendUrl = new Uri(navigationManager.Uri).GetLeftPart(UriPartial.Authority);
        Console.WriteLine(frontendUrl);
        request.Headers.Add("X-Redirect-Base", frontendUrl);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        if (
            !result.TryGetProperty("bookingId", out var bookingIdElement)
            || !result.TryGetProperty("paymentUrl", out var paymentUrlElement)
        )
        {
            throw new InvalidOperationException(
                "Response from server is missing required properties: 'bookingId' or 'paymentUrl'."
            );
        }

        // Parse en retourneer de waarden
        int bookingId = bookingIdElement.GetInt32();
        string paymentUrl = paymentUrlElement.GetString();

        return (bookingId, paymentUrl);
    }



    public async Task<BookingDto.Detail?> GetBookingByIdAsync(int bookingId)
    {
        var response = await httpClient.GetAsync($"{endpoint}/{bookingId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BookingDto.Detail>();
    }

    public async Task<bool> UpdateBookingAsync(int bookingId, BookingDto.Mutate model)
    {
        var response = await httpClient.PutAsJsonAsync($"{endpoint}/{bookingId}", model);
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteBookingAsync(int bookingId)
    {
        var response = await httpClient.DeleteAsync($"{endpoint}/{bookingId}");
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }

    public Task<decimal> CalculateTotalAmountForBooking(BookingDto.Mutate booking)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DateTime>> GetCurrentFullyBookedSlots()
    {
        throw new NotImplementedException();
    }

    public async Task<bool> CancelBookingAsync(int bookingId)
    {
        var response = await httpClient.PutAsync($"{endpoint}/cancel/{bookingId}", null);
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }
}
