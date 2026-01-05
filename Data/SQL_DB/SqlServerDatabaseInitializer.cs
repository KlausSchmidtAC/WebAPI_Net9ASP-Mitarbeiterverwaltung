namespace Data.SQL_DB;
using System;
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

    // Bootstrap ConnectionString (without specific database)
    private string BootstrapConnectionString =>
        $"Server={serverIP};Port={port};Uid={username};Pwd={password};";

    private string ApplicationConnectionString =>
        $"Server={serverIP};Port={port};Uid={username};Pwd={password};Database={databaseName};";

    public SqlServerDatabaseInitializer(ILogger<SqlServerDatabaseInitializer> logger, 
                                      string serverIP = "localhost", string databaseName = "Employees",
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
    public string GetBootstrapConnectionString() => BootstrapConnectionString;

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
        catch (MySqlException ex)
        {
            return HandleMySqlException(ex, "initializing database");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout occurred while initializing database '{DatabaseName}'", databaseName);
            return false;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Connection"))
        {
            _logger.LogError(ex, "Connection state error while initializing database '{DatabaseName}'", databaseName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error initializing database '{DatabaseName}'", databaseName);
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
                CREATE TABLE IF NOT EXISTS employees (
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
        catch (MySqlException ex)
        {
            return HandleMySqlException(ex, "creating database and tables");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating database '{DatabaseName}'", databaseName);
            return false;
        }
    }
    public async Task<bool> CheckIfDatabaseExists(MySqlConnection bootstrapConnection)
    {
        _logger.LogDebug("Checking if database '{DatabaseName}' exists", databaseName);

        // Alternative Solution to check if the database exists using a SQL procedure.
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
        catch (MySqlException ex)
        {
            _logger.LogWarning(ex, "MySQL error checking database existence for '{DatabaseName}' - Error {ErrorNumber}: {ErrorMessage}", 
                databaseName, ex.Number, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking database existence for '{DatabaseName}'", databaseName);
            return false; 
        }
    }

    private bool HandleMySqlException(MySqlException ex, string operation)
    {
        switch (ex.Number)
        {
            case 1044: // Access denied for user to database
                _logger.LogError(ex, "❌ Access denied while {Operation} for database '{DatabaseName}' - Check user permissions", 
                    operation, databaseName);
                break;
                
            case 1045: // Access denied for user (using password)
                _logger.LogError(ex, "❌ Authentication failed while {Operation} - Invalid username/password for user '{Username}'", 
                    operation, username);
                break;
                
            case 1049: // Unknown database
                _logger.LogWarning(ex, "⚠️ Database '{DatabaseName}' does not exist while {Operation} - This may be expected during initialization", 
                    databaseName, operation);
                break;
                
            case 2002: // Can't connect to local MySQL server  
                _logger.LogError(ex, "❌ Cannot connect to MySQL server at {ServerIP}:{Port} while {Operation} - Check if MySQL is running", 
                    serverIP, port, operation);
                break;
                
            case 2003: // Can't connect to MySQL server on port
                _logger.LogError(ex, "❌ Cannot connect to MySQL server on port {Port} while {Operation} - Check firewall/port availability", 
                    port, operation);
                break;
                
            case 1142: // Command denied to user for table
                _logger.LogError(ex, "❌ Insufficient privileges while {Operation} - User '{Username}' lacks required permissions", 
                    operation, username);
                break;
                
            case 1050: // Table already exists
                _logger.LogWarning(ex, "⚠️ Table already exists while {Operation} - This may be expected", operation);
                return true; // Not necessarily an error
                
            case 1007: // Database already exists
                _logger.LogInformation("ℹ️ Database '{DatabaseName}' already exists while {Operation} - Continuing", 
                    databaseName, operation);
                return true; // Not an error
                
            case 1226: // User has exceeded max_user_connections
                _logger.LogError(ex, "❌ Too many connections while {Operation} - Connection limit reached for user '{Username}'", 
                    operation, username);
                break;
                
            case 1040: // Too many connections
                _logger.LogError(ex, "❌ MySQL server has too many connections while {Operation} - Server overloaded", operation);
                break;
                
            default:
                _logger.LogError(ex, "❌ MySQL error {ErrorNumber} while {Operation}: {ErrorMessage}", 
                    ex.Number, operation, ex.Message);
                break;
        }
        
        return false; // Most errors should fail the operation
    }

}

