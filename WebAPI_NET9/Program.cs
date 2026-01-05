using WebAPI_NET9;
using WebAPI_NET9.Configuration;
using Application;
using Data.Repositories; 
using Data.SQL_DB;
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

// ✅ EARLY CONFIGURATION VALIDATION - Fail fast on startup errors
using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
var startupLogger = loggerFactory.CreateLogger("Startup");

try
{
    ConfigurationValidator.ValidateConfiguration(builder.Configuration, startupLogger);
}
catch (InvalidOperationException ex)
{
    startupLogger.LogCritical("❌ Application startup aborted due to configuration errors");
    Environment.Exit(1); // Exit with error code
}

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Any, 5100); // HTTP
    serverOptions.Listen(IPAddress.Any, 5101, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});

var jwtConfig = builder.Configuration.GetSection("JWTSettings");
Console.WriteLine("Hello from .NET 9 Web Employee API!");



builder.Logging.ClearProviders();

// OTLP Exporter instead of Console-Logging
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService("WebAPI_NET9_EmployeeService").AddAttributes(new Dictionary<string, object>
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

Console.WriteLine("Hello from OpenTelemetry logging setup!");


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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["SecretKey"] ?? "default-secret-key-for-jwt-tokens")), // Null-safe signature for JWT tokens
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});


/**     Admin Auth without application-side policy. Only via specified claim attribute in controllers

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Domain.Constants.IdentityData.Policies.AdminOnly, policy =>
        policy.RequireClaim(Domain.Constants.IdentityData.Claims.AdminRole, "true")); // alternative: (Domain.Constants.IdentityData.Claims.Role, Domain.Constants.IdentityData.Claims.AdminRole) 
});
**/

builder.Services.AddControllers();
builder.Services.AddOpenApi("WebAPI");
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger, JWT Token Service for Swagger-OPEN API configured as Singleton (only used at startup)
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();


// Configure JSON Serializer Options
builder.Services.ConfigureHttpJsonOptions(options =>    
{
   options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default); 
});

// Register Dependency Injection Services as Singleton (for the entire application)

builder.Services.AddSingleton<IEmployeeService, EmployeeService>();
builder.Services.AddSingleton<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddSingleton<IConnectionFactory, SqlConnectionFactory>();


// Register Database Initializer with configuration values from appsettings.json
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


app.UseAuthentication(); // IMPORTANT: Order matters! Authentication first, then Authorization
app.UseAuthorization();


app.MapControllers();

// List endpoints and their methods
Console.WriteLine("Available Endpoints:");
foreach (var endpoint in app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints)
{
    var routeEndpoint = endpoint as Microsoft.AspNetCore.Routing.RouteEndpoint;
    if (routeEndpoint != null)
    {
        var httpMethods = routeEndpoint.Metadata
            .OfType<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()
            .FirstOrDefault()?.HttpMethods;
        Console.WriteLine($"Route: {routeEndpoint.RoutePattern.RawText}, Methods: {string.Join(",", httpMethods ?? new List<string>())}");
    }
}

app.Run();




