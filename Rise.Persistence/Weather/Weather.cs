using Rise.Domain.Common;

namespace Rise.Persistence;

public class Weather : Entity
{
    
    public Weather(DateTime date, double temperature, int weatherCode)
    {
        Date = date;
        Temperature = temperature;
        WeatherCode = weatherCode;
    }

    
    public Weather(int id, DateTime date, double temperature, int weatherCode)
        : base(id)
    {
        Date = date;
        Temperature = temperature;
        WeatherCode = weatherCode;
    }

   
    public DateTime Date { get; set; }  
    public double Temperature { get; set; }  
    public int WeatherCode { get; set; } 
}