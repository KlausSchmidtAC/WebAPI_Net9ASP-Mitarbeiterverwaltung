using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain;
using Application;
using Domain.Constants;


namespace WebAPI_NET9.Controllers
{   
    [ApiController]
    [Route("api/employees")] // ← Route now in English
    public class EmployeeController : ControllerBase
    {

        IEmployeeService _employeeService;
        ILogger<EmployeeController> _logger; 
        public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
        {
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("EmployeeController initialized.");
        }

    
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetAll(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("GetAll called");

            var operationResult = await _employeeService.GetAllEmployees(cancellationToken);
            if (!operationResult.Success)
            {
                _logger.LogWarning("GetAll request: No employees in the list.");
                return NotFound(new { Message = operationResult.ErrorMessage });
            }
            else
            {
                _logger.LogInformation("{EmployeeCount} employees found", operationResult.Data.Count());
                return Ok(new { Message = "All employees", Data = operationResult.Data });
            }
        }

        [Authorize]
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetSorted([FromQuery] string? search, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(search))
            {   
                _logger.LogWarning("GetSorted request: Invalid employee filter.");
                return NotFound(new { Message = "Please enter a valid employee filter." });
            }
            var operationResult = await _employeeService.SearchEmployees(search, cancellationToken);

            if (!operationResult.Success)
            {   
                _logger.LogError("GetSorted request: Error during employee search. {ErrorMessage}", operationResult.ErrorMessage);
                return NotFound(new { Message = operationResult.ErrorMessage });
            }

            else if (search == "isActive")
            {
                if (operationResult.Data.Count() == 0)
                {
                    _logger.LogWarning("GetSorted request: No active employees in the list.");
                    return NotFound(new { Message = "No active employees in the list." });
                }   
                else
                    return Ok(new {Message = "All active employees", Filter = "isActive", Count = operationResult.Data.Count(), Data = operationResult.Data });
            }

            else if (search == "LastName")
            {
                if (operationResult.Data.Count() == 0)
                {
                    _logger.LogWarning("GetSorted request: No employees with last names in the list.");
                    return NotFound(new { Message = "No employees with last names in the list." });
                }   
                else
                    return Ok(new {Message = "All employees sorted alphabetically by last name", Filter = "LastName", Count = operationResult.Data.Count(), Data = operationResult.Data });
            }
            else if (operationResult.Data.Count() == 0)
            {   
                _logger.LogWarning("GetSorted request: No employee found with birth date earlier than {SearchDate}.", search);
                return NotFound(new { Message = $"No employee found with birth date earlier than {search}." });
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Sorted employees: {EmployeeList}",
                        string.Join(", ", operationResult.Data.ToList()));
                }
                else
                {
                    _logger.LogInformation("{EmployeeCount} employees found", operationResult.Data.Count());
                }
                
                return Ok(new {Message = $"All employees older than {search}", Filter = "Older than " + search, Count = operationResult.Data.Count(), Data = operationResult.Data });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee([FromRoute] int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {   
                _logger.LogWarning("GetEmployee request: Invalid ID {Id} provided.", id);
                return BadRequest(new { Message = "Invalid ID" });
            }

            var operationResult = await _employeeService.GetEmployeeById(id, cancellationToken);

            if (!operationResult.Success)
            {   
                _logger.LogError("GetEmployee request: Error retrieving employee. {ErrorMessage}", operationResult.ErrorMessage);
                return NotFound(new { Message = $"Employee with ID = {id} does not exist." });
            }

            _logger.LogInformation("Employee with ID {Id} found: {Employee}", id, operationResult.Data);
            return Ok(new {Message = $"Employee with ID {id} found", Filter = "ID", Count = 1, Data = operationResult.Data });
        }

        [Authorize]
        [HttpGet("birthDate")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetByDate([FromQuery] string? birthDate, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(birthDate))
            {   
                _logger.LogWarning("GetByDate request: Invalid date format or date input. Please use 'yyyy-MM-dd'.");
                return BadRequest(new { Message = "Invalid date format or date input. Please use 'yyyy-MM-dd'." });
            }

            var operationResult = await _employeeService.SearchEmployees(birthDate, cancellationToken);

            if (!operationResult.Success)
            {
                _logger.LogError("GetByDate request: Error during employee search by birth date. {ErrorMessage}", operationResult.ErrorMessage);
                return NotFound(new { Message = operationResult.ErrorMessage });
            }
            
            if(_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Employees with birth date earlier than {BirthDate} found: {Employees}", birthDate, operationResult.Data.ToList());
            }
            else
            {
                _logger.LogInformation("Employees with earlier birth date found: {Count}", operationResult.Data.Count());
            }
            return Ok(new {Message = $"Employees with birth date earlier than {birthDate} found", Filter = "Older than " + birthDate, Count = operationResult.Data.Count(), Data = operationResult.Data });
        }

        [Authorize]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] Employee employee, CancellationToken cancellationToken = default)
        {
            var operationResult = await _employeeService.CreateEmployee(employee, cancellationToken);

            if (!operationResult.Success)
            {
                _logger.LogError("CreateEmployee request: Error creating employee. {ErrorMessage}", operationResult.ErrorMessage);
                return BadRequest(new { Message = operationResult.ErrorMessage });
            }
            _logger.LogInformation("New employee created: {Employee}", employee.ToString());
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.id }, new {Message = "New employee created", Data = employee});
        }

        [Authorize]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee([FromRoute] int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {   
                _logger.LogWarning("DeleteEmployee request: Invalid ID {Id}", id);
                return BadRequest(new { Message = "Invalid ID" });
            }

            var operationResult = await _employeeService.DeleteEmployee(id, cancellationToken);

            if (!operationResult.Success)
            {   
                _logger.LogError("DeleteEmployee request: Error deleting employee. {ErrorMessage}", operationResult.ErrorMessage);
                return NotFound(new { Message = operationResult.ErrorMessage });
            }
            _logger.LogInformation("Employee with ID {Id} was deactivated/deleted.", id);
            return NoContent();
        }

        [Authorize]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateEmployee([FromRoute] int id, [FromBody] Employee employee, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {   
                _logger.LogWarning("UpdateEmployee request: Invalid ID {Id}", id);
                return BadRequest(new { Message = "Invalid ID" });
            }

            var operationResult = await _employeeService.UpdateEmployee(id, employee, cancellationToken);

            if (!operationResult.Success)
            {   
                _logger.LogError("UpdateEmployee request: Error updating employee. {ErrorMessage}", operationResult.ErrorMessage);
                return BadRequest(new { Message = operationResult.ErrorMessage });
            }
            _logger.LogInformation("Employee with ID {Id} was updated: {Employee}.", id, employee.ToString());
            return Ok(new {Message = $"Employee with ID {id} was successfully updated", Data = employee} );
        }
    }
}
