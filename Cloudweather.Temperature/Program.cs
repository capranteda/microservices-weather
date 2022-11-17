using Cloudweather.Temperature.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TemperatureDbContext>(options =>
{
    // EnableSensitiveDataLogging allows you to see the SQL generated by EF Core
    options.EnableSensitiveDataLogging();
    // EnableDetailedErrors allows you to see the actual error message from the database
    options.EnableDetailedErrors();
    // UseNpgsql is the EF Core provider for PostgreSQL
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDb"));
}, ServiceLifetime.Transient);
var app = builder.Build();
app.Run();