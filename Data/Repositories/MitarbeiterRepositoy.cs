namespace Data.Repositories;

using System.Linq.Expressions;
using Domain;
using Data.SQL_DB;
using Dapper;

public class MitarbeiterRepository : IMitarbeiterRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public MitarbeiterRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));   
        
    }

    public async Task<OperationResult<IEnumerable<Mitarbeiter>>> GetAll()
    {
        using (var connection = await _connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();

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

            // Option 2: Dapper mit explizitem Column-Mapping über anonyme Objekte
            var rawData = await connection.QueryAsync(
                "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Mitarbeiter"
            );

            var mitarbeiter = rawData.Select(row => new Mitarbeiter(
                (int)row.Id,
                (string)row.FirstName,
                (string)row.LastName,
                ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                (bool)row.IsActive
            ));
            if (mitarbeiter.Count() == 0)
            {
                return OperationResult<IEnumerable<Mitarbeiter>>.FailureResult("Keine Mitarbeiter gefunden.");
            }
            return OperationResult<IEnumerable<Mitarbeiter>>.SuccessResult(mitarbeiter);
        }
    }

    public async Task<OperationResult<Mitarbeiter>> GetById(int id)
    {
        using (var connection = await _connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();

            // Option 2: Dapper mit explizitem Column-Mapping über anonyme Objekte
            var rawData = await connection.QueryFirstOrDefaultAsync(
                "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Mitarbeiter WHERE Id = @id",
                new
                {
                    id = id
                }
            );

            if (rawData == null)
                return OperationResult<Mitarbeiter>.FailureResult($"Mitarbeiter mit der ID = {id} nicht existent.");

            return OperationResult<Mitarbeiter>.SuccessResult(new Mitarbeiter(
                (int)rawData.Id,
                (string)rawData.FirstName,
                (string)rawData.LastName,
                ((DateTime)rawData.Birthdate).ToString("yyyy-MM-dd"),
                (bool)rawData.IsActive
            )); 
        }
    }

    public async Task<OperationResult<IEnumerable<Mitarbeiter>>> Search(string search)
    {
        try
        {
            if (search == "isActive")
            {
                using (var connection = await _connectionFactory.CreateConnection())
                {

                    await connection.OpenAsync();
                    var rawData = await connection.QueryAsync(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Mitarbeiter WHERE IsActive = true"
                    );

                    var mitarbeiter = rawData.Select(row => new Mitarbeiter(
                        (int)row.Id,
                        (string)row.FirstName,
                        (string)row.LastName,
                        ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                        (bool)row.IsActive
                    ));
                    return OperationResult<IEnumerable<Mitarbeiter>>.SuccessResult(mitarbeiter);
                }
            }
            else if (search == "LastName")
            {
                using (var connection = await _connectionFactory.CreateConnection())
                {
                    await connection.OpenAsync();
                    var rawData = await connection.QueryAsync(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Mitarbeiter Order By LastName DESC"
                );

                    var mitarbeiter = rawData.Select(row => new Mitarbeiter(
                        (int)row.Id,
                        (string)row.FirstName,
                        (string)row.LastName,
                        ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                        (bool)row.IsActive
                    ));
                    return OperationResult<IEnumerable<Mitarbeiter>>.SuccessResult(mitarbeiter);
                }
            }
            else if (DateOnly.TryParseExact(search, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateOnly date))
            {
                var birthDate_parsed = date;

                using (var connection = await _connectionFactory.CreateConnection())
                {
                    await connection.OpenAsync();
                    var rawData = await connection.QueryAsync(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Mitarbeiter WHERE Birthdate < @birthDate",
                        new { birthDate = birthDate_parsed.ToString("yyyy-MM-dd") }
                    );

                    var mitarbeiter = rawData.Select(row => new Mitarbeiter(
                        (int)row.Id,
                        (string)row.FirstName,
                        (string)row.LastName,
                        ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                        (bool)row.IsActive
                    ));
                    return OperationResult<IEnumerable<Mitarbeiter>>.SuccessResult(mitarbeiter);
                }
            }
            return OperationResult<IEnumerable<Mitarbeiter>>.FailureResult("Ungültiger Suchfilter. Bitte 'isActive' oder 'LastName' oder ein Datum im Format 'yyyy-MM-dd' verwenden.");
        }
        catch (FormatException)
        {
            return OperationResult<IEnumerable<Mitarbeiter>>.FailureResult("Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben!");
        }
    }

    public async Task<OperationResult> Add(Mitarbeiter? mitarbeiter)
    {
        DateOnly date;

        try
        {
            if (mitarbeiter == null || mitarbeiter == default(Mitarbeiter) || mitarbeiter.FirstName == null || mitarbeiter.LastName == null || mitarbeiter.BirthDate == null)
            {
                return OperationResult.FailureResult("Mitarbeiterdaten sind korrumpiert oder leer.");
            }

            if (string.IsNullOrWhiteSpace(mitarbeiter.FirstName) || string.IsNullOrWhiteSpace(mitarbeiter.LastName))
            {
                return OperationResult.FailureResult("Ein Vorname und ein Nachname sind erforderlich.");
            }
            else if (string.IsNullOrWhiteSpace(mitarbeiter.BirthDate.ToString()))
            {
                return OperationResult.FailureResult("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.");
            }
            else if (
                DateOnly.TryParseExact(
                    mitarbeiter.BirthDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateOnly dateParsed
                ) == false)
            {
                return OperationResult.FailureResult("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.");
            }
            else if (await CheckDuplicateAsync(mitarbeiter))
            {
                return OperationResult.FailureResult("Ein Mitarbeiter mit dem gleichen Vornamen, Nachnamen und Geburtsdatum existiert bereits.");
            }
            else
            {
                date = dateParsed;
            }
        }
        catch (FormatException ex)
        {
            return OperationResult.FailureResult($"Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben! {ex.Message}");
        }
        
        using (var connection = await _connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();
            var Sql_maxID = "SELECT MAX(Id) FROM Mitarbeiter;";
            var maxId = await connection.ExecuteScalarAsync<int>(Sql_maxID);
            int newId = maxId + 1;

            var sql_Add = "INSERT INTO Mitarbeiter (Id,FirstName, LastName, Birthdate, IsActive) VALUES (@Id, @FirstName, @LastName, @Birthdate, @IsActive);";
            await connection.ExecuteAsync(sql_Add, new
            {
                Id = newId,
                FirstName = mitarbeiter.FirstName,
                LastName = mitarbeiter.LastName,
                Birthdate = date.ToString("yyyy-MM-dd"),
                IsActive = mitarbeiter.IsActive
            });
        }

        Console.WriteLine($"Mitarbeiter mit ID {mitarbeiter.id} hinzugefügt.");
        return OperationResult.SuccessResult();
    }

    public async Task<OperationResult> Update(int id, Mitarbeiter? mitarbeiter)
    {
        try
        {
            if (mitarbeiter == null)
            {
                return OperationResult.FailureResult("Mitarbeiterdaten sind korrumpiert oder leer.");
            }

            if (string.IsNullOrWhiteSpace(mitarbeiter.FirstName) || string.IsNullOrWhiteSpace(mitarbeiter.LastName))
            {
                return OperationResult.FailureResult("Ein Vorname und ein Nachname sind erforderlich.");
            }
            else if (string.IsNullOrWhiteSpace(mitarbeiter.BirthDate.ToString()))
            {
                return OperationResult.FailureResult("Ein Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.");
            }
            else if (string.IsNullOrWhiteSpace(mitarbeiter.BirthDate) ||
                DateOnly.TryParseExact(
                    mitarbeiter.BirthDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateOnly dateParsed
                ) == false)
            {
                return OperationResult.FailureResult("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.");
            }
        }
        catch (FormatException ex)
        {
            return OperationResult.FailureResult($"Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben! // {ex.Message}");
        }

        using (var connection = await _connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();

            // 1. Prüfen ob andere Mitarbeiter mit gleichen Daten existieren (außer dem aktuellen)
            var duplicateCheckSql = @"
        SELECT COUNT(*) FROM Mitarbeiter 
        WHERE FirstName = @FirstName 
        AND LastName = @LastName 
        AND Birthdate = @Birthdate 
        AND Id != @Id";

            var duplicateCount = await connection.ExecuteScalarAsync<int>(duplicateCheckSql, new
            {
                FirstName = mitarbeiter.FirstName,
                LastName = mitarbeiter.LastName,
                Birthdate = mitarbeiter.BirthDate,
                Id = id
            });

            if (duplicateCount > 0)
            {
                return OperationResult.FailureResult("Ein anderer Mitarbeiter mit den gleichen Daten existiert bereits unter anderer ID");
            }
        }

        using (var connection = await _connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();
            var sql_Update = "UPDATE Mitarbeiter SET FirstName = @FirstName, LastName = @LastName, Birthdate = @Birthdate, IsActive = @IsActive WHERE Id = @Id;";
            var updateresult = await connection.ExecuteAsync(sql_Update, new
            {
                Id = id,
                FirstName = mitarbeiter.FirstName,
                LastName = mitarbeiter.LastName,
                Birthdate = mitarbeiter.BirthDate,
                IsActive = mitarbeiter.IsActive
            });

            if (updateresult == 0)
            {
                return OperationResult.FailureResult($"Mitarbeiter konnte nicht aktualisiert werden, da diese Id = {id} nicht existiert");
            }
        }
        
        Console.WriteLine($"Mitarbeiter mit ID {id} aktualisiert.");
        return OperationResult.SuccessResult();
    }


    public async Task<OperationResult> Delete(int id)
    {   

        // ToDO: Eventuell alle anderen Ids eins heruntersetzen? 
        using (var connection = await _connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();
            var sql_Delete = "DELETE FROM Mitarbeiter WHERE Id = @Id;";

            var deletedRows = await connection.ExecuteAsync(sql_Delete, new { Id = id });
            
            if (deletedRows > 0)
            {
                Console.WriteLine($"Mitarbeiter mit ID {id} gelöscht.");
                return OperationResult.SuccessResult();
            }
            else
            {
                return OperationResult.FailureResult($"Mitarbeiter konnte nicht gelöscht werden, da diese Id = {id} nicht existiert.");
            }
        }
    }
    
    private async Task<bool> CheckDuplicateAsync(Mitarbeiter mitarbeiter)
{
    var operationResult = await GetAll();
    return operationResult.Data?.Any(m => 
        m.FirstName == mitarbeiter.FirstName && 
        m.LastName == mitarbeiter.LastName && 
        m.BirthDate == mitarbeiter.BirthDate) ?? false;
}
}