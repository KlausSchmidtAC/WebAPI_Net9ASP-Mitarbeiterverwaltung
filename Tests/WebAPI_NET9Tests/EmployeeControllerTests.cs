namespace WebAPI_NET9Tests;
using NSubstitute;
using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebAPI_NET9.Controllers;
using Application;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json;

[TestFixture]
public class EmployeeControllerTests
{
    
    IEmployeeService _service;
    EmployeeController _controller;

    [SetUp]
    public void Setup()
    {
        _service = Substitute.For<IEmployeeService>();
        var logger = Substitute.For<ILogger<EmployeeController>>();
        _controller = new EmployeeController(_service, logger);
    }

    // Helper method to process JSON responses
    private static JsonDocument ParseJsonResponse(object? responseValue)
    {
        var jsonString = JsonSerializer.Serialize(responseValue);
        return JsonDocument.Parse(jsonString);
    }

    [Test]
    public async Task GetAll_WhenServiceReturnsData_ReturnsOk()
    {   
        // Arrange
        var testData = new List<Employee>
        {
            new Employee(1, "Max", "Mustermann", "1985-01-15", true),
            new Employee(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        var successResult = OperationResult<IEnumerable<Employee>>.SuccessResult(testData.AsEnumerable());
        _service.GetAllEmployees().Returns(successResult);

        // Act
        var result = await _controller.GetAll();

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());

        var okResult = result.Result as OkObjectResult;
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("All employees"));
        var dataArray = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataArray.GetArrayLength(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAll_WhenServiceReturnsEmptyList_ReturnsNotFound()
    {
        // Arrange
        var emptyData = new List<Employee>();
        var failureResult = OperationResult<IEnumerable<Employee>>.FailureResult("No employees in the list.");
        _service.GetAllEmployees().Returns(failureResult);

        // Act
        var result = await _controller.GetAll();

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        
        using var jsonDoc = ParseJsonResponse(notFoundResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("No employees in the list."));
    }

    [Test]
    public async Task GetById_ExistingId_ReturnsEmployee()
    {
        // Arrange
        var testEmployee = new Employee(1, "Max", "Mustermann", "1985-01-15", true);
        var successResult = OperationResult<Employee>.SuccessResult(testEmployee);
        _service.GetEmployeeById(1).Returns(successResult);

        // Act
        var result = await _controller.GetEmployee(1);

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Employee with ID 1 found"));
        
        var dataObject = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataObject.GetProperty("id").GetInt32(), Is.EqualTo(1));
        Assert.That(dataObject.GetProperty("FirstName").GetString(), Is.EqualTo("Max"));
        Assert.That(dataObject.GetProperty("LastName").GetString(), Is.EqualTo("Mustermann"));
        Assert.That(dataObject.GetProperty("BirthDate").GetString(), Is.EqualTo("1985-01-15"));
        Assert.That(dataObject.GetProperty("IsActive").GetBoolean(), Is.True);
    }

    [Test]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var failureResult = OperationResult<Employee>.FailureResult("Employee with ID = 99 does not exist.");
        _service.GetEmployeeById(99).Returns(failureResult);

        // Act
        var result = await _controller.GetEmployee(99);

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        
        using var jsonDoc = ParseJsonResponse(notFoundResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Employee with ID = 99 does not exist."));
    }

    [Test]
    public async Task GetById_InvalidId_ReturnsBadRequest()
    {
        // Act - use invalid ID (<=0)
        var result = await _controller.GetEmployee(-1);

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result.Result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Invalid ID"));
    }

    [Test]
    public async Task GetByDate_IsNullOrEmpty_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetByDate(string.Empty);
        var result2 = await _controller.GetByDate(null);
        var result3 = await _controller.GetByDate("   ");

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result.Result as BadRequestObjectResult;
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Invalid date format or date input. Please use 'yyyy-MM-dd'."));
        
        var badRequestResult2 = result2.Result as BadRequestObjectResult;
        using var jsonDoc2 = ParseJsonResponse(badRequestResult2?.Value);
        Assert.That(jsonDoc2.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Invalid date format or date input. Please use 'yyyy-MM-dd'."));
        
        var badRequestResult3 = result3.Result as BadRequestObjectResult;
        using var jsonDoc3 = ParseJsonResponse(badRequestResult3?.Value);
        Assert.That(jsonDoc3.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Invalid date format or date input. Please use 'yyyy-MM-dd'."));
    }

    [Test]
    public async Task GetByDate_ValidDateWithResults_ReturnsOk()
    {
        // Arrange
        var testData = new List<Employee>
        {
            new Employee(1, "Max", "Mustermann", "1985-01-15", true),
            new Employee(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        var successResult = OperationResult<IEnumerable<Employee>>.SuccessResult(new List<Employee> { testData[0] }.AsEnumerable());
        _service.SearchEmployees("1985-01-16").Returns(successResult);

        // Act
        var result = await _controller.GetByDate("1985-01-16");

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());

        var okResult = result.Result as OkObjectResult;
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Employees with birth date earlier than 1985-01-16 found"));
        
        var dataArray = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataArray.GetArrayLength(), Is.EqualTo(1));
        
        var firstEmployee = dataArray[0];
        Assert.That(firstEmployee.GetProperty("FirstName").GetString(), Is.EqualTo("Max"));
        Assert.That(firstEmployee.GetProperty("LastName").GetString(), Is.EqualTo("Mustermann"));
        Assert.That(firstEmployee.GetProperty("BirthDate").GetString(), Is.EqualTo("1985-01-15"));
        Assert.That(firstEmployee.GetProperty("IsActive").GetBoolean(), Is.True);
    }

    [Test]
    public async Task GetByDate_ValidDateWithoutResults_ReturnsNotFound()
    {
        // Arrange
        var testData = new List<Employee>
        {
            new Employee(1, "Max", "Mustermann", "1985-01-15", true),
            new Employee(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        var failureResult = OperationResult<IEnumerable<Employee>>.FailureResult("No employee found with birth date earlier than 1985-01-15.");
        _service.SearchEmployees("1985-01-15").Returns(failureResult);

        // Act
        var result = await _controller.GetByDate("1985-01-15");

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        
        using var jsonDoc = ParseJsonResponse(notFoundResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("No employee found with birth date earlier than 1985-01-15."));
    }

    [Test]
    public async Task GetSorted_IsActiveFilter_ReturnsActiveEmployees()
    {
        // Arrange
        var activeEmployees = new List<Employee>
        {
            new Employee(1, "Max", "Mustermann", "1985-01-15", true),
            new Employee(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        var successResult = OperationResult<IEnumerable<Employee>>.SuccessResult(activeEmployees.AsEnumerable());
        _service.SearchEmployees("isActive").Returns(successResult);

        // Act
        var result = await _controller.GetSorted("isActive");

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("All active employees"));
        Assert.That(jsonDoc.RootElement.GetProperty("Filter").GetString(), Is.EqualTo("isActive"));
        Assert.That(jsonDoc.RootElement.GetProperty("Count").GetInt32(), Is.EqualTo(2));
        
        var dataArray = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataArray.GetArrayLength(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetSorted_LastNameFilter_ReturnsEmployeesSortedByLastName()
    {
        // Arrange
        var sortedEmployees = new List<Employee>
        {
            new Employee(1, "Max", "Mustermann", "1985-01-15", true),
            new Employee(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        var successResult = OperationResult<IEnumerable<Employee>>.SuccessResult(sortedEmployees.AsEnumerable());
        _service.SearchEmployees("LastName").Returns(successResult);

        // Act
        var result = await _controller.GetSorted("LastName");

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("All employees sorted alphabetically by last name"));
        Assert.That(jsonDoc.RootElement.GetProperty("Filter").GetString(), Is.EqualTo("LastName"));
        Assert.That(jsonDoc.RootElement.GetProperty("Count").GetInt32(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetSorted_EmptyFilter_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetSorted("");

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        
        using var jsonDoc = ParseJsonResponse(notFoundResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Please enter a valid employee filter."));
    }

    [Test]
    public async Task CreateEmployee_ValidEmployee_ReturnsCreatedAtAction()
    {
        // Arrange 
        var newOne = new Employee(3, "John", "Doe", "1978-11-22", false);
        var successResult = OperationResult.SuccessResult();
        _service.CreateEmployee(newOne).Returns(successResult);

        // Act
        var result = await _controller.CreateEmployee(newOne);

        // Assert
        Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        var createdAtActionResult = result as CreatedAtActionResult;
        
        using var jsonDoc = ParseJsonResponse(createdAtActionResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("New employee created"));
        
        var dataObject = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataObject.GetProperty("id").GetInt32(), Is.EqualTo(3));
        Assert.That(dataObject.GetProperty("FirstName").GetString(), Is.EqualTo("John"));
        Assert.That(dataObject.GetProperty("LastName").GetString(), Is.EqualTo("Doe"));
    }

    [Test]
    public async Task CreateEmployee_NullEmployee_ReturnsBadRequest()
    {
        // Arrange
        var newOneNull = null as Employee;
        var failureResult = OperationResult.FailureResult("Employee data is corrupted or empty.");
        _service.CreateEmployee(newOneNull!).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneNull!);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Employee data is corrupted or empty."));
    }

    [Test]
    [TestCase("22-11-1978")]
    [TestCase("1978-14-25")]
    [TestCase("     ")]
    [TestCase("")]
    public async Task CreateEmployee_InvalidDateFormat_ReturnsBadRequest(string invalidDate)
    {
        // Arrange
        var newOneWrongDateFormat = new Employee(3, "John", "Doe", invalidDate, false);
        var failureResult = OperationResult.FailureResult("A valid birth date in format 'yyyy-MM-dd' is required.");
        _service.CreateEmployee(newOneWrongDateFormat).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneWrongDateFormat);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("A valid birth date in format 'yyyy-MM-dd' is required."));
    }

    [Test]
    public async Task CreateEmployee_DateIsNull_ReturnsBadRequest()
    {
        // Arrange
        var newOneNullDateFormat = new Employee(3, "John", "Doe", "", false);
        var failureResult = OperationResult.FailureResult("A valid birth date in format 'yyyy-MM-dd' is required.");
        _service.CreateEmployee(newOneNullDateFormat).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneNullDateFormat);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("A valid birth date in format 'yyyy-MM-dd' is required."));
    }

    [Test]
    public async Task CreateEmployee_DateHasInvalidChars_ReturnsBadRequest()
    {
        // Arrange
        var newOneInvalidCharsInDateFormat = new Employee(3, "John", "Doe", "1$78\\1&//?2", false);
        var failureResult = OperationResult.FailureResult("Error processing birth date: invalid characters entered!");
        _service.CreateEmployee(newOneInvalidCharsInDateFormat).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneInvalidCharsInDateFormat);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Error processing birth date: invalid characters entered!"));
    }

    [Test]
    public async Task CreateEmployee_EmptyFirstName_ReturnsBadRequest()
    {
        // Arrange
        var newOneEmptyLastName = new Employee(3, "John", "", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("A first name and a last name are required.");
        _service.CreateEmployee(newOneEmptyLastName).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneEmptyLastName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("A first name and a last name are required."));
    }

    [Test]
    public async Task CreateEmployee_WhiteSpaceFirstName_ReturnsBadRequest()
    {
        // Arrange
        var newOneWhiteSpaceFirstName = new Employee(3, "   ", "Doe", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("A first name and a last name are required.");
        _service.CreateEmployee(newOneWhiteSpaceFirstName).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneWhiteSpaceFirstName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("A first name and a last name are required."));
    }

    [Test]
    public async Task CreateEmployee_NullFirstName_ReturnsBadRequest()
    {
        // Arrange
        var newOneNullFirstName = new Employee(3, null!, "Doe", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("A first name and a last name are required.");
        _service.CreateEmployee(newOneNullFirstName).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneNullFirstName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("A first name and a last name are required."));
    }

    [Test]
    public async Task CreateEmployee_EmptyLastName_ReturnsBadRequest()
    {
        // Arrange
        var newOneEmptyLastName = new Employee(3, "John", "", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("A first name and a last name are required.");
        _service.CreateEmployee(newOneEmptyLastName).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneEmptyLastName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("A first name and a last name are required."));
    }

    [Test]
    public async Task CreateEmployee_WhiteSpaceLastName_ReturnsBadRequest()
    {
        // Arrange
        var newOneWhiteSpaceLastName = new Employee(3, "John", "   ", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("A first name and a last name are required.");
        _service.CreateEmployee(newOneWhiteSpaceLastName).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneWhiteSpaceLastName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("A first name and a last name are required."));
    }

    [Test]
    public async Task CreateEmployee_NullLastName_ReturnsBadRequest()
    {
        // Arrange
        var newOneNullFirstName = new Employee(3, null!, "Doe", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("A first name and a last name are required.");
        _service.CreateEmployee(newOneNullFirstName).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneNullFirstName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("A first name and a last name are required."));
    }

    [Test]
    public async Task CreateEmployee_DuplicateEmployee_ReturnsBadRequest()
    {
        // Arrange
        var newOneDuplicate = new Employee(1, "Max", "Mustermann", "1985-01-15", true);
        var failureResult = OperationResult.FailureResult("An employee with the same first name, last name and birth date already exists.");
        _service.CreateEmployee(newOneDuplicate).Returns(failureResult);

        // Act
        var result = await _controller.CreateEmployee(newOneDuplicate);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("An employee with the same first name, last name and birth date already exists."));
    }

    [Test]
    public async Task DeleteEmployee_ValidId_ReturnsNotFound()
    {
        // Arrange
        int validId = 123456;
        var failureResult = OperationResult.FailureResult($"Employee could not be deleted, as ID = {validId} does not exist.");
        _service.DeleteEmployee(validId).Returns(failureResult);

        // Act
        var result = await _controller.DeleteEmployee(validId);

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        
        using var jsonDoc = ParseJsonResponse(notFoundResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo($"Employee could not be deleted, as ID = {validId} does not exist."));
    }

    [Test]
    public async Task DeleteEmployee_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        int invalidId = -99;

        // Act
        var result = await _controller.DeleteEmployee(invalidId);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Invalid ID"));
    }

    [Test]
    public async Task DeleteEmployee_ValidId_ReturnsNoContent()
    {
        // Arrange
        int validId = 1;
        var successResult = OperationResult.SuccessResult();
        _service.DeleteEmployee(validId).Returns(successResult);

        // Act
        var result = await _controller.DeleteEmployee(validId);

        // Assert
        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task UpdateEmployee_ValidData_ReturnsOk()
    {
        // Arrange
        int validId = 1;
        var updatedEmployee = new Employee(1, "Max", "Mustermann", "1985-01-15", true);
        var successResult = OperationResult.SuccessResult();
        _service.UpdateEmployee(validId, updatedEmployee).Returns(successResult);

        // Act
        var result = await _controller.UpdateEmployee(validId, updatedEmployee);

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo($"Employee with ID {validId} was successfully updated"));
        
        var dataObject = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataObject.GetProperty("id").GetInt32(), Is.EqualTo(1));
        Assert.That(dataObject.GetProperty("FirstName").GetString(), Is.EqualTo("Max"));
        Assert.That(dataObject.GetProperty("LastName").GetString(), Is.EqualTo("Mustermann"));
    }

    [Test]
    public async Task UpdateEmployee_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        int invalidId = -1;
        var employee = new Employee(1, "Max", "Mustermann", "1985-01-15", true);

        // Act
        var result = await _controller.UpdateEmployee(invalidId, employee);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Invalid ID"));
    }

    [Test]
    public async Task UpdateEmployee_ServiceFailure_ReturnsBadRequest()
    {
        // Arrange
        int validId = 1;
        var employee = new Employee(1, "Max", "Mustermann", "1985-01-15", true);
        var failureResult = OperationResult.FailureResult("Update failed");
        _service.UpdateEmployee(validId, employee).Returns(failureResult);

        // Act
        var result = await _controller.UpdateEmployee(validId, employee);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Update failed"));
    }
}