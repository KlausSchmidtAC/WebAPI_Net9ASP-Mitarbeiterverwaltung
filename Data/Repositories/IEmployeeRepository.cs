namespace Data.Repositories;
using Domain;

public interface IEmployeeRepository
{
    Task<OperationResult<IEnumerable<Employee>>> GetAll(CancellationToken cancellationToken = default);
    Task<OperationResult<Employee>> GetById(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<IEnumerable<Employee>>> Search(string search, CancellationToken cancellationToken = default);
    Task<OperationResult> Add(Employee? employee, CancellationToken cancellationToken = default);
    Task<OperationResult> Update(int id, Employee? employee, CancellationToken cancellationToken = default);
    Task<OperationResult> Delete(int id, CancellationToken cancellationToken = default);
}
