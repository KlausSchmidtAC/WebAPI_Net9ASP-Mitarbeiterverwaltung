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

    public Task<OperationResult<IEnumerable<Mitarbeiter>>> GetAllMitarbeiter()
    {
        return _mitarbeiterRepository.GetAll();
    }

    public Task<OperationResult<Mitarbeiter>> GetMitarbeiterById(int id)
    {
        return _mitarbeiterRepository.GetById(id);
    }

    public Task<OperationResult<IEnumerable<Mitarbeiter>>> SearchMitarbeiter(string search)
    {
        return _mitarbeiterRepository.Search(search);
    }

    public Task<OperationResult> CreateMitarbeiter(Mitarbeiter mitarbeiter)
    {
        return _mitarbeiterRepository.Add(mitarbeiter);
    }

    public Task<OperationResult> UpdateMitarbeiter(int id, Mitarbeiter mitarbeiter)
    {
        return _mitarbeiterRepository.Update(id, mitarbeiter);
    }

    public Task<OperationResult> DeleteMitarbeiter(int id)
    {
        return _mitarbeiterRepository.Delete(id);
    }
}
