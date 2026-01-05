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

    public Task<OperationResult<IEnumerable<Employee>>> GetAllEmployees()
    {
        return _employeeRepository.GetAll();
    }

    public Task<OperationResult<Employee>> GetEmployeeById(int id)
    {
        return _employeeRepository.GetById(id);
    }

    public Task<OperationResult<IEnumerable<Employee>>> SearchEmployees(string search)
    {
        return _employeeRepository.Search(search);
    }

    public Task<OperationResult> CreateEmployee(Employee employee)
    {
        return _employeeRepository.Add(employee);
    }

    public Task<OperationResult> UpdateEmployee(int id, Employee employee)
    {
        return _employeeRepository.Update(id, employee);
    }

    public Task<OperationResult> DeleteEmployee(int id)
    {
        return _employeeRepository.Delete(id);
    }
}
