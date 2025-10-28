namespace Data.Repositories;
using Domain;

public interface IMitarbeiterRepository
{
    Task<IEnumerable<Mitarbeiter>> GetAll();
    Task<Mitarbeiter?> GetById(int id);
    Task<IEnumerable<Mitarbeiter>?> Search(string search);
    Task<OperationResult> Add(Mitarbeiter? mitarbeiter);
    Task<OperationResult> Update(int id, Mitarbeiter? mitarbeiter);
    Task<OperationResult> Delete(int id);
}
