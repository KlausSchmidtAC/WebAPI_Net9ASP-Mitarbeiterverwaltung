using WebAPI_NET9;
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
using OpenTelemetry.Exporter;
using System.Net;
using OpenTelemetry.Resources;


var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Any, 5100); // HTTP
    serverOptions.Listen(IPAddress.Any, 5101, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});

var jwtConfig = builder.Configuration.GetSection("JWTSettings");
Console.WriteLine("Hello from .NET 9 Web Mitarbeiter-API!");



builder.Logging.ClearProviders();

//OTLP Exporter anstatt Console-Logging
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService("WebAPI_NET9_MitarbeiterService").AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = "development",
        ["service.version"] = "1.0.0"
    }));

    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;

    options.AddOtlpExporter(
    exporter =>
    {
        exporter.Endpoint = new Uri("http://localhost:5099/ingest/otlp/v1/logs");
        exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
        exporter.Headers = "";
    });
}); 

Console.WriteLine("Hello from openTelemetry logging setup!");


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


/**     Admin Auth Ohne Application-seitige-Policy. Nur per spezifiziertes Claim-Attribut in den Controllern

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Domain.Constants.IdentityData.Policies.AdminOnly, policy =>
        policy.RequireClaim(Domain.Constants.IdentityData.Claims.AdminRole, "true")); // alt.:  (Domain.Constants.IdentityData.Claims.Role, Domain.Constants.IdentityData.Claims.AdminRole) 
});
**/

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
        provider.GetRequiredService<ILogger<SqlServerDatabaseInitializer>>(),
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

// In Production: 
// app.UseHttpsRedirection();


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




