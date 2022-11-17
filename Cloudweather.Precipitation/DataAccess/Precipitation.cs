namespace Cloudweather.Precipitation.DataAccess;

public class Precipitation
{
    public Guid Id { get; set; }
    public DateTime CreatedOn { get; set; }
    public decimal AmountInches { get; set; }
    public string WeatherTipe { get; set; }
    public string ZipCode { get; set; }
}