namespace Data.Repositories;
using Domain;

public interface IEmployeeRepository
{
    Task<OperationResult<IEnumerable<Employee>>> GetAll();
    Task<OperationResult<Employee>> GetById(int id);
    Task<OperationResult<IEnumerable<Employee>>> Search(string search);
    Task<OperationResult> Add(Employee? employee);
    Task<OperationResult> Update(int id, Employee? employee);
    Task<OperationResult> Delete(int id);
}
