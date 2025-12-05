namespace Data.SQL_DB;
using System;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;


// For MySQL Database Initialization and Connection String Management
public class SqlServerDatabaseInitializer : IDatabaseInitializer
{
    private readonly ILogger<SqlServerDatabaseInitializer> _logger;
    private readonly string serverIP;
    private readonly string databaseName;
    private readonly string port;
    private readonly string username;
    private readonly string password;

    // Bootstrap ConnectionString (ohne spezifische Datenbank)
    private string BootstrapConnectionString =>
        $"Server={serverIP};Port={port};Uid={username};Pwd={password};";

    private string ApplicationConnectionString =>
        $"Server={serverIP};Port={port};Uid={username};Pwd={password};Database={databaseName};";

    public SqlServerDatabaseInitializer(ILogger<SqlServerDatabaseInitializer> logger, 
                                      string serverIP = "localhost", string databaseName = "Mitarbeiter",
                                      string port = "3306", string username = "root", string password = "")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.serverIP = serverIP;
        this.databaseName = databaseName;
        this.port = port;
        this.username = username;
        this.password = password;
    }
    public string GetApplicationConnectionString() => ApplicationConnectionString;

    public async Task<bool> InitializeDatabase()
    {
        _logger.LogInformation("Starting database initialization for database '{DatabaseName}'", databaseName);
        try
        {
            using (var connection = await Task.FromResult(new MySqlConnection(BootstrapConnectionString)))
            {
                await connection.OpenAsync();
                _logger.LogDebug("Bootstrap connection established successfully");

                if (!await CheckIfDatabaseExists(connection))
                {
                    _logger.LogInformation("Database '{DatabaseName}' does not exist, creating it", databaseName);
                    await CreateDatabase(connection);
                }
                else
                {
                    _logger.LogInformation("Database '{DatabaseName}' already exists", databaseName);
                }

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database '{DatabaseName}'", databaseName);
            return false;
        }
        
        _logger.LogInformation("Database initialization completed successfully for '{DatabaseName}'", databaseName);
        return true;
    }

    public async Task<bool> CreateDatabase(MySqlConnection bootstrapConnection)
    {
        _logger.LogInformation("Creating database '{DatabaseName}' and required tables", databaseName);
        try
        {
            var sql_string = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`;";
            using (var command = new MySqlCommand(sql_string, bootstrapConnection))
            {
                await Task.FromResult(command.ExecuteNonQuery());
            }
            _logger.LogDebug("Database '{DatabaseName}' created successfully", databaseName);

            var useDbString = $"USE `{databaseName}`;";
            using (var command = new MySqlCommand(useDbString, bootstrapConnection))
            {
                await Task.FromResult(command.ExecuteNonQuery())    ;
            }
            _logger.LogDebug("Switched to database '{DatabaseName}'", databaseName);

            var createTablesString = @"
                CREATE TABLE IF NOT EXISTS Mitarbeiter (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    FirstName VARCHAR(100) NOT NULL,
                    LastName VARCHAR(100) NOT NULL,
                    Birthdate DATE NOT NULL,
                    IsActive BOOLEAN NOT NULL
                );";

            using (var command = new MySqlCommand(createTablesString, bootstrapConnection))
            {
                await Task.FromResult(command.ExecuteNonQuery());
            }
            _logger.LogInformation("MySQL Server Database '{DatabaseName}' and tables initialized successfully", databaseName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database '{DatabaseName}'", databaseName);
            return false;
        }
    }
    public async Task<bool> CheckIfDatabaseExists(MySqlConnection bootstrapConnection)
    {
        _logger.LogDebug("Checking if database '{DatabaseName}' exists", databaseName);

        // ToDO: Alternative Lösung wäre über SQL-Procedure zu prüfen, ob die Datenbank existiert.
        // IF DB_ID(@db) IS NULL
        //          BEGIN
        //              DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@db) + N';';
        //              EXEC (@sql);
        //          END

        try
        {
            var sql_checkstring = "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @databaseName";
            using (var command = new MySqlCommand(sql_checkstring, bootstrapConnection))
            {
                command.Parameters.AddWithValue("@databaseName", databaseName);
                var result = await Task.FromResult(command.ExecuteScalar());
                bool exists = result != null;
                _logger.LogDebug("Database '{DatabaseName}' existence check result: {Exists}", databaseName, exists);
                return exists; 
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database existence for '{DatabaseName}'", databaseName);
            return false; 
        }
    }

}

