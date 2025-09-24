namespace Data.Repositories;
using Domain;

public interface IMitarbeiterRepository
{
    IEnumerable<Mitarbeiter> GetAll();
    Mitarbeiter GetById(int id);
    IEnumerable<Mitarbeiter> Search(string search);
    void Add(Mitarbeiter mitarbeiter);
    void Update(Mitarbeiter mitarbeiter);
    void Delete(int id);
}
