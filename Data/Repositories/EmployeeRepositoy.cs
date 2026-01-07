namespace Data.Repositories;
using Domain;
using Data.SQL_DB;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmployeeRepository> _logger;
    private readonly int _commandTimeout;

    public EmployeeRepository(IConnectionFactory connectionFactory, IConfiguration configuration, ILogger<EmployeeRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));   
        _configuration = configuration;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commandTimeout = _configuration.GetValue<int>("Database:CommandTimeout", 30);
        if (_commandTimeout <= 0 || _commandTimeout > 300) // Max 6 minutes
    {
        throw new ArgumentOutOfRangeException(nameof(_commandTimeout), 
            $"CommandTimeout {_commandTimeout} ist außerhalb des gültigen Bereichs (1-300 Sekunden)");
    }
    
    _logger.LogInformation("CommandTimeout konfiguriert: {CommandTimeout}s", _commandTimeout);   
    }

    public async Task<OperationResult<IEnumerable<Employee>>> GetAll(CancellationToken cancellationToken = default)
    {
        using (var connection = await _connectionFactory.CreateConnection())
        {
            // Connection is already opened by SqlConnectionFactory

            /** "Langer Weg" mit MySqlCommand und MySqlDataReader. Kurze Syntax aber mit Dapper VIEL besser! 

            using (var command = connection.CreateCommand()) {
                command.CommandText = "SELECT * FROM Mitarbeiter";
                using (var reader = command.ExecuteReader()) {
                    var result = new List<Mitarbeiter>();
                    while (reader.Read()) {
                        var mitarbeiter = new Mitarbeiter(
                            reader.GetInt32("Id"),
                            reader.GetString("FirstName"),
                            reader.GetString("LastName"),
                            reader.GetDateTime("Birthdate").ToString("yyyy-MM-dd"), // ← DateTime → String
                            reader.GetBoolean("IsActive")
                        );
                        result.Add(mitarbeiter);
                    }
                    return result;
                }
            }
            **/

            // Dapper mit explizitem Column-Mapping über anonyme Objekte

    
            var command = new CommandDefinition(
                "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Employees", 
                cancellationToken: cancellationToken, 
                commandTimeout: _commandTimeout);

            var rawData = await connection.QueryAsync(command);

            var employee = rawData.Select(row => new Employee(
                (int)row.Id,
                (string)row.FirstName,
                (string)row.LastName,
                ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                (bool)row.IsActive
            ));
            if (employee.Count() == 0)
            {
                return OperationResult<IEnumerable<Employee>>.FailureResult("No employees found.");
            }
            return OperationResult<IEnumerable<Employee>>.SuccessResult(employee);
        }
    }

    public async Task<OperationResult<Employee>> GetById(int id, CancellationToken cancellationToken = default)
    {
        using (var connection = await _connectionFactory.CreateConnection())
        {
            // Connection is already opened by SqlConnectionFactory
            var command = new CommandDefinition("SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Employees WHERE Id = @id", 
                new
                {
                    id = id
                },
                cancellationToken: cancellationToken,
                commandTimeout: _commandTimeout);
            
            var rawData = await connection.QueryFirstOrDefaultAsync(command); 
             
    
            if (rawData == null)
                return OperationResult<Employee>.FailureResult($"Employee with ID = {id} does not exist.");

            return OperationResult<Employee>.SuccessResult(new Employee(
                (int)rawData.Id,
                (string)rawData.FirstName,
                (string)rawData.LastName,
                ((DateTime)rawData.Birthdate).ToString("yyyy-MM-dd"),
                (bool)rawData.IsActive
            )); 
        }
    }

    public async Task<OperationResult<IEnumerable<Employee>>> Search(string search, CancellationToken cancellationToken = default)
    {
        try
        {
            if (search == "isActive")
            {
                using (var connection = await _connectionFactory.CreateConnection())
                {
                    // Connection is already opened by SqlConnectionFactory

                    var command = new CommandDefinition(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Employees WHERE IsActive = true",
                        cancellationToken: cancellationToken,
                        commandTimeout: _commandTimeout);

                    var rawData = await connection.QueryAsync(command);

                    var employee = rawData.Select(row => new Employee(
                        (int)row.Id,
                        (string)row.FirstName,
                        (string)row.LastName,
                        ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                        (bool)row.IsActive
                    ));
                    return OperationResult<IEnumerable<Employee>>.SuccessResult(employee);
                }
            }
            else if (search == "LastName")
            {
                using (var connection = await _connectionFactory.CreateConnection())
                {
                    // Connection is already opened by SqlConnectionFactory
                    var command = new CommandDefinition("SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Employees Order By LastName DESC",
                        cancellationToken: cancellationToken,
                        commandTimeout: _commandTimeout);
                    var rawData = await connection.QueryAsync(command);

                    var employee = rawData.Select(row => new Employee(
                        (int)row.Id,
                        (string)row.FirstName,
                        (string)row.LastName,
                        ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                        (bool)row.IsActive
                    ));
                    return OperationResult<IEnumerable<Employee>>.SuccessResult(employee);
                }
            }
            else if (DateOnly.TryParseExact(search, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateOnly date))
            {
                var birthDate_parsed = date;

                using (var connection = await _connectionFactory.CreateConnection())
                {
                    // Connection is already opened by SqlConnectionFactory
                    var command = new CommandDefinition(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Employees WHERE Birthdate < @birthDate",
                        new { birthDate = birthDate_parsed.ToString("yyyy-MM-dd") },
                        cancellationToken: cancellationToken,
                        commandTimeout: _commandTimeout);
                    var rawData = await connection.QueryAsync(command);
                    var employee = rawData.Select(row => new Employee(
                        (int)row.Id,
                        (string)row.FirstName,
                        (string)row.LastName,
                        ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                        (bool)row.IsActive
                    ));
                    return OperationResult<IEnumerable<Employee>>.SuccessResult(employee);
                }
            }
            return OperationResult<IEnumerable<Employee>>.FailureResult("Invalid search filter. Please use 'isActive' or 'LastName' or a date in format 'yyyy-MM-dd'.");
        }
        catch (FormatException)
        {
            return OperationResult<IEnumerable<Employee>>.FailureResult("Error processing birth date: invalid characters entered!");
        }
    }

    public async Task<OperationResult> Add(Employee? employee, CancellationToken cancellationToken = default)
    {
        DateOnly date;

        try
        {
            if (employee == null || employee == default(Employee) || employee.FirstName == null || employee.LastName == null || employee.BirthDate == null)
            {
                return OperationResult.FailureResult("Employee data is corrupted or empty.");
            }

            if (string.IsNullOrWhiteSpace(employee.FirstName) || string.IsNullOrWhiteSpace(employee.LastName))
            {
                return OperationResult.FailureResult("A first name and last name are required.");
            }
            else if (string.IsNullOrWhiteSpace(employee.BirthDate.ToString()))
            {
                return OperationResult.FailureResult("A valid birth date in format 'yyyy-MM-dd' is required.");
            }
            else if (
                DateOnly.TryParseExact(
                    employee.BirthDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateOnly dateParsed
                ) == false)
            {
                return OperationResult.FailureResult("A valid birth date in format 'yyyy-MM-dd' is required.");
            }
            else if (await CheckDuplicateAsync(employee))
            {
                return OperationResult.FailureResult("An employee with the same first name, last name and birth date already exists.");
            }
            else
            {
                date = dateParsed;
            }
        }
        catch (FormatException ex)
        {
            return OperationResult.FailureResult($"Error processing birth date: invalid characters entered! {ex.Message}");
        }
        
        using (var connection = await _connectionFactory.CreateConnection())
        {
            // Connection is already opened by SqlConnectionFactory
            var Sql_maxID_command = new CommandDefinition("SELECT MAX(Id) FROM Employees;", cancellationToken: cancellationToken, commandTimeout: _commandTimeout);
            var maxId = await connection.ExecuteScalarAsync<int>(Sql_maxID_command);
            int newId = maxId + 1;

            var sql_Add_command = new CommandDefinition("INSERT INTO Employees (Id,FirstName, LastName, Birthdate, IsActive) VALUES (@Id, @FirstName, @LastName, @Birthdate, @IsActive);",
                new
                {
                Id = newId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Birthdate = date.ToString("yyyy-MM-dd"),
                IsActive = employee.IsActive
                }, 
            cancellationToken: cancellationToken, 
            commandTimeout: _commandTimeout);
            await connection.ExecuteAsync(sql_Add_command);

        // Console.WriteLine($"Mitarbeiter mit ID {newId} hinzugefügt.");
        }
        return OperationResult.SuccessResult();
    }

    public async Task<OperationResult> Update(int id, Employee? employee, CancellationToken cancellationToken = default)
    {
        try
        {
            if (employee == null)
            {
                return OperationResult.FailureResult("Employee data is corrupted or empty.");
            }

            if (string.IsNullOrWhiteSpace(employee.FirstName) || string.IsNullOrWhiteSpace(employee.LastName))
            {
                return OperationResult.FailureResult("A first name and last name are required.");
            }
            else if (string.IsNullOrWhiteSpace(employee.BirthDate.ToString()))
            {
                return OperationResult.FailureResult("A birth date in format 'yyyy-MM-dd' is required.");
            }
            else if (string.IsNullOrWhiteSpace(employee.BirthDate) ||
                DateOnly.TryParseExact(
                    employee.BirthDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateOnly dateParsed
                ) == false)
            {
                return OperationResult.FailureResult("A valid birth date in format 'yyyy-MM-dd' is required.");
            }
        }
        catch (FormatException ex)
        {
            return OperationResult.FailureResult($"Error processing birth date: invalid characters entered! // {ex.Message}");
        }

        using (var connection = await _connectionFactory.CreateConnection())
        {
            // Connection is already opened by SqlConnectionFactory

            // 1. Check if employee with given ID exists
            var duplicateCheckSql = new CommandDefinition(@"
        SELECT COUNT(*) FROM Employees
        WHERE FirstName = @FirstName 
        AND LastName = @LastName 
        AND Birthdate = @Birthdate 
        AND Id != @Id", new
            {
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Birthdate = employee.BirthDate,
                Id = id
            }, 
            cancellationToken: cancellationToken, commandTimeout: _commandTimeout);

            var duplicateCount = await connection.ExecuteScalarAsync<int>(duplicateCheckSql);

            if (duplicateCount > 0)
            {
                return OperationResult.FailureResult("Another employee with the same data already exists under a different ID");
            }
        }

        using (var connection = await _connectionFactory.CreateConnection())
        {
            // Connection is already opened by SqlConnectionFactory
            var sql_Update = new CommandDefinition("UPDATE Employees SET FirstName = @FirstName, LastName = @LastName, Birthdate = @Birthdate, IsActive = @IsActive WHERE Id = @Id;",  
            new
            {
                Id = id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Birthdate = employee.BirthDate,
                IsActive = employee.IsActive
            },
            cancellationToken: cancellationToken, commandTimeout: _commandTimeout);
            var updateresult = await connection.ExecuteAsync(sql_Update);

            if (updateresult == 0)
            {
                return OperationResult.FailureResult($"Employee could not be updated because ID = {id} does not exist");
            }
        }
        
        Console.WriteLine($"Employee with ID {id} updated.");
        return OperationResult.SuccessResult();
    }


    public async Task<OperationResult> Delete(int id, CancellationToken cancellationToken = default)
    {   

       
        using (var connection = await _connectionFactory.CreateConnection())
        {
            // Connection is already opened by SqlConnectionFactory
            var sql_Delete = new CommandDefinition("DELETE FROM Employees WHERE Id = @Id;", new { Id = id }, cancellationToken: cancellationToken, commandTimeout: _commandTimeout);

            var deletedRows = await connection.ExecuteAsync(sql_Delete);
            
            if (deletedRows > 0)
            {
                Console.WriteLine($"Employee with ID {id} deleted.");
                return OperationResult.SuccessResult();
            }
            else
            {
                return OperationResult.FailureResult($"Employee could not be deleted because ID = {id} does not exist.");
            }
        }
    }
    
    private async Task<bool> CheckDuplicateAsync(Employee employee, CancellationToken cancellationToken = default)
{
    var operationResult = await GetAll(cancellationToken);
    return operationResult.Data?.Any(m => 
        m.FirstName == employee.FirstName && 
        m.LastName == employee.LastName && 
        m.BirthDate == employee.BirthDate) ?? false;
}
}