using WebAPI_NET9;
using Application;
using Data.Repositories; 
using Serilog;
using Microsoft.AspNetCore.Routing; // Für EndpointDataSource, RouteEndpoint
using Microsoft.AspNetCore.Http; // Für HttpMethodMetadata
using System.Linq; // Für OfType(), FirstOrDefault()




var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .WriteTo.Console()
        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
        .ReadFrom.Configuration(context.Configuration));

/** AUTHENTIFIZIERUNG, HEALTHCHECKS, STANDARD SERVICES, CUSTOM SERVICES
builder.AddStandardServices();
builder.AddAuthServices(); // JWT Bearer Authentifizierung
builder.AddHealthCheckServices();
builder.AddCustomServices();

**/


Console.WriteLine("Hello from .NET 9 Web API!");


builder.Services.AddControllers();

builder.Services.AddOpenApi("WebAPI");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.ConfigureHttpJsonOptions(options =>
{
   options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default); 
});

// Dependency Injection Services als Scoped (für jeden einzelnen Request) registrieren 
builder.Services.AddSingleton<IMitarbeiterService, MitarbeiterService>();
builder.Services.AddSingleton<IMitarbeiterRepository, MitarbeiterRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); 
    app.UseSwagger();
    app.UseSwaggerUI();
    Console.WriteLine("Swagger enabled in Development environment.");
}

app.MapControllers();
Console.WriteLine("Available Endpoints:");


foreach (var endpoint in app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints)
{
    var routeEndpoint = endpoint as Microsoft.AspNetCore.Routing.RouteEndpoint;
    if (routeEndpoint != null)
    {
        var httpMethods = routeEndpoint.Metadata
            .OfType<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()
            .FirstOrDefault()?.HttpMethods;
        Console.WriteLine($"Route: {routeEndpoint.RoutePattern.RawText}, Methoden: {string.Join(",", httpMethods ?? new List<string>())}");
    }
}


app.Run();




