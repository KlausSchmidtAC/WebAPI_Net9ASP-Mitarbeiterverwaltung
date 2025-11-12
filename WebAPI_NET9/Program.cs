using WebAPI_NET9;
using Domain;
using Application;
using Data.Repositories; 
using Data.SQL_DB;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;



var builder = WebApplication.CreateBuilder(args);

/**
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .WriteTo.Console()
        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
        .ReadFrom.Configuration(context.Configuration));
**/

var jwtConfig = builder.Configuration.GetSection("JWTSettings");
Console.WriteLine("Hello from .NET 9 Web Mitarbeiter-API!");

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options => options.AddConsoleExporter());

/**
builder.Logging.AddOpenTelemetry(options => options.AddOtlpExporter(
a => 
{
    a.Endpoint = new Uri("http://localhost:5100/ingest/otlp/v1/logs");
    a.Protocol = OtlpExportProtocol.HttpProtobuf;
    a.Headers.Add("Authorization", "Bearer " + jwtConfig["SecretKey"]);
}
));
**/


builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwtConfig["Issuer"],
        ValidAudience = jwtConfig["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["SecretKey"] ?? "default-secret-key-for-jwt-tokens")), // Null-safe Signatur für JWT-Token
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Domain.Constants.IdentityData.Policies.AdminOnly, policy =>
        policy.RequireClaim(Domain.Constants.IdentityData.Claims.AdminRole, "true")); // alt.:  (Domain.Constants.IdentityData.Claims.Role, Domain.Constants.IdentityData.Claims.AdminRole) 
});


builder.Services.AddControllers();
builder.Services.AddOpenApi("WebAPI");
builder.Services.AddEndpointsApiExplorer();

//Swagger konfigurieren, JWT Token Service für Swagger-OPEN API konfigurieren als Singleton (wird nur beim Start verwendet)
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();


// JSon Serializer Optionen konfigurieren
builder.Services.ConfigureHttpJsonOptions(options =>    
{
   options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default); 
});

// Dependency Injection Services als Singleton (für die ganze Anwendung) registrieren

builder.Services.AddSingleton<IMitarbeiterService, MitarbeiterService>();
builder.Services.AddSingleton<IMitarbeiterRepository, MitarbeiterRepository>();
builder.Services.AddSingleton<IConnectionFactory, SqlConnectionFactory>();


// Datenbank Initializer mit Konfigurationswerten aus appsettings.json registrieren
var dbConfig = builder.Configuration.GetSection("Database");
builder.Services.AddSingleton<IDatabaseInitializer>(provider =>
    new SqlServerDatabaseInitializer(
        dbConfig["ServerIP"] ?? "localhost",
        dbConfig["DatabaseName"] ?? "Mitarbeiter", 
        dbConfig["Port"] ?? "3306",
        dbConfig["Username"] ?? "root",
        dbConfig["Password"] ?? ""
    )
);

var app = builder.Build();


// Middleware Area

if (app.Environment.IsDevelopment())
{   
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();


app.UseAuthentication(); //ACHTUNG: Reihenfolge wichtig! Erst Authentifizierung, dann Autorisierung
app.UseAuthorization();


app.MapControllers();

// Endpoints und ihre Methoden auflisten
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




