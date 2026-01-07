namespace Data.SQL_DB; 
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;

// Factory-Pattern for creating MySQL database connections
public class SqlConnectionFactory : IConnectionFactory
{
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly ILogger<SqlConnectionFactory> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private volatile bool _isInitialized = false;

    public SqlConnectionFactory(IDatabaseInitializer databaseInitializer, ILogger<SqlConnectionFactory> logger)
    {
        _databaseInitializer = databaseInitializer ?? throw new ArgumentNullException(nameof(databaseInitializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GetConnectionString()
    {
        return _databaseInitializer.GetApplicationConnectionString();
    }

    public async Task<MySqlConnection> CreateConnection()
    {
        // Fast path: Skip initialization if already completed successfully
        if (_isInitialized)
        {
            _logger.LogDebug("Database already initialized, creating connection directly");
            
            try 
            {
                // Direct connection attempt - fast path with minimal overhead
                var connection = new MySqlConnection(GetConnectionString());
                await connection.OpenAsync(); // Test connection immediately
                return connection;
            }
            catch (MySqlException ex) when (ex.Number == 1049) // Database doesn't exist anymore although _isInitialized was checked true (DB externally deleted)
            {
                _logger.LogWarning("Database was externally deleted (Error 1049), re-initializing");
                _isInitialized = false; // Reset flag to trigger re-initialization
                // Fall through to initialization logic
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during fast-path connection");
                throw; // Re-throw unexpected errors
            }
        }

        // Initialization required - use semaphore for thread safety
        await _semaphore.WaitAsync();
        try
        {
            // Double-check pattern: Another thread might have completed initialization, if slightly faster than this one 
            if (_isInitialized)
            {
                _logger.LogDebug("Database initialization completed by another thread");
                var connection = new MySqlConnection(_databaseInitializer.GetApplicationConnectionString());
                await connection.OpenAsync();
                return connection;
            }

            // Perform initialization once
            _logger.LogInformation("Starting database initialization process");
            var initialized = await _databaseInitializer.InitializeDatabase();
            
            if (!initialized)
            {
                _logger.LogError("Database initialization failed in SqlConnectionFactory");
                throw new InvalidOperationException("Database initialization failed in SqlConnectionFactory.");
            }
            
            // Mark DB as initialized to skip future checks
            _isInitialized = true;
            _logger.LogInformation("Database initialization completed successfully");
        }
        finally
        {
            _semaphore.Release();
        }
        var connection_first = new MySqlConnection(GetConnectionString());
        await connection_first.OpenAsync();
        return connection_first;
    }
}
