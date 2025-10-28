namespace WebAPI_NET9Tests;
using NSubstitute;
using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
using WebAPI_NET9.Controllers;
using Application;

using Domain;
using Microsoft.AspNetCore.Http.HttpResults;

[TestFixture]
// still incomplete: Search LastName, UpdateMitarbeiter..
public class MitarbeiterControllerTests

{
    private MitarbeiterController _controller;
    private IMitarbeiterService _service;

    [SetUp]
    public void Setup()
    {
        _service = Substitute.For<IMitarbeiterService>();
        _controller = new MitarbeiterController(_service);

    }

    [Test]
    public async Task GetAll_WhenServiceReturnsData_ReturnsOk()

    {   // Arrange
        var testData = new List<Mitarbeiter>
        {
            new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true),
            new Mitarbeiter(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        _service.GetAllMitarbeiter().Returns(testData);

        // Act
        var result = await _controller.GetAll();

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());

        var okResult = result.Result as OkObjectResult;
        var returnedData = okResult?.Value as IEnumerable<Mitarbeiter>;
        Assert.That(returnedData?.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAll_WhenServiceReturnsEmptyList_ReturnsNotFound()
    {
        // Arrange
        var emptyData = new List<Mitarbeiter>();
        _service.GetAllMitarbeiter().Returns(emptyData);

        // Act
        var result = await _controller.GetAll();

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult?.Value, Is.EqualTo("Keine Mitarbeiter in der Liste."));
    }

    [Test]
    public async Task GetById_ExistingId_ReturnsMitarbeiter()
    {
        // Arrange
        var testmitarbeiter = new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true);
        _service.GetMitarbeiterById(1).Returns(testmitarbeiter);

        // Act
        var result = await _controller.GetMitarbeiter(1);

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var returnedMitarbeiter = okResult?.Value as Mitarbeiter;
        Assert.That(returnedMitarbeiter?.id, Is.EqualTo(1));
        Assert.That(returnedMitarbeiter?.FirstName, Is.EqualTo("Max"));
        Assert.That(returnedMitarbeiter?.LastName, Is.EqualTo("Mustermann"));
        Assert.That(returnedMitarbeiter?.BirthDate, Is.EqualTo("1985-01-15"));
        Assert.That(returnedMitarbeiter?.IsActive, Is.True);
    }
    [Test]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _service.GetMitarbeiterById(99).Returns((Mitarbeiter?)null);

        // Act
        var result = await _controller.GetMitarbeiter(99);

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult?.Value, Is.EqualTo("Mitarbeiter mit der ID = 99 nicht existent."));
    }

    [Test]
    public async Task GetById_InvalidId_ReturnsBadRequest()
    {
        // Act - verwende ungültige ID (<=0)
        var result = await _controller.GetMitarbeiter(-1);

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult?.Value, Is.EqualTo("Unzulässige ID"));
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
        Assert.That(badRequestResult?.Value, Is.EqualTo("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'."));
        var badRequestResult2 = result2.Result as BadRequestObjectResult;
        Assert.That(badRequestResult2?.Value, Is.EqualTo("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'."));
        var badRequestResult3 = result3.Result as BadRequestObjectResult;
        Assert.That(badRequestResult3?.Value, Is.EqualTo("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'."));
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

        _service.SearchMitarbeiter("1985-01-16").Returns(new List<Mitarbeiter> { testData[0] });

        // Act
        var result = await _controller.GetByDate("1985-01-16");

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());

        var okResult = result.Result as OkObjectResult;
        var returnedData = okResult?.Value as IEnumerable<Mitarbeiter>;
        Assert.That(returnedData?.Count(), Is.EqualTo(1));
        Assert.That(returnedData?.First().FirstName, Is.EqualTo("Max"));
        Assert.That(returnedData?.First().LastName, Is.EqualTo("Mustermann"));
        Assert.That(returnedData?.First().BirthDate, Is.EqualTo("1985-01-15"));
        Assert.That(returnedData?.First().IsActive, Is.True);
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

        _service.SearchMitarbeiter("1985-01-15").Returns(new List<Mitarbeiter>());

        // Act
        var result = await _controller.GetByDate("1985-01-15");

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult?.Value, Is.EqualTo("Kein Mitarbeiter mit früherem Geburtsdatum als 1985-01-15 gefunden."));
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
        Assert.That(createdAtActionResult?.Value, Is.EqualTo(newOne));
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
        Assert.That(badRequestResult?.Value, Is.EqualTo("Mitarbeiterdaten sind korrumpiert oder leer."));
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
        Assert.That(badRequestResult?.Value, Is.EqualTo("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich."));
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
        Assert.That(badRequestResult?.Value, Is.EqualTo("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich."));
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
        Assert.That(badRequestResult?.Value, Is.EqualTo("Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben!"));
    }

    [Test]
    public async Task CreateMitarbeiter_EmptyFirstName_ReturnsBadRequest()
    {
        // Arrange
        var newOneEmptyFirstName = new Mitarbeiter(3, "", "Doe", "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("Ein Vorname und ein Nachname sind erforderlich.");
        _service.CreateMitarbeiter(newOneEmptyFirstName).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneEmptyFirstName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult?.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
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
        Assert.That(badRequestResult?.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
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
        Assert.That(badRequestResult?.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
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
        Assert.That(badRequestResult?.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
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
        Assert.That(badRequestResult?.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }
    
    [Test]
    public async Task CreateMitarbeiter_NullLastName_ReturnsBadRequest()
    {
        // Arrange
        var newOneNullLastName = new Mitarbeiter(3, "John", null!, "1978-11-22", false);
        var failureResult = OperationResult.FailureResult("Ein Vorname und ein Nachname sind erforderlich.");
        _service.CreateMitarbeiter(newOneNullLastName).Returns(failureResult);

        // Act
        var result = await _controller.CreateMitarbeiter(newOneNullLastName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult?.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
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
        Assert.That(badRequestResult?.Value, Is.EqualTo("Ein Mitarbeiter mit dem gleichen Vornamen, Nachnamen und Geburtsdatum existiert bereits."));
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
        Assert.That(notFoundResult?.Value, Is.EqualTo($"Mitarbeiter konnte nicht gelöscht werden, da diese Id = {validId} nicht existiert."));
    }
    
    [Test]
    public async Task DeleteMitarbeiter_invalidId_ReturnsBadRequest()
    {
        // Arrange
        int invalidId = -99;
        var failureResult = OperationResult.FailureResult("Unzulässige ID");
        _service.DeleteMitarbeiter(invalidId).Returns(failureResult);

        // Act
        var result = await _controller.DeleteMitarbeiter(invalidId);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult?.Value, Is.EqualTo($"Unzulässige ID"));
    }

    [Test]
    public async Task DeleteMitarbeiter_validId_ReturnsContent()
    {
        // Arrange
        int validId = 1;
        var successResult = OperationResult.SuccessResult();
        _service.DeleteMitarbeiter(validId).Returns(successResult);

        // Act
        var result = await _controller.DeleteMitarbeiter(validId);

        // Assert
        Assert.That(result, Is.TypeOf<ContentResult>());
        var contentResult = result as ContentResult;
        Assert.That(contentResult?.Content, Is.EqualTo($"Mitarbeiter mit der ID " + validId + " wurde deaktiviert bzw. gelöscht."));
    }

}


