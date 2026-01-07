namespace Application;
using Domain;
using Data.Repositories;


public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public Task<OperationResult<IEnumerable<Employee>>> GetAllEmployees(CancellationToken cancellationToken = default)
    {
        return _employeeRepository.GetAll(cancellationToken);
    }

    public Task<OperationResult<Employee>> GetEmployeeById(int id, CancellationToken cancellationToken = default)
    {
        return _employeeRepository.GetById(id, cancellationToken);
    }

    public Task<OperationResult<IEnumerable<Employee>>> SearchEmployees(string search, CancellationToken cancellationToken = default)
    {
        return _employeeRepository.Search(search, cancellationToken);
    }

    public Task<OperationResult> CreateEmployee(Employee employee, CancellationToken cancellationToken = default)
    {
        return _employeeRepository.Add(employee, cancellationToken);
    }

    public Task<OperationResult> UpdateEmployee(int id, Employee employee, CancellationToken cancellationToken = default)
    {
        return _employeeRepository.Update(id, employee, cancellationToken);
    }

    public Task<OperationResult> DeleteEmployee(int id, CancellationToken cancellationToken = default)
    {
        return _employeeRepository.Delete(id, cancellationToken);
    }
}
