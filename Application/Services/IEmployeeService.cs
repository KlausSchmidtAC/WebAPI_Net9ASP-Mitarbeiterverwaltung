namespace Application;
using Domain;

public interface IEmployeeService
{
    Task<OperationResult<IEnumerable<Employee>>> GetAllEmployees(CancellationToken cancellationToken = default);
    Task<OperationResult<Employee>> GetEmployeeById(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<IEnumerable<Employee>>> SearchEmployees(string search, CancellationToken cancellationToken = default);
    Task<OperationResult> CreateEmployee(Employee employee, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateEmployee(int id, Employee employee, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteEmployee(int id, CancellationToken cancellationToken = default);
}
