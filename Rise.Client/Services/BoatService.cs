using System.Net;
using System.Net.Http.Json;
using Rise.Shared.Boats;

namespace Rise.Client.Services;

public class BoatService(HttpClient httpClient) : IBoatService
{
    private readonly HttpClient _httpClient = httpClient;
    private const string endpoint = "boat";

    public async Task<IEnumerable<BoatDto.BoatIndex>?> GetAllBoatsAsync()
    {
        var response = await _httpClient.GetAsync("boat");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Enumerable.Empty<BoatDto.BoatIndex>();
        }

        response.EnsureSuccessStatusCode();
        var boats = await response.Content.ReadFromJsonAsync<IEnumerable<BoatDto.BoatIndex>>();
        return boats;
    }

    public async Task<BoatDto.BoatIndex?> GetBoatByIdAsync(int boatId)
    {
        var response = await _httpClient.GetAsync($"{endpoint}/{boatId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var boat = await response.Content.ReadFromJsonAsync<BoatDto.BoatIndex>();
        return boat;
    }

    public async Task<int> GetAvailableBoatsCountAsync()
    {
        var response = await _httpClient.GetAsync($"{endpoint}/available/count");

        response.EnsureSuccessStatusCode();

        var count = await response.Content.ReadFromJsonAsync<int>();
        return count;
    }

    public async Task<BoatDto.BoatIndex> CreateNewBoatAsync(BoatDto.CreateBoatDto createDto)
    {
        var response = await _httpClient.PostAsJsonAsync($"{endpoint}", createDto);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(response.Content.ReadAsStringAsync().Result);
        }
        
        response.EnsureSuccessStatusCode();

        var boat =
            await response.Content.ReadFromJsonAsync<BoatDto.BoatIndex>()
            ?? throw new InvalidOperationException("Failed to deserialize the response content.");
        return boat;
    }

    public async Task<bool> UpdateBoatStatusAsync(
        int boatId,
        BoatDto.Mutate model
    )
    {
        var response = await _httpClient.PutAsJsonAsync($"{endpoint}/{boatId}", model);
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteBoatAsync(int boatId)
    {
        var response = await _httpClient.DeleteAsync($"{endpoint}/{boatId}");
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }
}
