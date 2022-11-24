using System.Net.Http.Json;
using Cloudweather.DataLoader.Models;
using Microsoft.Extensions.Configuration;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

//Accedemos a las distintas partes de la configuración

var servicesConfig = config.GetSection("Services");

var tempServiceConfig = servicesConfig.GetSection("Temperature");
var tempServiceHost = tempServiceConfig["Host"];
var tempServicePort = tempServiceConfig["Port"];

var precipServiceConfig = servicesConfig.GetSection("Precipitation");
var precipServiceHost = precipServiceConfig["Host"];
var precipServicePort = precipServiceConfig["Port"];


var zipCodes = new List<string>
{
    "73026",
    "68104",
    "04401",
    "32808",
    "19717"
};

Console.WriteLine("Starting the Data Load");

var temperatureHttpClient = new HttpClient();
temperatureHttpClient.BaseAddress = new Uri($"http://{tempServiceHost}:{tempServicePort}");

var precipitationHttpClient = new HttpClient();
precipitationHttpClient.BaseAddress = new Uri($"http://{precipServiceHost}:{precipServicePort}");

foreach (var zip in zipCodes)
{
    Console.WriteLine($"Procesing zip code {zip}");
    var from = DateTime.Now.AddYears(-2);
    var thru = DateTime.Now;

    for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
    {
        var temps = PostTemp(zip, day, temperatureHttpClient);
        PostPrecip(temps[0], zip, day, precipitationHttpClient);
    }
}

void PostPrecip(int lowTemp, string zip, DateTime day, HttpClient precipitationHttpClient)
{
    var rand = new Random();
    var isPrecip = rand.Next(2) < 1;

    PrecipitationModel precipitation;

    if (isPrecip)
    {
        var precipInches = rand.Next(1, 16);
        if (lowTemp < 32)
        {
            precipitation = new PrecipitationModel()
            {
                AmountInches = precipInches,
                ZipCode = zip,
                CreatedOn = day,
                WeatherType = "snow"
            };
        }
        else
        {
            precipitation = new PrecipitationModel()
            {
                AmountInches = precipInches,
                ZipCode = zip,
                CreatedOn = day,
                WeatherType = "rain"
            };
        }
    }
    else
    {
        precipitation = new PrecipitationModel()
        {
            AmountInches = 0,
            ZipCode = zip,
            CreatedOn = day,
            WeatherType = "none"
        };
    }

    var precipResponse = precipitationHttpClient.PostAsJsonAsync("observation", precipitation).Result;

    if (precipResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"Posted precipitation: Date: {day:d} " +
                          $"Zip: {zip} " +
                          $"Type: {precipitation.WeatherType} " +
                          "Amount (in.): {precipitation.AmountInches}");
    }
}

List<int> PostTemp(string zip, DateTime day, HttpClient httpClient)
{
    var rand = new Random();
    //we generate a random temperature between 0 and 100
    var t1 = rand.Next(0, 100);
    var t2 = rand.Next(0, 100);

    var hiloTemps = new List<int> { t1, t2 };
    //Sort the list to make sure the first element is the lowest
    hiloTemps.Sort();

    var temperatureObservation = new TemperatureModel()
    {
        ZipCode = zip,
        CreatedOn = day,
        TempLowF = hiloTemps[0],
        TempHighF = hiloTemps[1]
    };

    //Post the temperature to the Temperature service
    var tempResponse = httpClient.PostAsJsonAsync("observation", temperatureObservation).Result;

    if (tempResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"Posted temperature: Date: {day:d} " +
                          $"Zip: {zip} " +
                          $"Low: {temperatureObservation.TempLowF} " +
                          $"High: {temperatureObservation.TempHighF}");
    }
    else
    {
        Console.WriteLine(tempResponse.ToString());
    }

    return hiloTemps;
}