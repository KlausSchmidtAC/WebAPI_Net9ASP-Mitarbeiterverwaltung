namespace Application;
using Domain;

public interface IMitarbeiterService
{
    Task<IEnumerable<Mitarbeiter>> GetAllMitarbeiter();
    Task<Mitarbeiter?> GetMitarbeiterById(int id);
    Task<IEnumerable<Mitarbeiter>?> SearchMitarbeiter(string search);
    Task<OperationResult> CreateMitarbeiter(Mitarbeiter mitarbeiter);
    Task<OperationResult> UpdateMitarbeiter(int id, Mitarbeiter mitarbeiter);
    Task<OperationResult> DeleteMitarbeiter(int id);
}
