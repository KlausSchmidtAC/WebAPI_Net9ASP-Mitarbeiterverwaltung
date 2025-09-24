namespace Data.Repositories;
using Domain; 
public class MitarbeiterRepository : IMitarbeiterRepository
{
    private readonly List<Mitarbeiter> _mitarbeiterList;
    

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

    public Mitarbeiter GetById(int id)
    {
        return _mitarbeiterList.FirstOrDefault(m => m.id == id);
    }

    public IEnumerable<Mitarbeiter> Search(string search)
    {
        return _mitarbeiterList.Where(m => m.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           m.LastName.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    public void Add(Mitarbeiter mitarbeiter)
    {
        _mitarbeiterList.Add(mitarbeiter);
        Console.WriteLine($"Mitarbeiter mit ID {mitarbeiter.id} hinzugefügt.");
    }

    public void Update(Mitarbeiter mitarbeiter)
    {
        var existingMitarbeiter = GetById(mitarbeiter.id);
            existingMitarbeiter.FirstName = mitarbeiter.FirstName;
            existingMitarbeiter.LastName = mitarbeiter.LastName;
            existingMitarbeiter.BirthDate = mitarbeiter.BirthDate;
            existingMitarbeiter.IsActive = mitarbeiter.IsActive;
    }

    public void Delete(int id)
    {
        var mitarbeiter = GetById(id);
        if (mitarbeiter != null)
        {
            _mitarbeiterList.Remove(mitarbeiter);
        }
    }
}