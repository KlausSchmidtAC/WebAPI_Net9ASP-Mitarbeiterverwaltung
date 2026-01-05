namespace Data.Repositories;

using System.Linq.Expressions;
using Domain;
using Data.SQL_DB;
using Dapper;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public EmployeeRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));   
        
    }

    public async Task<OperationResult<IEnumerable<Employee>>> GetAll()
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
            var rawData = await connection.QueryAsync(
                "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Employees"
            );

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

    public async Task<OperationResult<Employee>> GetById(int id)
    {
        using (var connection = await _connectionFactory.CreateConnection())
        {
            // Connection is already opened by SqlConnectionFactory

            
            var rawData = await connection.QueryFirstOrDefaultAsync(
                "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Employees WHERE Id = @id",
                new
                {
                    id = id
                }
            );

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

    public async Task<OperationResult<IEnumerable<Employee>>> Search(string search)
    {
        try
        {
            if (search == "isActive")
            {
                using (var connection = await _connectionFactory.CreateConnection())
                {
                    // Connection is already opened by SqlConnectionFactory
                    var rawData = await connection.QueryAsync(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Employees WHERE IsActive = true"
                    );

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
                    var rawData = await connection.QueryAsync(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Employees Order By LastName DESC"
                );

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
                    var rawData = await connection.QueryAsync(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Employees WHERE Birthdate < @birthDate",
                        new { birthDate = birthDate_parsed.ToString("yyyy-MM-dd") }
                    );

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

    public async Task<OperationResult> Add(Employee? employee)
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
            var Sql_maxID = "SELECT MAX(Id) FROM Employees;";
            var maxId = await connection.ExecuteScalarAsync<int>(Sql_maxID);
            int newId = maxId + 1;

            var sql_Add = "INSERT INTO Employees (Id,FirstName, LastName, Birthdate, IsActive) VALUES (@Id, @FirstName, @LastName, @Birthdate, @IsActive);";
            await connection.ExecuteAsync(sql_Add, new
            {
                Id = newId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Birthdate = date.ToString("yyyy-MM-dd"),
                IsActive = employee.IsActive
            });
        

        // Console.WriteLine($"Mitarbeiter mit ID {newId} hinzugefügt.");
        }
        return OperationResult.SuccessResult();
    }

    public async Task<OperationResult> Update(int id, Employee? employee)
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

            // 1. Pr\u00fcfen ob andere Mitarbeiter mit gleichen Daten existieren (au\u00dfer dem aktuellen)
            var duplicateCheckSql = @"
        SELECT COUNT(*) FROM Employees
        WHERE FirstName = @FirstName 
        AND LastName = @LastName 
        AND Birthdate = @Birthdate 
        AND Id != @Id";

            var duplicateCount = await connection.ExecuteScalarAsync<int>(duplicateCheckSql, new
            {
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Birthdate = employee.BirthDate,
                Id = id
            });

            if (duplicateCount > 0)
            {
                return OperationResult.FailureResult("Another employee with the same data already exists under a different ID");
            }
        }

        using (var connection = await _connectionFactory.CreateConnection())
        {
            // Connection is already opened by SqlConnectionFactory
            var sql_Update = "UPDATE Employees SET FirstName = @FirstName, LastName = @LastName, Birthdate = @Birthdate, IsActive = @IsActive WHERE Id = @Id;";
            var updateresult = await connection.ExecuteAsync(sql_Update, new
            {
                Id = id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Birthdate = employee.BirthDate,
                IsActive = employee.IsActive
            });

            if (updateresult == 0)
            {
                return OperationResult.FailureResult($"Employee could not be updated because ID = {id} does not exist");
            }
        }
        
        Console.WriteLine($"Employee with ID {id} updated.");
        return OperationResult.SuccessResult();
    }


    public async Task<OperationResult> Delete(int id)
    {   

       
        using (var connection = await _connectionFactory.CreateConnection())
        {
            // Connection is already opened by SqlConnectionFactory
            var sql_Delete = "DELETE FROM Employees WHERE Id = @Id;";

            var deletedRows = await connection.ExecuteAsync(sql_Delete, new { Id = id });
            
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
    
    private async Task<bool> CheckDuplicateAsync(Employee employee)
{
    var operationResult = await GetAll();
    return operationResult.Data?.Any(m => 
        m.FirstName == employee.FirstName && 
        m.LastName == employee.LastName && 
        m.BirthDate == employee.BirthDate) ?? false;
}
}