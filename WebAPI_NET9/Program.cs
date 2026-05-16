using WebAPI_NET9;
using WebAPI_NET9.Configuration;
using WebAPI_NET9.HealthChecks;
using WebAPI_NET9.Models;
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
using OpenTelemetry.Resources;


var builder = WebApplication.CreateBuilder(args);

// ✅ EARLY CONFIGURATION VALIDATION - Fail fast on startup errors
using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
var startupLogger = loggerFactory.CreateLogger("Startup");

try
{
    ConfigurationValidator.ValidateConfiguration(builder.Configuration, startupLogger);
}
catch (InvalidOperationException)
{
    // Configuration validation failed - application will exit
    startupLogger.LogCritical("❌ Application startup aborted due to configuration errors");
    Environment.Exit(1); // Exit with error code
}

// Kestrel Server Configuration for multiple endpoints (HTTP + HTTPS)
//  Configuaration via appsettings.{...}.json and environment variables
/** Development: HTTP + HTTPS
    Production: HTTPS only 

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    if (builder.Environment.IsDevelopment())
    {
        serverOptions.Listen(IPAddress.Any, 5100); // HTTP - Development
        serverOptions.Listen(IPAddress.Any, 5101, listenOptions =>
        {
            listenOptions.UseHttps(); // HTTPS - Development  
        });
    }
    else
    {
        // Production: HTTPS only
        serverOptions.Listen(IPAddress.Any, 443, listenOptions =>
        {
            listenOptions.UseHttps(); // Production HTTPS on standard port
        });
    }
});
**/

// Kestrel Limits für Hochlast
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;  // WebSocket-Upgrades eingeschlossen
    options.Limits.MaxRequestBodySize = 1024 * 1024; // 1 MB
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
    options.Limits.Http2.MaxStreamsPerConnection = 100; // HTTP/2 parallele Streams
});

// ThreadPool Minimum für viele parallele DB-Requests
ThreadPool.SetMinThreads(100, 100);

var jwtConfig = builder.Configuration.GetSection("JWTSettings");
Console.WriteLine("Hello from .NET 9 Web Employee API!");


builder.Logging.ClearProviders();

// In Docker: Console logging so exceptions are visible in container logs
// In Development: OTLP to Seq (localhost:5099)

// OTLP Exporter instead of Console-Logging
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateEmpty()
    .AddService("WebAPI_NET9_EmployeeService")
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = builder.Environment.EnvironmentName,
        ["service.version"] = "1.0.0",
        ["service.name"] = "WebAPI_NET9_EmployeeService",
        ["service.instance.id"] = Environment.MachineName // Example of custom attribute - could be Git commit hash or build number in real scenarios
    }));

    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;

    options.AddOtlpExporter(
    exporter =>
    {
        exporter.Endpoint = new Uri(builder.Configuration["Seq:OtlpEndpoint"]!);
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
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // HTTPS only in Production

    var secretKey = (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Docker")
       ? jwtConfig["SecretKey"]                                        // Development/Docker: from appsettings.{ENV}.json
       : Environment.GetEnvironmentVariable("JWT_SECRET_KEY");         // Production: from environment variable

    if (string.IsNullOrEmpty(secretKey))
    {
        throw new InvalidOperationException($"JWT SecretKey is required. Environment: {builder.Environment.EnvironmentName}");
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwtConfig["Issuer"],
        ValidAudience = jwtConfig["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), // Null-safe signature for JWT tokens
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});


/**     Admin Auth without application-side policy. Only specified via claim attribute in controllers

builder.Services.AddAuthorization(options =>cd
{
    options.AddPolicy(Domain.Constants.IdentityData.Policies.AdminOnly, policy =>
        policy.RequireClaim(Domain.Constants.IdentityData.Claims.AdminRole, "true")); // alternative: (Domain.Constants.IdentityData.Claims.Role, Domain.Constants.IdentityData.Claims.AdminRole) 
});
**/

builder.Services.AddControllers();

// Environment-based CORS configuration
var corsOrigins = (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Docker") ?
    new[] { // HTTP Development Origins
        "http://localhost:8080",    // Vue dev server
        "http://127.0.0.1:8080",
        "http://localhost:3000",
        "http://localhost:5173",    // Vite dev server
        // HTTPS Development Origins
        "https://localhost:8080",
        "https://127.0.0.1:8080",
        "https://localhost:3000",
        "https://localhost:5173"}
    : new[] { "https://yourdomain.com", "https://www.yourdomain.com" };  // HTTPS in Production!

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebPolicy", policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()              // GET, POST, PATCH, DELETE
              .AllowAnyHeader()              // Content-Type, Authorization, etc.
              .AllowCredentials());          // for JWT Authentication
});

// OpenAPI / Swagger Configuration
// builder.Services.AddOpenApi("WebAPI"); // Alternative: AddSwaggerGen() + AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>() for more control and customization of Swagger-UI and OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger, JWT Token Service for Swagger-OPEN API configured as Singleton (only used at startup)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Employee API",
        Description = "A simple ASP.NET Core Web API for managing employees with JWT Authentication and OpenAPI documentation.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "K. Schmidt",
            Email = "klaus.schmidt1@rwth-aachen.de"
        }
    });
    c.EnableAnnotations();
});


