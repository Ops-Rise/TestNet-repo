using System.Net;
using System.Net.Http.Json;
using Rise.Shared.TimeSlots;

namespace Rise.Client.Services;

public class TimeSlotService : ITimeSlotService
{
    private readonly HttpClient _httpClient;
    private const string endpoint = "timeslot";

    public TimeSlotService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task BlockTimeSlotAsync(TimeSlotDto model)
    {
        Console.WriteLine(
            "BlockTimeSlotAsync FOR USER ID : "
                + model.CreatedByUserId
                + " DATE : "
                + model.Date
                + " TIME SLOT : "
                + model.TimeSlot
        );
        var response = await _httpClient.PostAsJsonAsync($"{endpoint}/block", model);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<TimeSlotDto>> GetAllTimeSlotsAsync(
        DateTime startDate,
        DateTime endDate
    )
    {
        var formattedStartDate = startDate.ToString("yyyy-MM-dd");
        var formattedEndDate = endDate.ToString("yyyy-MM-dd");

        var response = await _httpClient.GetAsync(
            $"{endpoint}?startDate={formattedStartDate}&endDate={formattedEndDate}"
        );

        response.EnsureSuccessStatusCode();

        var timeSlots = await response.Content.ReadFromJsonAsync<IEnumerable<TimeSlotDto>>();
        return timeSlots ?? Enumerable.Empty<TimeSlotDto>();
    }

    public async Task<bool> UnblockTimeSlotAsync(DateTime date, int timeSlot)
    {
        var formattedDate = date.ToString("yyyy-MM-dd");
        var response = await _httpClient.DeleteAsync(
            $"{endpoint}/unblock?date={formattedDate}&timeSlot={timeSlot}"
        );

        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }
}
