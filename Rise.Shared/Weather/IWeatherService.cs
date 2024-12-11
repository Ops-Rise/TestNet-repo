namespace Rise.Shared.Weather;

/// <summary>
/// Interface voor de service die verantwoordelijk is voor het ophalen en opslaan van weersgegevens.
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Haalt weersgegevens op van een externe API (bijvoorbeeld Open-Meteo) en slaat deze op in de database.
    /// </summary>
    /// <param name="forecastDays">Het aantal dagen waarvoor de weersvoorspelling wordt opgehaald (maximaal 7 dagen).</param>
    /// <returns>Een taak die asynchroon de weersgegevens ophaalt en opslaat.</returns>
    Task FetchAndStoreWeatherDataAsync();
}