namespace Application;
using Domain; // nicht nötig?? 
public interface IMitarbeiterService
{
    IEnumerable<Mitarbeiter> GetAllMitarbeiter();
    Mitarbeiter GetMitarbeiterById(int id);
    IEnumerable<Mitarbeiter> SearchMitarbeiter(string search);
    void CreateMitarbeiter(Mitarbeiter mitarbeiter);
    void UpdateMitarbeiter(Mitarbeiter mitarbeiter);
    void DeleteMitarbeiter(int id);
}
