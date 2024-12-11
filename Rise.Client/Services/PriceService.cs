using System;
using System.Net;
using System.Net.Http.Json;
using Rise.Shared.Prices;

namespace Rise.Client.Services
{
    public class PriceService(HttpClient httpClient) : IPriceService
    {
        private readonly HttpClient _httpClient = httpClient;

        private const string endpoint = "price";

        public async Task<IEnumerable<PriceDto.History>?> GetAllPricesAsync()
        {
            var response = await _httpClient.GetAsync(endpoint);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Enumerable.Empty<PriceDto.History>();
            }

            response.EnsureSuccessStatusCode();
            var prices = await response.Content.ReadFromJsonAsync<IEnumerable<PriceDto.History>>();
            return prices;
        }

        public async Task<PriceDto.Index?> GetPriceAsync()
        {
            var response = await _httpClient.GetAsync($"{endpoint}/latest");

            //Not found = error throwen
            response.EnsureSuccessStatusCode();

            var price = await response.Content.ReadFromJsonAsync<PriceDto.Index>();
            return price;
        }

        public async Task<PriceDto.Index?> GetPriceByIdAsync(int priceId)
        {
            var response = await _httpClient.GetAsync($"{endpoint}/{priceId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var price = await response.Content.ReadFromJsonAsync<PriceDto.Index>();
            return price;
        }

        public async Task<int> CreatePriceAsync(PriceDto.Create model)
        {
            var response = await _httpClient.PostAsJsonAsync($"{endpoint}", model);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>();
        }
    }
}
