namespace Application;
using Domain;

public interface IMitarbeiterService
{
    Task<OperationResult<IEnumerable<Mitarbeiter>>> GetAllMitarbeiter();
    Task<OperationResult<Mitarbeiter>> GetMitarbeiterById(int id);
    Task<OperationResult<IEnumerable<Mitarbeiter>>> SearchMitarbeiter(string search);
    Task<OperationResult> CreateMitarbeiter(Mitarbeiter mitarbeiter);
    Task<OperationResult> UpdateMitarbeiter(int id, Mitarbeiter mitarbeiter);
    Task<OperationResult> DeleteMitarbeiter(int id);
}
