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
public class MitarbeiterControllerTests
{
    // still incomplete: Search LastName, UpdateMitarbeiter..

    IMitarbeiterService _service;
    MitarbeiterController _controller;

    [SetUp]
    public void Setup()
    {
        _service = Substitute.For<IMitarbeiterService>();
        var logger = Substitute.For<ILogger<MitarbeiterController>>();
        _controller = new MitarbeiterController(_service, logger);
    }

    // Hilfsmethode um JSON-Responses zu verarbeiten
    private static JsonDocument ParseJsonResponse(object? responseValue)
    {
        var jsonString = JsonSerializer.Serialize(responseValue);
        return JsonDocument.Parse(jsonString);
    }

    [Test]
    public async Task GetAll_WhenServiceReturnsData_ReturnsOk()
    {   
        // Arrange
        var testData = new List<Mitarbeiter>
        {
            new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true),
            new Mitarbeiter(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        var successResult = OperationResult<IEnumerable<Mitarbeiter>>.SuccessResult(testData.AsEnumerable());
        _service.GetAllMitarbeiter().Returns(successResult);

        // Act
        var result = await _controller.GetAll();

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());

        var okResult = result.Result as OkObjectResult;
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Alle Mitarbeiter"));
        var dataArray = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataArray.GetArrayLength(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAll_WhenServiceReturnsEmptyList_ReturnsNotFound()
    {
        // Arrange
        var emptyData = new List<Mitarbeiter>();
        var failureResult = OperationResult<IEnumerable<Mitarbeiter>>.FailureResult("Keine Mitarbeiter in der Liste.");
        _service.GetAllMitarbeiter().Returns(failureResult);

        // Act
        var result = await _controller.GetAll();

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        
        using var jsonDoc = ParseJsonResponse(notFoundResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Keine Mitarbeiter in der Liste."));
    }

    [Test]
    public async Task GetById_ExistingId_ReturnsMitarbeiter()
    {
        // Arrange
        var testmitarbeiter = new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true);
        var successResult = OperationResult<Mitarbeiter>.SuccessResult(testmitarbeiter);
        _service.GetMitarbeiterById(1).Returns(successResult);

        // Act
        var result = await _controller.GetMitarbeiter(1);

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Mitarbeiter mit ID 1 gefunden"));
        
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
        var failureResult = OperationResult<Mitarbeiter>.FailureResult("Mitarbeiter mit der ID = 99 nicht existent.");
        _service.GetMitarbeiterById(99).Returns(failureResult);

        // Act
        var result = await _controller.GetMitarbeiter(99);

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        
        using var jsonDoc = ParseJsonResponse(notFoundResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Mitarbeiter mit der ID = 99 nicht existent."));
    }

    [Test]
    public async Task GetById_InvalidId_ReturnsBadRequest()
    {
        // Act - verwende ungültige ID (<=0)
        var result = await _controller.GetMitarbeiter(-1);

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result.Result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Unzulässige ID"));
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
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'."));
        
        var badRequestResult2 = result2.Result as BadRequestObjectResult;
        using var jsonDoc2 = ParseJsonResponse(badRequestResult2?.Value);
        Assert.That(jsonDoc2.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'."));
        
        var badRequestResult3 = result3.Result as BadRequestObjectResult;
        using var jsonDoc3 = ParseJsonResponse(badRequestResult3?.Value);
        Assert.That(jsonDoc3.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'."));
    }

    [Test]
    public async Task GetByDate_ValidDateWithResults_ReturnsOk()
    {
        // Arrange
        var testData = new List<Mitarbeiter>
        {
            new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true),
            new Mitarbeiter(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        var successResult = OperationResult<IEnumerable<Mitarbeiter>>.SuccessResult(new List<Mitarbeiter> { testData[0] }.AsEnumerable());
        _service.SearchMitarbeiter("1985-01-16").Returns(successResult);

        // Act
        var result = await _controller.GetByDate("1985-01-16");

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());

        var okResult = result.Result as OkObjectResult;
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Mitarbeiter mit aelterem Geburtsdatum als 1985-01-16 gefunden"));
        
        var dataArray = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataArray.GetArrayLength(), Is.EqualTo(1));
        
        var firstMitarbeiter = dataArray[0];
        Assert.That(firstMitarbeiter.GetProperty("FirstName").GetString(), Is.EqualTo("Max"));
        Assert.That(firstMitarbeiter.GetProperty("LastName").GetString(), Is.EqualTo("Mustermann"));
        Assert.That(firstMitarbeiter.GetProperty("BirthDate").GetString(), Is.EqualTo("1985-01-15"));
        Assert.That(firstMitarbeiter.GetProperty("IsActive").GetBoolean(), Is.True);
    }

    [Test]
    public async Task GetByDate_ValidDateWithoutResults_ReturnsNotFound()
    {
        // Arrange
        var testData = new List<Mitarbeiter>
        {
            new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true),
            new Mitarbeiter(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        var failureResult = OperationResult<IEnumerable<Mitarbeiter>>.FailureResult("Kein Mitarbeiter mit früherem Geburtsdatum als 1985-01-15 gefunden.");
        _service.SearchMitarbeiter("1985-01-15").Returns(failureResult);

        // Act
        var result = await _controller.GetByDate("1985-01-15");

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        
        using var jsonDoc = ParseJsonResponse(notFoundResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Kein Mitarbeiter mit früherem Geburtsdatum als 1985-01-15 gefunden."));
    }

    [Test]
    public async Task GetSorted_IsActiveFilter_ReturnsActiveEmployees()
    {
        // Arrange
        var activeEmployees = new List<Mitarbeiter>
        {
            new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true),
            new Mitarbeiter(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        var successResult = OperationResult<IEnumerable<Mitarbeiter>>.SuccessResult(activeEmployees.AsEnumerable());
        _service.SearchMitarbeiter("isActive").Returns(successResult);

        // Act
        var result = await _controller.GetSorted("isActive");

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Alle aktiven Mitarbeiter"));
        Assert.That(jsonDoc.RootElement.GetProperty("Filter").GetString(), Is.EqualTo("isActive"));
        Assert.That(jsonDoc.RootElement.GetProperty("Count").GetInt32(), Is.EqualTo(2));
        
        var dataArray = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataArray.GetArrayLength(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetSorted_LastNameFilter_ReturnsEmployeesSortedByLastName()
    {
        // Arrange
        var sortedEmployees = new List<Mitarbeiter>
        {
            new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true),
            new Mitarbeiter(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        var successResult = OperationResult<IEnumerable<Mitarbeiter>>.SuccessResult(sortedEmployees.AsEnumerable());
        _service.SearchMitarbeiter("LastName").Returns(successResult);

        // Act
        var result = await _controller.GetSorted("LastName");

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Alle Mitarbeiter nach Nachname aufsteigend alphabetisch sortiert"));
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
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Bitte einen gültigen Mitarbeiterfilter eingegeben."));
    }

    [Test]
    public async Task CreateMitarbeiter_ValidMitarbeiter_ReturnsCreatedAtAction()
    {
        //Arrange 
        var newOne = new Mitarbeiter(3, "John", "Doe", "1978-11-22", false);
        var successResult = OperationResult.SuccessResult();
        _service.CreateMitarbeiter(newOne).Returns(successResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOne);

        // Assert
        Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        var createdAtActionResult = result as CreatedAtActionResult;
        
        using var jsonDoc = ParseJsonResponse(createdAtActionResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Neuer Mitarbeiter erstellt"));
        
        var dataObject = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataObject.GetProperty("id").GetInt32(), Is.EqualTo(3));
        Assert.That(dataObject.GetProperty("FirstName").GetString(), Is.EqualTo("John"));
        Assert.That(dataObject.GetProperty("LastName").GetString(), Is.EqualTo("Doe"));
    }

    [Test]
    public async Task CreateMitarbeiter_NullMitarbeiter_ReturnsBadRequest()
    {
        // Arrange
        var newOneNull = null as Mitarbeiter;
        var failureResult = OperationResult.FailureResult("Mitarbeiterdaten sind korrumpiert oder leer.");
        _service.CreateMitarbeiter(newOneNull!).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneNull!);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Mitarbeiterdaten sind korrumpiert oder leer."));
    }

    [Test]
    [TestCase("22-11-1978")]
    [TestCase("1978-14-25")]
    [TestCase("     ")]
    [TestCase("")]
    public async Task CreateMitarbeiter_InvalidDateFormat_ReturnsBadRequest(string invalidDate)
    {
        // Arrange
        var newOneWrongDateFormat = new Mitarbeiter(3, "John", "Doe", invalidDate, false);
        var failureResult = OperationResult.FailureResult("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.");
        _service.CreateMitarbeiter(newOneWrongDateFormat).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneWrongDateFormat);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich."));
    }

    [Test]
    public async Task CreateMitarbeiter_DateIsNull_ReturnsBadRequest()
    {
        // Arrange
        var newOneNullDateFormat = new Mitarbeiter(3, "John", "Doe", "", false);
        var failureResult = OperationResult.FailureResult("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.");
        _service.CreateMitarbeiter(newOneNullDateFormat).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneNullDateFormat);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich."));
    }

    [Test]
    public async Task CreateMitarbeiter_DateHasInvalidChars_ReturnsBadRequest()
    {
        // Arrange
        var newOneInvalidCharsInDateFormat = new Mitarbeiter(3, "John", "Doe", "1$78\\1&//?2", false);
        var failureResult = OperationResult.FailureResult("Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben!");
        _service.CreateMitarbeiter(newOneInvalidCharsInDateFormat).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneInvalidCharsInDateFormat);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben!"));
    }

    [Test]
    public async Task CreateMitarbeiter_EmptyFirstName_ReturnsBadRequest()
    {
        // Arrange
        var newOneEmptyLastName = new Mitarbeiter(3, "John", "", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("Ein Vorname und ein Nachname sind erforderlich.");
        _service.CreateMitarbeiter(newOneEmptyLastName).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneEmptyLastName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }

    [Test]
    public async Task CreateMitarbeiter_WhiteSpaceFirstName_ReturnsBadRequest()
    {
        // Arrange
        var newOneWhiteSpaceFirstName = new Mitarbeiter(3, "   ", "Doe", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("Ein Vorname und ein Nachname sind erforderlich.");
        _service.CreateMitarbeiter(newOneWhiteSpaceFirstName).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneWhiteSpaceFirstName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }

    [Test]
    public async Task CreateMitarbeiter_NullFirstName_ReturnsBadRequest()
    {
        // Arrange
        var newOneNullFirstName = new Mitarbeiter(3, null!, "Doe", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("Ein Vorname und ein Nachname sind erforderlich.");
        _service.CreateMitarbeiter(newOneNullFirstName).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneNullFirstName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }

    [Test]
    public async Task CreateMitarbeiter_EmptyLastName_ReturnsBadRequest()
    {
        // Arrange
        var newOneEmptyLastName = new Mitarbeiter(3, "John", "", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("Ein Vorname und ein Nachname sind erforderlich.");
        _service.CreateMitarbeiter(newOneEmptyLastName).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneEmptyLastName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }

    [Test]
    public async Task CreateMitarbeiter_WhiteSpaceLastName_ReturnsBadRequest()
    {
        // Arrange
        var newOneWhiteSpaceLastName = new Mitarbeiter(3, "John", "   ", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("Ein Vorname und ein Nachname sind erforderlich.");
        _service.CreateMitarbeiter(newOneWhiteSpaceLastName).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneWhiteSpaceLastName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }

    [Test]
    public async Task CreateMitarbeiter_NullLastName_ReturnsBadRequest()
    {
        // Arrange
        var newOneNullFirstName = new Mitarbeiter(3, null!, "Doe", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("Ein Vorname und ein Nachname sind erforderlich.");
        _service.CreateMitarbeiter(newOneNullFirstName).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneNullFirstName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }

    [Test]
    public async Task CreateMitarbeiter_DuplicateMitarbeiter_ReturnsBadRequest()
    {
        // Arrange
        var newOneDuplicate = new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true);
        var failureResult = OperationResult.FailureResult("Ein Mitarbeiter mit dem gleichen Vornamen, Nachnamen und Geburtsdatum existiert bereits.");
        _service.CreateMitarbeiter(newOneDuplicate).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneDuplicate);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Ein Mitarbeiter mit dem gleichen Vornamen, Nachnamen und Geburtsdatum existiert bereits."));
    }

    [Test]
    public async Task DeleteMitarbeiter_ValidId_ReturnsNotFound()
    {
        // Arrange
        int validId = 123456;
        var failureResult = OperationResult.FailureResult($"Mitarbeiter konnte nicht gelöscht werden, da diese Id = {validId} nicht existiert.");
        _service.DeleteMitarbeiter(validId).Returns(failureResult);

        // Act
        var result = await _controller.DeleteMitarbeiter(validId);

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        
        using var jsonDoc = ParseJsonResponse(notFoundResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo($"Mitarbeiter konnte nicht gelöscht werden, da diese Id = {validId} nicht existiert."));
    }

    [Test]
    public async Task DeleteMitarbeiter_invalidId_ReturnsBadRequest()
    {
        // Arrange
        int invalidId = -99;

        // Act
        var result = await _controller.DeleteMitarbeiter(invalidId);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Unzulässige ID"));
    }

    [Test]
    public async Task DeleteMitarbeiter_validId_ReturnsNoContent()
    {
        // Arrange
        int validId = 1;
        var successResult = OperationResult.SuccessResult();
        _service.DeleteMitarbeiter(validId).Returns(successResult);

        // Act
        var result = await _controller.DeleteMitarbeiter(validId);

        // Assert
        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task UpdateMitarbeiter_ValidData_ReturnsOk()
    {
        // Arrange
        int validId = 1;
        var updatedMitarbeiter = new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true);
        var successResult = OperationResult.SuccessResult();
        _service.UpdateMitarbeiter(validId, updatedMitarbeiter).Returns(successResult);

        // Act
        var result = await _controller.UpdateMitarbeiter(validId, updatedMitarbeiter);

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        
        using var jsonDoc = ParseJsonResponse(okResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo($"Mitarbeiter mit der ID {validId} wurde erfolgreich aktualisiert"));
        
        var dataObject = jsonDoc.RootElement.GetProperty("Data");
        Assert.That(dataObject.GetProperty("id").GetInt32(), Is.EqualTo(1));
        Assert.That(dataObject.GetProperty("FirstName").GetString(), Is.EqualTo("Max"));
        Assert.That(dataObject.GetProperty("LastName").GetString(), Is.EqualTo("Mustermann"));
    }

    [Test]
    public async Task UpdateMitarbeiter_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        int invalidId = -1;
        var mitarbeiter = new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true);

        // Act
        var result = await _controller.UpdateMitarbeiter(invalidId, mitarbeiter);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Unzulässige ID"));
    }

    [Test]
    public async Task UpdateMitarbeiter_ServiceFailure_ReturnsBadRequest()
    {
        // Arrange
        int validId = 1;
        var mitarbeiter = new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true);
        var failureResult = OperationResult.FailureResult("Update fehlgeschlagen");
        _service.UpdateMitarbeiter(validId, mitarbeiter).Returns(failureResult);

        // Act
        var result = await _controller.UpdateMitarbeiter(validId, mitarbeiter);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        
        using var jsonDoc = ParseJsonResponse(badRequestResult?.Value);
        Assert.That(jsonDoc.RootElement.GetProperty("Message").GetString(), Is.EqualTo("Update fehlgeschlagen"));
    }
}


