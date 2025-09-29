namespace Application;
using Domain; // nicht nötig?? 
public interface IMitarbeiterService
{
    IEnumerable<Mitarbeiter> GetAllMitarbeiter();
    Mitarbeiter? GetMitarbeiterById(int id);
    IEnumerable<Mitarbeiter>? SearchMitarbeiter(string search);
    bool CreateMitarbeiter(Mitarbeiter mitarbeiter, out string? errorMessage);
    bool UpdateMitarbeiter(int id, Mitarbeiter mitarbeiter, out string? errorMessage);
    bool DeleteMitarbeiter(int id);
}
