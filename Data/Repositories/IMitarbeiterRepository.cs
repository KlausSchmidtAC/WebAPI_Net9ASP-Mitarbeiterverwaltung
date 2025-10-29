namespace Data.Repositories;
using Domain;

public interface IMitarbeiterRepository
{
    Task<OperationResult<IEnumerable<Mitarbeiter>>> GetAll();
    Task<OperationResult<Mitarbeiter>> GetById(int id);
    Task<OperationResult<IEnumerable<Mitarbeiter>>> Search(string search);
    Task<OperationResult> Add(Mitarbeiter? mitarbeiter);
    Task<OperationResult> Update(int id, Mitarbeiter? mitarbeiter);
    Task<OperationResult> Delete(int id);
}
