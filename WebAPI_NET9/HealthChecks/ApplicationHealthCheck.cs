using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebAPI_NET9.HealthChecks;

public class ApplicationHealthCheck : IHealthCheck
{
    private readonly ILogger<ApplicationHealthCheck> _logger;
    private readonly IConfiguration _configuration;
    private static readonly DateTime _startTime = DateTime.UtcNow;
    
    private readonly int _memoryThresholdMB;
    private readonly int _gcCollectionThreshold;
    private readonly int _timeoutSeconds;

    public ApplicationHealthCheck(ILogger<ApplicationHealthCheck> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        // Load configurable values from appsettings.json
        _memoryThresholdMB = _configuration.GetValue<int>("HealthCheckSettings:MemoryThresholdMB", 500);
        _gcCollectionThreshold = _configuration.GetValue<int>("HealthCheckSettings:GCCollectionThreshold", 100);
        _timeoutSeconds = _configuration.GetValue<int>("HealthCheckSettings:TimeoutSeconds", 5);
        
        _logger.LogInformation("HealthCheck configured with MemoryThreshold: {MemoryThreshold}MB, GCThreshold: {GCThreshold}, Timeout: {Timeout}s", 
            _memoryThresholdMB, _gcCollectionThreshold, _timeoutSeconds);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        try
        {
            var uptime = DateTime.UtcNow - _startTime;
            var memoryUsage = GC.GetTotalMemory(false);
            var generation0Collections = GC.CollectionCount(0);
            var generation1Collections = GC.CollectionCount(1);
            var generation2Collections = GC.CollectionCount(2);

            var data = new Dictionary<string, object>
            {
                ["uptime"] = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s",
                ["memoryUsage"] = $"{memoryUsage / 1024 / 1024:F2} MB",
                ["gcGen0Collections"] = generation0Collections,
                ["gcGen1Collections"] = generation1Collections,
                ["gcGen2Collections"] = generation2Collections,
                ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                ["dotnetVersion"] = Environment.Version.ToString(),
                ["processorCount"] = Environment.ProcessorCount,
                ["workingSet"] = $"{Environment.WorkingSet / 1024 / 1024:F2} MB"
            };

            // Memory threshold check
            var memoryMB = memoryUsage / 1024.0 / 1024.0;
            if (memoryMB > _memoryThresholdMB)
            {
                _logger.LogWarning("High memory usage detected: {MemoryUsage}MB (threshold: {Threshold}MB)", memoryMB, _memoryThresholdMB);
                return HealthCheckResult.Degraded("High memory usage", data: data);
            }

            // GC pressure check
            if (generation2Collections > _gcCollectionThreshold)
            {
                _logger.LogWarning("High GC pressure detected. Gen2 collections: {Gen2Collections} (threshold: {Threshold})", generation2Collections, _gcCollectionThreshold);
                return HealthCheckResult.Degraded("High GC pressure", data: data);
            }

            return HealthCheckResult.Healthy("Application is running normally", data: data);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Application health check timed out after {Timeout} seconds", _timeoutSeconds);
            return HealthCheckResult.Unhealthy($"Health check timed out after {_timeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application health check failed");
            return HealthCheckResult.Unhealthy($"Application health check failed: {ex.Message}", ex);
        }
    }
}