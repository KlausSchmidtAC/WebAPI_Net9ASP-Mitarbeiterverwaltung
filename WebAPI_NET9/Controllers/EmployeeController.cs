using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain;
using Application;
using Domain.Constants;
using Swashbuckle.AspNetCore.Annotations;


namespace WebAPI_NET9.Controllers
{
    [ApiController]
    [Route("api/employees")] 
    [SwaggerTag("Employee Management – CRUD-Operations for Employees")]
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

        /// <summary>
        /// Retrieves a list of all employees.
        /// </summary>
        /// <remarks>
        /// Returns a complete, unsorted list of all employees.
        /// This endpoint is accessible without authentication for testing purposes.
        /// example: GET /api/employees with no Authorization header
        /// </remarks>
        [HttpGet]
        [SwaggerOperation(
            OperationId = "Employee_GetAll",
            Tags = new[] { "Employees" })]
        [SwaggerResponse(200, "Successful", typeof(IEnumerable<Employee>))]
        [SwaggerResponse(404, "No employees found")]
        [AllowAnonymous]
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


        /// <summary>
        /// Retrieves a list of employees based on the specified search criteria.
        /// </summary>
        /// <remarks>
        /// Protected endpoint that allows searching employees based on the specified search criteria in the `search` query parameter. Supported search criteria include:
        /// - `search=isActive`: Returns only active employees.
        /// - `search=LastName`: Returns employees sorted alphabetically by last name.
        /// Exmaple: GET /api/employees/search?search=isActive with Authorization in Request-Header: Bearer {token}
        /// </remarks>
        [Authorize]
        [SwaggerOperation(
            OperationId = "Employee_GetSorted",
            Tags = new[] { "Employees" })]
        [SwaggerResponse(200, "Successful", typeof(IEnumerable<Employee>))]
        [SwaggerResponse(400, "Invalid or missing search criteria")]
        [SwaggerResponse(401, "Unauthorized – valid JWT required")]
        [SwaggerResponse(404, "No employees found matching the search criteria")]
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetSorted([FromQuery][SwaggerParameter("The search criteria for filtering employees", Required = true)] string? search, CancellationToken cancellationToken = default)
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
                    return Ok(new { Message = "All active employees", Filter = "isActive", Count = operationResult.Data.Count(), Data = operationResult.Data });
            }

            else if (search == "LastName")
            {
                if (operationResult.Data.Count() == 0)
                {
                    _logger.LogWarning("GetSorted request: No employees with last names in the list.");
                    return NotFound(new { Message = "No employees with last names in the list." });
                }
                else
                    return Ok(new { Message = "All employees sorted alphabetically by last name", Filter = "LastName", Count = operationResult.Data.Count(), Data = operationResult.Data });
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
                    _logger.LogDebug("Sorted employees: {}",
                        string.Join(", ", operationResult.Data.ToList()));
                }
                else
                {
                    _logger.LogInformation("{EmployeeCount} employees found", operationResult.Data.Count());
                }

                return Ok(new { Message = $"All employees older than {search}", Filter = "Older than " + search, Count = operationResult.Data.Count(), Data = operationResult.Data });
            }
        }
        ///<summary>
        /// Retrieves an employee by their ID.
        ///</summary>
        ///<remarks> 
        ///Returns the employee with the specified ID. Requires a valid JWT Bearer token in the Authorization header.
        ///Use the /api/auth/token endpoint to generate a token with custom claims for testing. 
        /// Example: GET /api/employees/1 with Authorization in Request-Header: Bearer {token}
        ///</remarks> 
        [Authorize]
        [SwaggerOperation(
            OperationId = "Employee_GetById",
            Tags = new[] { "Employees" })]
        [SwaggerResponse(200, "Successful", typeof(Employee))]
        [SwaggerResponse(400, "Invalid ID supplied")]
        [SwaggerResponse(404, "Employee not found")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee([FromRoute][SwaggerParameter("Employee ID (must be > 0)", Required = true)] int id, CancellationToken cancellationToken = default)
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
            return Ok(new { Message = $"Employee with ID {id} found", Filter = "ID", Count = 1, Data = operationResult.Data });
        }


        /// <summary>
        /// Retrieves a list of employees with birth dates earlier than the specified date.    
        /// </summary>
        /// <remarks>
        /// Returns a list of employees with birth dates earlier than the specified date only in the format 'yyyy-MM-dd'. Requires a valid JWT Bearer token in the Authorization header.
        /// Use the /api/auth/token endpoint to generate a token with custom claims for testing. 
        /// Example: GET /api/employees/birthDate?birthDate=1990-01-01 with Authorization in Request-Header: Bearer {token}
        /// </remarks>
        [Authorize]
        [SwaggerOperation(
            OperationId = "Employee_GetByBirthDate",
            Tags = new[] { "Employees" })]
        [SwaggerResponse(200, "Successful", typeof(IEnumerable<Employee>))]
        [SwaggerResponse(400, "Invalid date format or date input")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "No employees found with birth date earlier than the specified date")]
        [HttpGet("birthDate")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetByDate([FromQuery] [SwaggerParameter("Birth date in 'yyyy-MM-dd' format", Required = true)] string? birthDate, CancellationToken cancellationToken = default)
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

            if (operationResult.Data?.Count() == 0)
            {
                _logger.LogWarning("GetByDate request: No employees found with birth date earlier than {BirthDate}.", birthDate);
                return NotFound(new { Message = $"No employees found with birth date earlier than {birthDate}." });
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Employees with birth date earlier than {BirthDate} found: {Employees}", birthDate, operationResult.Data.ToList());
            }
            else
            {
                _logger.LogInformation("Employees with earlier birth date found: {Count}", operationResult.Data.Count());
            }
            return Ok(new { Message = $"Employees with birth date earlier than {birthDate} found", Filter = "Older than " + birthDate, Count = operationResult.Data.Count(), Data = operationResult.Data });
        }

        ///<summary>
        /// Creates a new employee.
        /// </summary>
        /// <remarks>
        /// Creates a new employee based on the provided employee data. Requires a valid JWT Bearer token with the "AdminRole" claim set to "true" in the Authorization header.
        /// Use the /api/auth/token endpoint to generate a token with custom claims for testing.    
        /// Example: POST /api/employees with Authorization in Request-Header: Bearer {token} and JSON body: { "firstName": "John", "lastName": "Doe", "birthDate": "1990-01-01", "isActive": true }
        /// </remarks>
        [Authorize]
        [SwaggerOperation(
            OperationId = "Employee_Create",
            Tags = new[] { "Employees" })]
        [SwaggerResponse(201, "Employee created successfully", typeof(Employee))]
        [SwaggerResponse(400, "Invalid employee data")]
        [SwaggerResponse(401, "Unauthorized")]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]

        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody][SwaggerRequestBody("Employee data", Required = true)] Employee employee, CancellationToken cancellationToken = default)
        {
            var operationResult = await _employeeService.CreateEmployee(employee, cancellationToken);

            if (!operationResult.Success)
            {
                _logger.LogError("CreateEmployee request: Error creating employee. {ErrorMessage}", operationResult.ErrorMessage);
                return BadRequest(new { Message = operationResult.ErrorMessage });
            }
            _logger.LogInformation("New employee created: {Employee}", employee.ToString());
            var newEmployee = new Employee(operationResult.Data, employee.FirstName, employee.LastName, employee.BirthDate, employee.IsActive);
            return CreatedAtAction(nameof(GetEmployee), new { id = operationResult.Data }, new { Message = "New employee created", Data = newEmployee });
        }

        /// <summary>
        /// Deletes an employee by their ID.
        /// </summary>
        /// <remarks>
        /// Soft-deletes or removes the employee with the specified ID. Requires a valid JWT Bearer token
        /// with the claim 'admin = true' in the Authorization header.
        /// Example: DELETE /api/employees/1 with Authorization in Request-Header: Bearer {token}
        /// </remarks>
        [Authorize]
        [SwaggerOperation(
            OperationId = "Employee_Delete",
            Tags = new[] { "Employees" })]
        [SwaggerResponse(204, "Employee deleted successfully")]
        [SwaggerResponse(400, "Invalid ID supplied")]
        [SwaggerResponse(401, "Unauthorized – valid JWT with admin claim required")]
        [SwaggerResponse(404, "Employee not found")]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(
            [FromRoute][SwaggerParameter("Employee ID (must be > 0)", Required = true)] int id,
            CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Partially updates an existing employee.
        /// </summary>
        /// <remarks>
        /// Updates the employee with the specified ID based on the provided data. Requires a valid JWT Bearer token
        /// with the claim 'admin = true' in the Authorization header.
        /// Example: PATCH /api/employees/1 with Authorization in Request-Header: Bearer {token}
        /// and JSON body: { "firstName": "John", "lastName": "Doe", "birthDate": "1990-01-01", "isActive": true }
        /// </remarks>
        [Authorize]
        [SwaggerOperation(
            OperationId = "Employee_Update",
            Tags = new[] { "Employees" })]
        [SwaggerResponse(200, "Employee updated successfully", typeof(Employee))]
        [SwaggerResponse(400, "Invalid ID or employee data")]
        [SwaggerResponse(401, "Unauthorized – valid JWT with admin claim required")]
        [SwaggerResponse(404, "Employee not found")]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateEmployee(
            [FromRoute][SwaggerParameter("Employee ID (must be > 0)", Required = true)] int id,
            [FromBody][SwaggerRequestBody("Updated employee data", Required = true)] Employee employee,
            CancellationToken cancellationToken = default)
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
            return Ok(new { Message = $"Employee with ID {id} was successfully updated", Data = employee });
        }
    }
}
