namespace Data.SQL_DB; 
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;

// Factory-Pattern for creating MySQL database connections
public class SqlConnectionFactory : IConnectionFactory
{
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly ILogger<SqlConnectionFactory> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private Task<bool>? _initializationTask;

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
        // semaphore stellt sicher: Initialisierung wird nur einmal ausgeführt
        await _semaphore.WaitAsync();
        try
        {
            if (_initializationTask == null)
            {
                _initializationTask = _databaseInitializer.InitializeDatabase();
            }
        }
        finally
        {
            _semaphore.Release();
        }

        var initialized = await _initializationTask;
        if (!initialized)
        {
            _logger.LogError("Database initialization failed in SqlConnectionFactory");
            throw new InvalidOperationException("Database initialization failed in SqlConnectionFactory.");
        }
        
        _logger.LogDebug("Creating new MySQL connection");
        return new MySqlConnection(_databaseInitializer.GetApplicationConnectionString());
    }
}
