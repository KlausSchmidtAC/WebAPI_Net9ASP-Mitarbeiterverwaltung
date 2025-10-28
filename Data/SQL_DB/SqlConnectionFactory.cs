namespace Data.SQL_DB; 
using MySql.Data.MySqlClient;

// Factory for creating MySQL database connections
public class SqlConnectionFactory : IConnectionFactory
{
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private Task<bool>? _initializationTask;

    public SqlConnectionFactory(IDatabaseInitializer databaseInitializer)
    {
        _databaseInitializer = databaseInitializer ?? throw new ArgumentNullException(nameof(databaseInitializer));
    }

    public string GetConnectionString()
    {
        return _databaseInitializer.GetApplicationConnectionString();
    }

    public async Task<MySqlConnection> CreateConnection()
    {
        // Ensure initialization happens only once
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
            Console.WriteLine("Database initialization failed in SqlConnectionFactory.");
            throw new InvalidOperationException("Database initialization failed in SqlConnectionFactory.");
        }
        return new MySqlConnection(_databaseInitializer.GetApplicationConnectionString());
    }
}
