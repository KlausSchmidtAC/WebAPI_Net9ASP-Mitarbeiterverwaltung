namespace Data.SQL_DB;
using System;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySql.Data.MySqlClient;


// For MySQL Database Initialization and Connection String Management
public class SqlServerDatabaseInitializer : IDatabaseInitializer
{

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

    public SqlServerDatabaseInitializer(string serverIP = "localhost", string databaseName = "Mitarbeiter",
                                      string port = "3306", string username = "root", string password = "")
    {
        this.serverIP = serverIP;
        this.databaseName = databaseName;
        this.port = port;
        this.username = username;
        this.password = password;
    }
    public string GetApplicationConnectionString() => ApplicationConnectionString;

    public async Task<bool> InitializeDatabase()
    {
        try
        {
            using (var connection = await Task.FromResult(new MySqlConnection(BootstrapConnectionString)))
            {
                await connection.OpenAsync();

                if (!await CheckIfDatabaseExists(connection))
                {
                    await CreateDatabase(connection);
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error initializing database: " + ex.Message);
            return false;
        }
        Console.WriteLine($"MySQL Server Database '{databaseName}' initialized successfully.");
        return true;
    }

    public async Task<bool> CreateDatabase(MySqlConnection bootstrapConnection)
    {
        try
        {
            var sql_string = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`;";
            using (var command = new MySqlCommand(sql_string, bootstrapConnection))
            {
                await Task.FromResult(command.ExecuteNonQuery());
            }

            var useDbString = $"USE `{databaseName}`;";
            using (var command = new MySqlCommand(useDbString, bootstrapConnection))
            {
                await Task.FromResult(command.ExecuteNonQuery())    ;
            }

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

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating database: {ex.Message}");
            return false;
        }
    }
    public async Task<bool> CheckIfDatabaseExists(MySqlConnection bootstrapConnection)
    {

        // ToDO: Alternative wäre über SQL-Anfragen zu prüfen, ob die Datenbank existiert.
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
                return result != null; // Einfacher Return
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking database existence: {ex.Message}");
            return false; // Bei Fehler annehmen, dass DB nicht existiert
        }
    }

}

