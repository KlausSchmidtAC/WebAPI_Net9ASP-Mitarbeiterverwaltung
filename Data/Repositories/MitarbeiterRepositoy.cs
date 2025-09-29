namespace Data.Repositories;

using System.Linq.Expressions;
using Domain; 
public class MitarbeiterRepository : IMitarbeiterRepository
{
    private readonly List<Mitarbeiter> _mitarbeiterList;
    private int _nextId = 7;

    public MitarbeiterRepository()
    {
        // Beispielhafte Initialisierung mit einigen Mitarbeitern als Testdaten
        _mitarbeiterList = new List<Mitarbeiter>
        {
            new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true),
            new Mitarbeiter(2, "Erika", "Musterfrau", "1990-06-30", true),
            new Mitarbeiter(3, "John", "Doe", "1978-11-22", false),
            new Mitarbeiter(4, "Hein1", "Blöd", DateOnly.FromDateTime(DateTime.Now.AddDays(-100)).ToString("yyyy-MM-dd"), true),
            new Mitarbeiter(5, "Hein2", "ZBlödB", DateOnly.FromDateTime(DateTime.Now.AddDays(-200)).ToString("yyyy-MM-dd"), true),
            new Mitarbeiter(6, "Hein3", "CBlödC", DateOnly.FromDateTime(DateTime.Now.AddDays(-300)).ToString("yyyy-MM-dd"), false),
            new Mitarbeiter(7, "Hein4", "DBlödD", DateOnly.FromDateTime(DateTime.Now.AddDays(-400)).ToString("yyyy-MM-dd"), true)
        };
        
    }

    public IEnumerable<Mitarbeiter> GetAll()
    {
        return _mitarbeiterList;
    }

    public Mitarbeiter? GetById(int id)
    {   
        return _mitarbeiterList.FirstOrDefault(m => m.id == id);
    }

    public IEnumerable<Mitarbeiter>? Search(string search)
    {
        try {
            if (search == "isActive")
            {
                return _mitarbeiterList.Where(m => m.IsActive);
            }
            else if (search == "LastName")
            {
                return _mitarbeiterList.OrderBy(m => m.LastName).Reverse().ToList(); ;
            }
            else if (DateOnly.TryParseExact(search, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateOnly date))
            {
                var birthDate_parsed = date;
                return _mitarbeiterList.Where(m => DateOnly.Parse(m.BirthDate) < birthDate_parsed);
            }
            return Enumerable.Empty<Mitarbeiter>();
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public bool Add(Mitarbeiter? mitarbeiter, out string? errorMessage)
    {
        
        DateOnly date;

        try {
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
        else if (_mitarbeiterList.Any(m => m.FirstName == mitarbeiter.FirstName && m.LastName == mitarbeiter.LastName && m.BirthDate == mitarbeiter.BirthDate))
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
                errorMessage = $"Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben! // {ex.Message}";
                return false;
            }
        mitarbeiter.id = ++_nextId;
        _mitarbeiterList.Add(mitarbeiter);
        Console.WriteLine($"Mitarbeiter mit ID {mitarbeiter.id} hinzugefügt.");
        errorMessage = null; 
        return true;
    }

    public bool Update(int id, Mitarbeiter? mitarbeiter, out string? errorMessage)
    {   try
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
        var existingMitarbeiter = GetById(id);
            if (existingMitarbeiter == null)
            {
                errorMessage = $"Mitarbeiter mit der ID {id} nicht gefunden. INTERNER FEHLER: NULL REFERENZ GESPEICHERT!!";
                return false;
            }
        errorMessage = null;
        existingMitarbeiter.FirstName = mitarbeiter.FirstName;
        existingMitarbeiter.LastName = mitarbeiter.LastName;
        existingMitarbeiter.BirthDate = mitarbeiter.BirthDate;
        existingMitarbeiter.IsActive = mitarbeiter.IsActive;
        return true; 
    }

    public bool Delete(int id)
    {
        var mitarbeiter = GetById(id);
        if (mitarbeiter == null)
        {
            return false;
        }
        _mitarbeiterList.Remove(mitarbeiter);
        // Alt: mitarbeiter.IsActive = false; // Soft Delete

        Console.WriteLine($"Mitarbeiter mit ID {id} gelöscht.");
        return true;
    }
}