builder.Services.AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();


// Configure JSON Serializer Options: Use Source-Generated Context for better performance and AOT compatibility, especially in Blazor WebAssembly or Native AOT scenarios. This allows for pre-compilation of JSON serialization metadata, improving runtime performance and reducing memory usage.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// Register Dependency Injection Services as Singleton (for the entire application)

builder.Services.AddSingleton<IEmployeeService, EmployeeService>();
builder.Services.AddSingleton<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddSingleton<IConnectionFactory, SqlConnectionFactory>();

// Register Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "sql" })
    .AddCheck<ApplicationHealthCheck>("application", tags: new[] { "app", "system" });

// Register Database Initializer with configuration values from appsettings.json
var dbConfig = builder.Configuration.GetSection("Database");
builder.Services.AddSingleton<IDatabaseInitializer>(provider =>
    new SqlServerDatabaseInitializer(
        provider.GetRequiredService<ILogger<SqlServerDatabaseInitializer>>(),
        dbConfig["ServerIP"] ?? "localhost",
        dbConfig["DatabaseName"] ?? "employees",
        dbConfig["Port"] ?? "3306",
        dbConfig["Username"] ?? "root",
        dbConfig["Password"] ?? ""
    )
);

var app = builder.Build();


// Middleware Area

// CORS Middleware - Always before Authentication/Authorization!
app.UseCors("WebPolicy");


if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi(); // Alternative: app.UseSwagger() + app.UseSwaggerUI() for more control and customization of Swagger-UI and OpenAPI documentation
    app.UseSwagger();
    app.UseSwaggerUI();
}
else if (!builder.Environment.IsEnvironment("Docker"))
{
    // Production: HTTPS Enforcement
    app.UseHttpsRedirection();
    // app.UseHsts();  // HTTP Strict Transport Security
}


// In Production: 
// app.UseHttpsRedirection();


app.UseAuthentication(); // IMPORTANT: Order matters! Authentication first, then Authorization
app.UseAuthorization();    // RequiresClaimAttribute.OnAuthorizationAsync is called during this step

// Health Checks Endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var response = new HealthCheckResponse(
            Status: report.Status.ToString(),
            TotalDuration: report.TotalDuration.TotalMilliseconds,
            Checks: report.Entries.Select(entry => new HealthCheckEntry(
                Name: entry.Key,
                Status: entry.Value.Status.ToString(),
                Duration: entry.Value.Duration.TotalMilliseconds,
                Description: entry.Value.Description,
                Data: entry.Value.Data,
                Exception: entry.Value.Exception?.Message,
                Tags: entry.Value.Tags
            )).ToArray()
        );

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(response, AppJsonSerializerContext.Default.HealthCheckResponse)
        );
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("app")
});

app.MapControllers();

app.Run();




