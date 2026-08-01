using System.Text.Json.Serialization;
using Tripwalaah.LocationService.Api.Endpoints;
using Tripwalaah.LocationService.Application;
using Tripwalaah.LocationService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    service = "Tripwalaah.LocationService",
    version = "1.0.0",
    status = "running"
}))
.WithName("GetServiceInfo")
.ExcludeFromDescription();

app.MapLocationEndpoints();

app.Run();

public partial class Program;
