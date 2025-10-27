namespace Data.SQL_DB; 
using MySql.Data.MySqlClient;

// Factory for creating MySQL database connections
public class SqlConnectionFactory : IConnectionFactory
{
    private readonly IDatabaseInitializer _databaseInitializer;

    public SqlConnectionFactory(IDatabaseInitializer databaseInitializer)
    {
        _databaseInitializer = databaseInitializer ?? throw new ArgumentNullException(nameof(databaseInitializer));

        if (!_databaseInitializer.InitializeDatabase())
        {
            Console.WriteLine("Database initialization failed in SqlConnectionFactory.");
            throw new InvalidOperationException("Database initialization failed in SqlConnectionFactory.");
        }
    }

    public string GetConnectionString()
    {
        return _databaseInitializer.GetApplicationConnectionString();
    }

    public MySqlConnection CreateConnection()
    {
        return new MySqlConnection(_databaseInitializer.GetApplicationConnectionString());
    }
}
