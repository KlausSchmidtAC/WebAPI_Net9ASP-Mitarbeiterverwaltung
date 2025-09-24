namespace Application;
using Domain;
using Data.Repositories;


public class MitarbeiterService : IMitarbeiterService
{
    private readonly IMitarbeiterRepository _mitarbeiterRepository;

    public MitarbeiterService(IMitarbeiterRepository mitarbeiterRepository)
    {
        _mitarbeiterRepository = mitarbeiterRepository;
    }

    public IEnumerable<Mitarbeiter> GetAllMitarbeiter()
    {
        return _mitarbeiterRepository.GetAll();
    }

    public Mitarbeiter GetMitarbeiterById(int id)
    {
        return _mitarbeiterRepository.GetById(id);
    }

    public IEnumerable<Mitarbeiter> SearchMitarbeiter(string search)
    {
        return _mitarbeiterRepository.Search(search);
    }

    public void CreateMitarbeiter(Mitarbeiter mitarbeiter)
    {
        _mitarbeiterRepository.Add(mitarbeiter);
    }

    public void UpdateMitarbeiter(Mitarbeiter mitarbeiter)
    {
        _mitarbeiterRepository.Update(mitarbeiter);
    }

    public void DeleteMitarbeiter(int id)
    {
        _mitarbeiterRepository.Delete(id);
    }
}