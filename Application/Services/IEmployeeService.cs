namespace Application;
using Domain;

public interface IEmployeeService
{
    Task<OperationResult<IEnumerable<Employee>>> GetAllEmployees();
    Task<OperationResult<Employee>> GetEmployeeById(int id);
    Task<OperationResult<IEnumerable<Employee>>> SearchEmployees(string search);
    Task<OperationResult> CreateEmployee(Employee employee);
    Task<OperationResult> UpdateEmployee(int id, Employee employee);
    Task<OperationResult> DeleteEmployee(int id);
}
