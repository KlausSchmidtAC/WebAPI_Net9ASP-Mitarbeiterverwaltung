namespace Data.Repositories;
using Domain;

public interface IMitarbeiterRepository
{
    IEnumerable<Mitarbeiter> GetAll();
    Mitarbeiter? GetById(int id);
    IEnumerable<Mitarbeiter>? Search(string search);
    bool Add(Mitarbeiter? mitarbeiter, out string? errorMessage);
    bool Update(int id, Mitarbeiter? mitarbeiter, out string? errorMessage);
    bool Delete(int id);
}
