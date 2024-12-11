using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rise.Persistence;
using Rise.Shared.Weather;

namespace Rise.Services.Weather
{
    public class WeatherService(
        ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<WeatherService> logger
    ) : IWeatherService
    {
        private const string Latitude = "51.08373737256565";
        private const string Longitude = "3.730739210593181";

        public async Task FetchAndStoreWeatherDataAsync()
        {
            const string url = "https://api.open-meteo.com/";
            const string endpoint =
                $"v1/forecast?latitude={Latitude}&longitude={Longitude}&hourly=temperature_2m,weathercode&forecast_days=7";
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(url);

            var targetTimeSlots = new HashSet<int> { 9, 12, 15 }; // Realistisch gezien halen we deze timeslots op, maar deze zijn hardcoded

            try
            {
                var now = DateTime.UtcNow;
                var relevantDates = targetTimeSlots
                    .Select(slot => now.Date.AddHours(slot))
                    .ToList();

                var existingWeatherData = await dbContext
                    .Weather.Where(w =>
                        relevantDates.Contains(w.Date) && w.UpdatedAt.AddDays(1) >= now
                    )
                    .ToListAsync();

                if (existingWeatherData.Count == relevantDates.Count)
                {
                    logger.LogInformation(
                        "Actuele weerdata is al aanwezig, geen nieuwe gegevens opgehaald."
                    );
                    return;
                }

                var response = await httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<ForecastResponse>(responseBody);

                if (weatherData != null)
                {
                    foreach (var (time, index) in weatherData.Hourly.Time.Select((t, i) => (t, i)))
                    {
                        var timestamp = DateTime.Parse(
                            time,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal
                        );

                        if (!targetTimeSlots.Contains(timestamp.Hour))
                            continue;

                        var temperature = weatherData.Hourly.Temperature_2m[index];
                        var weatherCode = weatherData.Hourly.WeatherCode[index];

                        var existingWeather = existingWeatherData.FirstOrDefault(w =>
                            w.Date == timestamp
                        );

                        if (existingWeather != null)
                        {
                            existingWeather.Temperature = temperature;
                            existingWeather.WeatherCode = weatherCode;
                            existingWeather.UpdatedAt = now;
                        }
                        else
                        {
                            var newWeather = new Persistence.Weather(
                                timestamp,
                                temperature,
                                weatherCode
                            )
                            {
                                UpdatedAt = now,
                            };

                            await dbContext.Weather.AddAsync(newWeather);
                        }
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching weather data.");
            }
        }
    }

    public class HourlyForecast
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; set; }

        [JsonPropertyName("temperature_2m")]
        public List<double> Temperature_2m { get; set; }

        [JsonPropertyName("weathercode")]
        public List<int> WeatherCode { get; set; }
    }

    public class HourlyUnits
    {
        [JsonPropertyName("time")]
        public string Time { get; set; }

        [JsonPropertyName("temperature_2m")]
        public string Temperature_2m { get; set; }

        [JsonPropertyName("weathercode")]
        public string WeatherCode { get; set; }
    }

    public class ForecastResponse
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("elevation")]
        public double Elevation { get; set; }

        [JsonPropertyName("hourly_units")]
        public HourlyUnits HourlyUnits { get; set; }

        [JsonPropertyName("hourly")]
        public HourlyForecast Hourly { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("timezone_abbreviation")]
        public string TimezoneAbbreviation { get; set; }
    }
}
