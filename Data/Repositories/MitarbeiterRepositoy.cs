namespace Data.Repositories;

using System.Linq.Expressions;
using Domain;
using Data.SQL_DB;
using Dapper;

public class MitarbeiterRepository : IMitarbeiterRepository
{
    private readonly List<Mitarbeiter> _mitarbeiterList;

    private int _nextId = 7;
    private readonly IConnectionFactory _connectionFactory;

    public MitarbeiterRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));   

    }

    public IEnumerable<Mitarbeiter> GetAll()
    {
        using (var connection = _connectionFactory.CreateConnection())
        {
            connection.Open();

            /** "Langer Weg" mit MySqlCommand und MySqlDataReader. Kurze Syntax aber mit Dapper! 

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
            var rawData = connection.Query(
                "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Mitarbeiter"
            );

            var mitarbeiter = rawData.Select(row => new Mitarbeiter(
                (int)row.Id,
                (string)row.FirstName,
                (string)row.LastName,
                ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                (bool)row.IsActive
            )).ToList();

            return mitarbeiter;
        }
    }

    public Mitarbeiter? GetById(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        // Option 2: Dapper mit explizitem Column-Mapping über anonyme Objekte
        var rawData = connection.QueryFirstOrDefault(
            "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Mitarbeiter WHERE Id = @id",
            new { id = id }
        );

        if (rawData == null)
            return null;

        return new Mitarbeiter(
            (int)rawData.Id,
            (string)rawData.FirstName,
            (string)rawData.LastName,
            ((DateTime)rawData.Birthdate).ToString("yyyy-MM-dd"),
            (bool)rawData.IsActive
        );
    }

    public IEnumerable<Mitarbeiter>? Search(string search)
    {
        try
        {
            if (search == "isActive")
            {
                using (var connection = _connectionFactory.CreateConnection())
                {

                    connection.Open();
                    var rawData = connection.Query(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Mitarbeiter WHERE IsActive = true"
                    );

                    var mitarbeiter = rawData.Select(row => new Mitarbeiter(
                        (int)row.Id,
                        (string)row.FirstName,
                        (string)row.LastName,
                        ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                        (bool)row.IsActive
                    )).ToList();
                    return mitarbeiter;
                }
            }
            else if (search == "LastName")
            {
                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    var rawData = connection.Query(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Mitarbeiter Order By LastName DESC"
                );

                    var mitarbeiter = rawData.Select(row => new Mitarbeiter(
                        (int)row.Id,
                        (string)row.FirstName,
                        (string)row.LastName,
                        ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                        (bool)row.IsActive
                    )).ToList();
                    return mitarbeiter;
                }
            }
            else if (DateOnly.TryParseExact(search, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateOnly date))
            {
                var birthDate_parsed = date;

                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    var rawData = connection.Query(
                        "SELECT Id, FirstName, LastName, Birthdate, IsActive FROM Mitarbeiter WHERE Birthdate < @birthDate",
                        new { birthDate = birthDate_parsed.ToString("yyyy-MM-dd") }
                    );

                    var mitarbeiter = rawData.Select(row => new Mitarbeiter(
                        (int)row.Id,
                        (string)row.FirstName,
                        (string)row.LastName,
                        ((DateTime)row.Birthdate).ToString("yyyy-MM-dd"),
                        (bool)row.IsActive
                    )).ToList();
                    return mitarbeiter;
                }
            }
            Console.WriteLine("Fehler beim Verarbeiten des Suchfilters!");
            return null;
        }
        catch (FormatException)
        {
            Console.WriteLine("Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben!");
            return null;
        }
    }

    public bool Add(Mitarbeiter? mitarbeiter, out string? errorMessage)
    {

        DateOnly date;

        try
        {
            if (mitarbeiter == null)
            {
                errorMessage = "Mitarbeiterdaten sind korrumpiert oder leer.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(mitarbeiter.FirstName) || string.IsNullOrWhiteSpace(mitarbeiter.LastName))
            {
                errorMessage = "Ein Vorname und ein Nachname sind erforderlich.";
                return false;
            }
            else if (string.IsNullOrWhiteSpace(mitarbeiter.BirthDate.ToString()))
            {
                errorMessage = "Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.";
                return false;
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
                errorMessage = "Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.";
                return false;
            }
            else if (GetAll().Any(m => m.FirstName == mitarbeiter.FirstName && m.LastName == mitarbeiter.LastName && m.BirthDate == mitarbeiter.BirthDate))
            {
                errorMessage = "Ein Mitarbeiter mit dem gleichen Vornamen, Nachnamen und Geburtsdatum existiert bereits.";
                return false;
            }
            else
            {
                date = dateParsed;
            }
        }
        catch (FormatException ex)
        {
            errorMessage = $"Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben! {ex.Message}";
            return false;
        }
        using (var connection = _connectionFactory.CreateConnection())
        {
            connection.Open();
            var Sql_maxID = "SELECT MAX(Id) FROM Mitarbeiter;";
            var maxId = connection.ExecuteScalar<int>(Sql_maxID);
            int newId = maxId + 1;

            var sql_Add = "INSERT INTO Mitarbeiter (Id,FirstName, LastName, Birthdate, IsActive) VALUES (@Id, @FirstName, @LastName, @Birthdate, @IsActive);";
            connection.Execute(sql_Add, new
            {
                Id = newId,
                FirstName = mitarbeiter.FirstName,
                LastName = mitarbeiter.LastName,
                Birthdate = date.ToString("yyyy-MM-dd"),
                IsActive = mitarbeiter.IsActive
            });

        }

        Console.WriteLine($"Mitarbeiter mit ID {mitarbeiter.id} hinzugefügt.");

        errorMessage = null;
        return true;
    }

    public bool Update(int id, Mitarbeiter? mitarbeiter, out string? errorMessage)
    {
        try
        {
            if (mitarbeiter == null)
            {
                errorMessage = "Mitarbeiterdaten sind korrumpiert oder leer.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(mitarbeiter.FirstName) || string.IsNullOrWhiteSpace(mitarbeiter.LastName))
            {
                errorMessage = "Ein Vorname und ein Nachname sind erforderlich.";
                return false;
            }
            else if (string.IsNullOrWhiteSpace(mitarbeiter.BirthDate.ToString()))
            {
                errorMessage = "Ein Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.";
                return false;
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
                errorMessage = "Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.";
                return false;
            }
        }
        catch (FormatException ex)
        {
            errorMessage = $"Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben! // {ex.Message}";
            return false;
        }

        using (var connection = _connectionFactory.CreateConnection())
        {
            connection.Open();

            // 1. Prüfen ob andere Mitarbeiter mit gleichen Daten existieren (außer dem aktuellen)
            var duplicateCheckSql = @"
        SELECT COUNT(*) FROM Mitarbeiter 
        WHERE FirstName = @FirstName 
        AND LastName = @LastName 
        AND Birthdate = @Birthdate 
        AND Id != @Id";

            var duplicateCount = connection.ExecuteScalar<int>(duplicateCheckSql, new
            {
                FirstName = mitarbeiter.FirstName,
                LastName = mitarbeiter.LastName,
                Birthdate = mitarbeiter.BirthDate,
                Id = id
            });

            if (duplicateCount > 0)
            {
                errorMessage = "Ein anderer Mitarbeiter mit den gleichen Daten existiert bereits unter anderer ID";
                return false;
            }
        }

        using (var connection = _connectionFactory.CreateConnection())
        {
            connection.Open();
            var sql_Update = "UPDATE Mitarbeiter SET FirstName = @FirstName, LastName = @LastName, Birthdate = @Birthdate, IsActive = @IsActive WHERE Id = @Id;";
            var updateresult = connection.Execute(sql_Update, new
            {
                Id = id,
                FirstName = mitarbeiter.FirstName,
                LastName = mitarbeiter.LastName,
                Birthdate = mitarbeiter.BirthDate,
                IsActive = mitarbeiter.IsActive
            });

            errorMessage = updateresult > 0 ? null : $"Mitarbeiter konnte nicht aktualisiert werden, da diese Id = {id} nicht existiert";
            if (updateresult == 0)
            {
                return false;
            }

        }
        Console.WriteLine($"Mitarbeiter mit ID {id} aktualisiert.");
        return true;
    }


    public bool Delete(int id, out string? errorMessage)
    {
        using (var connection = _connectionFactory.CreateConnection())
        {
            connection.Open();
            var sql_Delete = "DELETE FROM Mitarbeiter WHERE Id = @Id;";


            if (connection.Execute(sql_Delete, new { Id = id }) > 0)
            {
                Console.WriteLine($"Mitarbeiter mit ID {id} gelöscht.");
                errorMessage = null;
                return true;
            }
        }
        errorMessage = $"Mitarbeiter konnte nicht gelöscht werden, da diese Id = {id} nicht existiert.";
        return false;
    }
}