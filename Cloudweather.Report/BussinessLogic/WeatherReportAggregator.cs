using System.Text.Json;
using Cloudweather.Report.Config;
using Cloudweather.Report.DataAccess;
using Cloudweather.Report.Models;
using Microsoft.Extensions.Options;

namespace Cloudweather.Report.BussinessLogic;

/// <summary>
/// Agregates data from multiple external sources to build a weather report
/// </summary>
public interface IWeatherReportAggregator
{
    /// <summary>
    /// Build and returns a Weekly Weather Report
    /// Persist Weekly Weather Report to the database
    /// </summary>
    /// <param name="zip"></param>
    /// <param name="days"></param>
    /// <returns>Weather report for the city</returns>
    public Task<WeatherReport> BuildReport(string zip, int days);
}

public class WeatherReportAggregator : IWeatherReportAggregator
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<WeatherReportAggregator> _logger;
    private readonly IOptions<WeatherDataConfig> _weatherconfig;
    private readonly WeatherReportDbContext _db;

    public WeatherReportAggregator(IHttpClientFactory http, ILogger<WeatherReportAggregator> logger,
        IOptions<WeatherDataConfig> weatherconfig, WeatherReportDbContext db)
    {
        _http = http;
        _logger = logger;
        _weatherconfig = weatherconfig;
        _db = db;
    }


    public async Task<WeatherReport> BuildReport(string zip, int days)
    {
        var httpClient = _http.CreateClient();
        var precipData = await FetchPrecipitationData(httpClient, zip, days);
        var totalSnow = GetTotalSnow(precipData);
        var totalRain = GetTotalRain(precipData);
        _logger.LogInformation(
            $"zip: {zip} over last {days} days: " +
            $"total snow: {totalSnow} total rain: {totalRain}");

        var tempData = await FetchTemperatureData(httpClient, zip, days);
        var averageTempHigh = tempData.Average(x => x.TempHighF);
        var averageTempLow = tempData.Average(x => x.TempLowF);
        
        _logger.LogInformation(
            $"zip: {zip} over last {days} days: " +
            $"average temp high: {averageTempHigh} average temp low: {averageTempLow}");

        var weatherReport = new WeatherReport
        {
            AverageHighF = Math.Round(averageTempHigh, 2),
            AverageLowF = Math.Round(averageTempLow, 2),
            RainFallTotalInches = totalRain,
            SnowTotalInches = totalSnow,
            ZipCode = zip,
            CreatedOn = DateTime.Now
        };
        
        //TODO: use 'cached' weather reports instead of hitting the database when possible
        _db.Add(weatherReport);
        await _db.SaveChangesAsync();
        
        return weatherReport;

    }

    private static decimal GetTotalSnow(IEnumerable<PrecipitationModel> precipData)
    {
        var totalSnow = precipData.Where(x => x.WeatherType == "snow").Sum(x => x.AmountInches);
        return Math.Round(totalSnow, 1);
    }

    private static decimal GetTotalRain(IEnumerable<PrecipitationModel> precipData)
    {
        var totalRain = precipData.Where(x => x.WeatherType == "rain").Sum(x => x.AmountInches);
        return Math.Round(totalRain, 1);
    }


    private async Task<List<PrecipitationModel>> FetchPrecipitationData(HttpClient httpClient, string zip, int days)
    {
        var endpoint = BuildPrecipitationServiceEndpoint(zip, days);
        var precipRecords = await httpClient.GetAsync(endpoint);
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var precipData = await precipRecords.Content.ReadFromJsonAsync<List<PrecipitationModel>>(jsonSerializerOptions);
        return precipData ?? new List<PrecipitationModel>();
    }

    //Este método es el que se encarga de obtener los datos de temperatura del otro microservicio
    private async Task<List<TemperatureModel>> FetchTemperatureData(HttpClient httpClient, string zip, int days)
    {
        var endpoint = BuildTemperatureServiceEndpoint(zip, days);
        var temperatureRecords = await httpClient.GetAsync(endpoint);
        var temperatureData = await temperatureRecords.Content.ReadFromJsonAsync<List<TemperatureModel>>();

        return temperatureData ?? new List<TemperatureModel>();
    }

    // Este metodo construye la url para el servicio de temperatura
    private string? BuildTemperatureServiceEndpoint(string zip, int days)
    {
        var temperatureServiceProtocol = _weatherconfig.Value.TempDataProtocol;
        var temperatureServiceHost = _weatherconfig.Value.TempDataHost;
        var temperatureServicePort = _weatherconfig.Value.TempDataPort;

        return
            $"{temperatureServiceProtocol}://{temperatureServiceHost}:{temperatureServicePort}/observation/{zip}?days={days}";
    }

    private string? BuildPrecipitationServiceEndpoint(string zip, int days)
    {
        var precipitationServiceProtocol = _weatherconfig.Value.PrecipDataProtocol;
        var precipitationServiceHost = _weatherconfig.Value.PrecipDataHost;
        var precipitationServicePort = _weatherconfig.Value.PrecipDataPort;

        return
            $"{precipitationServiceProtocol}://{precipitationServiceHost}:{precipitationServicePort}/observation/{zip}?days={days}";
    }
}