using System;
using System.Net;
using System.Net.Http.Json;
using Rise.Shared.Batteries;
using Rise.Domain.Batteries;

namespace Rise.Client.Services
{
    public class BatteryService(HttpClient httpClient) : IBatteryService
    {
        private readonly HttpClient _httpClient = httpClient;

        private const string endpoint = "battery";

        public async Task<IEnumerable<BatteryDto.BatteryIndex>?> GetAllBatteriesAsync()
        {
            var response = await _httpClient.GetAsync($"{endpoint}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<BatteryDto.BatteryIndex>>();
        }

        public async Task<BatteryDto.BatteryIndex?> GetBatteryByIdAsync(int batteryId)
        {
            var response = await _httpClient.GetAsync($"{endpoint}/{batteryId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<BatteryDto.BatteryIndex>();
        }

        public async Task<IEnumerable<BatteryDto.BatteryDetail>?> GetAllBatteriesWithDetailsAsync()
        {
            var response = await _httpClient.GetAsync($"{endpoint}/details");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<
                IEnumerable<BatteryDto.BatteryDetail>
            >();
        }

        public async Task<BatteryDto.BatteryDetail?> GetBatteryWithDetailsByIdAsync(int batteryId)
        {
            var response = await _httpClient.GetAsync($"{endpoint}/details/{batteryId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<BatteryDto.BatteryDetail>();
        }

        public async Task<int> CreateBatteryAsync(BatteryDto.Create model)
        {
            var response = await _httpClient.PostAsJsonAsync($"{endpoint}", model);
            
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new ArgumentException(response.Content.ReadAsStringAsync().Result);
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>();
        }

        public async Task<bool> UpdateBatteryAsync(int batteryId, BatteryDto.Mutate model)
        {
            var response = await _httpClient.PutAsJsonAsync($"{endpoint}/{batteryId}", model);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new ArgumentException(response.Content.ReadAsStringAsync().Result);
            }

            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<BatteryDto.BatteryIndex>?> GetBatteriesByStatusAsync(
            BatteryStatus status
        )
        {
            var response = await _httpClient.GetAsync($"{endpoint}/status/{status}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<BatteryDto.BatteryIndex>>();
        }

        public async Task<bool> DeleteBatteryAsync(int batteryId)
        {
            var response = await _httpClient.DeleteAsync($"{endpoint}/{batteryId}");
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
    }
}
