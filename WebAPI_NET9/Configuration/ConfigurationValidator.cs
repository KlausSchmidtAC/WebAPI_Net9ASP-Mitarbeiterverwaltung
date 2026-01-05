using System.ComponentModel.DataAnnotations;
using System.Net;

namespace WebAPI_NET9.Configuration;

public class ConfigurationValidator
{
    public static void ValidateConfiguration(IConfiguration configuration, ILogger logger)
    {
        logger.LogInformation("Starting application configuration validation...");

        var errors = new List<string>();

        // Database Configuration Validation
        ValidateDatabaseConfiguration(configuration, errors);

        // JWT Configuration Validation  
        ValidateJwtConfiguration(configuration, errors);

        // Kestrel Configuration Validation
        ValidateKestrelConfiguration(configuration, errors);

        // OpenTelemetry Configuration Validation (Optional)
        ValidateOpenTelemetryConfiguration(configuration, errors);

        if (errors.Count > 0)
        {
            var errorMessage = $"Configuration validation failed with {errors.Count} error(s):\n" +
                             string.Join("\n", errors.Select(e => $"- {e}"));
            logger.LogCritical(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        logger.LogInformation("✅ Configuration validation completed successfully");
    }

    private static void ValidateDatabaseConfiguration(IConfiguration configuration, List<string> errors)
    {
        var dbSection = configuration.GetSection("Database");

        // Server IP validation
        var serverIP = dbSection["ServerIP"];
        if (string.IsNullOrWhiteSpace(serverIP))
        {
            errors.Add("Database:ServerIP is required");
        }
        else if (!IsValidIPOrHostname(serverIP))
        {
            errors.Add($"Database:ServerIP '{serverIP}' is not a valid IP address or hostname");
        }

        // Port validation
        var portString = dbSection["Port"];
        if (string.IsNullOrWhiteSpace(portString))
        {
            errors.Add("Database:Port is required");
        }
        else if (!int.TryParse(portString, out var port) || port < 1 || port > 65535)
        {
            errors.Add($"Database:Port '{portString}' must be a valid port number (1-65535)");
        }

        // Database name validation
        var databaseName = dbSection["DatabaseName"];
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            errors.Add("Database:DatabaseName is required");
        }
        else if (databaseName.Length > 64 || !IsValidDatabaseName(databaseName))
        {
            errors.Add($"Database:DatabaseName '{databaseName}' is invalid (max 64 chars, alphanumeric + underscore)");
        }

        // Username validation
        var username = dbSection["Username"];
        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add("Database:Username is required");
        }

        // Password validation (can be empty for localhost development)
        var password = dbSection["Password"];
        if (serverIP != "localhost" && serverIP != "127.0.0.1" && string.IsNullOrEmpty(password))
        {
            errors.Add("Database:Password is required for non-localhost connections");
        }
    }

    private static void ValidateJwtConfiguration(IConfiguration configuration, List<string> errors)
    {
        var jwtSection = configuration.GetSection("JWTSettings");

        // Issuer validation
        var issuer = jwtSection["Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            errors.Add("JWTSettings:Issuer is required");
        }
        else if (!Uri.TryCreate(issuer, UriKind.Absolute, out _))
        {
            errors.Add($"JWTSettings:Issuer '{issuer}' must be a valid URI");
        }

        // Audience validation
        var audience = jwtSection["Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            errors.Add("JWTSettings:Audience is required");
        }
        else if (!Uri.TryCreate(audience, UriKind.Absolute, out _))
        {
            errors.Add($"JWTSettings:Audience '{audience}' must be a valid URI");
        }

        // Secret key validation
        var secretKey = jwtSection["SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            errors.Add("JWTSettings:SecretKey is required");
        }
        else if (secretKey.Length < 32)
        {
            errors.Add("JWTSettings:SecretKey must be at least 32 characters long for security");
        }
        else if (secretKey.Contains("HaHaHa") || secretKey == "your-secret-key")
        {
            errors.Add("JWTSettings:SecretKey appears to be a default/example value - use a secure random key");
        }
    }

    private static void ValidateKestrelConfiguration(IConfiguration configuration, List<string> errors)
    {
        var kestrelSection = configuration.GetSection("Kestrel:Endpoints");

        Uri? httpUri = null;
        Uri? httpsUri = null;

        // HTTP endpoint validation
        var httpUrl = kestrelSection["Http:Url"];
        if (!string.IsNullOrWhiteSpace(httpUrl))
        {
            if (!Uri.TryCreate(httpUrl, UriKind.Absolute, out httpUri))
            {
                errors.Add($"Kestrel:Endpoints:Http:Url '{httpUrl}' is not a valid URI");
            }
        }

        // HTTPS endpoint validation
        var httpsUrl = kestrelSection["Https:Url"];
        if (!string.IsNullOrWhiteSpace(httpsUrl))
        {
            if (!Uri.TryCreate(httpsUrl, UriKind.Absolute, out httpsUri))
            {
                errors.Add($"Kestrel:Endpoints:Https:Url '{httpsUrl}' is not a valid URI");
            }
        }

        // Warn if both endpoints use same port (only if both URIs are valid)
        if (httpUri != null && httpsUri != null && httpUri.Port == httpsUri.Port)
        {
            errors.Add($"HTTP and HTTPS endpoints cannot use the same port {httpUri.Port}");
        }
    }

    private static void ValidateOpenTelemetryConfiguration(IConfiguration configuration, List<string> errors)
    {
        // This is optional - only validate if OpenTelemetry section exists
        var otlpSection = configuration.GetSection("OpenTelemetry");
        if (!otlpSection.Exists()) return;

        var endpoint = otlpSection["Endpoint"];
        if (!string.IsNullOrWhiteSpace(endpoint) && !Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            errors.Add($"OpenTelemetry:Endpoint '{endpoint}' is not a valid URI");
        }
    }

    private static bool IsValidIPOrHostname(string value)
    {
        // Check if it's a valid IP address
        if (IPAddress.TryParse(value, out _))
            return true;

        // Check if it's a valid hostname (basic validation)
        if (value == "localhost")
            return true;

        // Basic hostname validation - alphanumeric + dots + hyphens
        return value.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-') &&
               !value.StartsWith('.') && !value.EndsWith('.') &&
               value.Length <= 253;
    }

    private static bool IsValidDatabaseName(string name)
    {
        // MySQL database name rules: alphanumeric + underscore, cannot start with number
        return name.All(c => char.IsLetterOrDigit(c) || c == '_') &&
               !char.IsDigit(name[0]);
    }
}