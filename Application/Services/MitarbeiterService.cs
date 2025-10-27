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

    public Mitarbeiter? GetMitarbeiterById(int id)
    {
        return _mitarbeiterRepository.GetById(id);
    }

    public IEnumerable<Mitarbeiter>? SearchMitarbeiter(string search)
    {
        return _mitarbeiterRepository.Search(search);
    }

    public bool CreateMitarbeiter(Mitarbeiter mitarbeiter, out string? errorMessage)
    {
        return _mitarbeiterRepository.Add(mitarbeiter, out errorMessage);
    }

    public bool UpdateMitarbeiter(int id, Mitarbeiter mitarbeiter, out string? errorMessage)
    {
        return _mitarbeiterRepository.Update(id, mitarbeiter, out errorMessage);
    }

    public bool DeleteMitarbeiter(int id,out string? errorMessage)
    {
        return _mitarbeiterRepository.Delete(id, out errorMessage);
    }
}