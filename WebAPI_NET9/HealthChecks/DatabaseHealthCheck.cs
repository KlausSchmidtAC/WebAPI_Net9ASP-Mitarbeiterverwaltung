using Microsoft.Extensions.Diagnostics.HealthChecks;
using Data.SQL_DB;

namespace WebAPI_NET9.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IConnectionFactory connectionFactory, ILogger<DatabaseHealthCheck> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            using var connection = await _connectionFactory.CreateConnection();
            
            // Simple connectivity test with timeout
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5; // 5 second timeout
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var responseTime = DateTime.UtcNow - startTime;
            
            if (result?.ToString() == "1")
            {
                var data = new Dictionary<string, object>
                {
                    ["server"] = GetServerInfo(connection.ConnectionString),
                    ["database"] = GetDatabaseName(connection.ConnectionString),
                    ["responseTime"] = $"{responseTime.TotalMilliseconds:F2}ms",
                    ["status"] = "Connected"
                };

                // Performance thresholds
                if (responseTime.TotalMilliseconds > 1000)
                {
                    _logger.LogWarning("Database response time is slow: {ResponseTime}ms", responseTime.TotalMilliseconds);
                    return HealthCheckResult.Degraded("Database responding slowly", data: data);
                }

                return HealthCheckResult.Healthy("Database connection successful", data: data);
            }
            
            return HealthCheckResult.Unhealthy("Database query returned unexpected result");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Database health check timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy($"Database connection failed: {ex.Message}", ex);
        }
    }

    private static string GetServerInfo(string connectionString)
    {
        try
        {
            var builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(connectionString);
            return $"{builder.Server}:{builder.Port}";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetDatabaseName(string connectionString)
    {
        try
        {
            var builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(connectionString);
            return builder.Database ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}