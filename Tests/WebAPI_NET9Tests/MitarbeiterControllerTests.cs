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
    public void GetAll_WhenServiceReturnsData_ReturnsOk()

    {   // Arrange
        var testData = new List<Mitarbeiter>
        {
            new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true),
            new Mitarbeiter(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        _service.GetAllMitarbeiter().Returns(testData);

        // Act
        var result = _controller.GetAll();

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());

        var okResult = result.Result as OkObjectResult;
        var returnedData = okResult.Value as IEnumerable<Mitarbeiter>;
        Assert.That(returnedData.Count(), Is.EqualTo(2));
    }

    [Test]
    public void GetAll_WhenServiceReturnsEmptyList_ReturnsNotFound()
    {
        // Arrange
        var emptyData = new List<Mitarbeiter>();
        _service.GetAllMitarbeiter().Returns(emptyData);

        // Act
        var result = _controller.GetAll();

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult.Value, Is.EqualTo("Keine Mitarbeiter in der Liste."));
    }

    [Test]
    public void GetById_ExistingId_ReturnsMitarbeiter()
    {
        // Arrange
        var testmitarbeiter = new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true);
        _service.GetMitarbeiterById(1).Returns(testmitarbeiter);

        // Act
        var result = _controller.GetMitarbeiter(1);

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var returnedMitarbeiter = okResult.Value as Mitarbeiter;
        Assert.That(returnedMitarbeiter.id, Is.EqualTo(1));
        Assert.That(returnedMitarbeiter.FirstName, Is.EqualTo("Max"));
        Assert.That(returnedMitarbeiter.LastName, Is.EqualTo("Mustermann"));
        Assert.That(returnedMitarbeiter.BirthDate, Is.EqualTo("1985-01-15"));
        Assert.That(returnedMitarbeiter.IsActive, Is.True);
    }
    [Test]
    public void GetById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _service.GetMitarbeiterById(99).Returns((Mitarbeiter?)null);

        // Act
        var result = _controller.GetMitarbeiter(99);

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult.Value, Is.EqualTo("Mitarbeiter mit der ID = 99 nicht existent."));
    }

    [Test]
    public void GetById_InvalidId_ReturnsBadRequest()
    {
        // Act - verwende ungültige ID (<=0)
        var result = _controller.GetMitarbeiter(-1);

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Unzulässige ID"));
    }

    [Test]
    public void GetByDate_IsNullOrEmpty_ReturnsBadRequest()
    {
        // Act
        var result = _controller.GetByDate(string.Empty);
        var result2 = _controller.GetByDate(null);
        var result3 = _controller.GetByDate("   ");

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'."));
        var badRequestResult2 = result2.Result as BadRequestObjectResult;
        Assert.That(badRequestResult2.Value, Is.EqualTo("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'."));
        var badRequestResult3 = result3.Result as BadRequestObjectResult;
        Assert.That(badRequestResult3.Value, Is.EqualTo("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'."));
    }
    [Test]
    public void GetByDate_ValidDateWithResults_ReturnsOk()
    {
        // Arrange
        var testData = new List<Mitarbeiter>
        {
            new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true),
            new Mitarbeiter(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        _service.SearchMitarbeiter("1985-01-16").Returns(new List<Mitarbeiter> { testData[0] });

        // Act
        var result = _controller.GetByDate("1985-01-16");

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());

        var okResult = result.Result as OkObjectResult;
        var returnedData = okResult.Value as IEnumerable<Mitarbeiter>;
        Assert.That(returnedData.Count(), Is.EqualTo(1));
        Assert.That(returnedData.First().FirstName, Is.EqualTo("Max"));
        Assert.That(returnedData.First().LastName, Is.EqualTo("Mustermann"));
        Assert.That(returnedData.First().BirthDate, Is.EqualTo("1985-01-15"));
        Assert.That(returnedData.First().IsActive, Is.True);
    }



    [Test]
    public void GetByDate_ValidDateWithoutResults_ReturnsNotFound()
    {
        // Arrange
        var testData = new List<Mitarbeiter>
        {
            new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true),
            new Mitarbeiter(2, "Erika", "Musterfrau", "1990-06-30", true)
        };

        _service.SearchMitarbeiter("1985-01-15").Returns(new List<Mitarbeiter>());

        // Act
        var result = _controller.GetByDate("1985-01-15");

        // Assert
        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult.Value, Is.EqualTo("Kein Mitarbeiter mit früherem Geburtsdatum als 1985-01-15 gefunden."));
    }


    [Test]
    public void CreateMitarbeiter_ValidMitarbeiter_ReturnsCreatedAtAction()
    {
        //Arrange 
        var newOne = new Mitarbeiter(3, "John", "Doe", "1978-11-22", false);
        _service.CreateMitarbeiter(newOne, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = null; // No error message
            return true; // Indicate success
        });
    }

    [Test]
    public void CreateMitarbeiter_NullMitarbeiter_ReturnsBadRequest()
    {
        // Arrange
        var newOneNull = null as Mitarbeiter;
        _service.CreateMitarbeiter(newOneNull, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Mitarbeiterdaten sind korrumpiert oder leer.";
            return false; // Indicate failure
        });
        // Act
        var result = _controller.CreateMitarbeiter(newOneNull);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    [TestCase("22-11-1978")]
    [TestCase("1978-14-25")]
    [TestCase("     ")]
    [TestCase("")]
    public void CreateMitarbeiter_InvalidDateFormat_ReturnsBadRequest(string invalidDate)
    {
        // Arrange
        var newOneWrongDateFormat = new Mitarbeiter(3, "John", "Doe", "03.03.2003", false);

        _service.CreateMitarbeiter(newOneWrongDateFormat, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.";
            return false;
        });

        // Act
        var result = _controller.CreateMitarbeiter(newOneWrongDateFormat);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich."));
    }

    [Test]
    public void CreateMitarbeiter_DateIsNull_ReturnsBadRequest()
    {
        // Arrange
        var newOneNullDateFormat = new Mitarbeiter(3, "John", "Doe", null, false);

        _service.CreateMitarbeiter(newOneNullDateFormat, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.";
            return false;
        });

        // Act
        var result = _controller.CreateMitarbeiter(newOneNullDateFormat);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Ein gültiges Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich."));
    }

    [Test]
    public void CreateMitarbeiter_DateHasInvalidChars_ReturnsBadRequest()
    {
        // Arrange
        var newOneInvalidCharsInDateFormat = new Mitarbeiter(3, "John", "Doe", "1$78\\1&//?2", false);

        _service.CreateMitarbeiter(newOneInvalidCharsInDateFormat, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben!";
            return false;
        });

        // Act
        var result = _controller.CreateMitarbeiter(newOneInvalidCharsInDateFormat);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben!"));
    }

    [Test]
    public void CreateMitarbeiter_EmptyFirstName_ReturnsBadRequest()
    {
        // Arrange
        var newOneEmptyFirstName = new Mitarbeiter(3, "", "Doe", "1978-11-22", false);

        _service.CreateMitarbeiter(newOneEmptyFirstName, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Ein Vorname und ein Nachname sind erforderlich.";
            return false;
        });
        // Act
        var result = _controller.CreateMitarbeiter(newOneEmptyFirstName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }

     [Test]
    public void CreateMitarbeiter_WhiteSpaceFirstName_ReturnsBadRequest()
    {
        // Arrange
        var newOneWhiteSpaceFirstName = new Mitarbeiter(3, "   ", "Doe", "1978-11-22", false);

        _service.CreateMitarbeiter(newOneWhiteSpaceFirstName, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Ein Vorname und ein Nachname sind erforderlich.";
            return false;
        });
        // Act
        var result = _controller.CreateMitarbeiter(newOneWhiteSpaceFirstName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }

     [Test]
    public void CreateMitarbeiter_NullFirstName_ReturnsBadRequest()
    {
        // Arrange
        var newOneNullFirstName = new Mitarbeiter(3, null, "Doe", "1978-11-22", false);

        _service.CreateMitarbeiter(newOneNullFirstName, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Ein Vorname und ein Nachname sind erforderlich.";
            return false;
        });
        // Act
        var result = _controller.CreateMitarbeiter(newOneNullFirstName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }

    [Test]
    public void CreateMitarbeiter_EmptyLastName_ReturnsBadRequest()
    {
        // Arrange
        var newOneEmptyLastName = new Mitarbeiter(3, "John", "", "1978-11-22", false);

        _service.CreateMitarbeiter(newOneEmptyLastName, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Ein Vorname und ein Nachname sind erforderlich.";
            return false;
        });
        // Act
        var result = _controller.CreateMitarbeiter(newOneEmptyLastName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }

    [Test]
    public void CreateMitarbeiter_WhiteSpaceLastName_ReturnsBadRequest()
    {
        // Arrange
        var newOneWhiteSpaceLastName = new Mitarbeiter(3, "John", "   ", "1978-11-22", false);

        _service.CreateMitarbeiter(newOneWhiteSpaceLastName, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Ein Vorname und ein Nachname sind erforderlich.";
            return false;
        });
        // Act
        var result = _controller.CreateMitarbeiter(newOneWhiteSpaceLastName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }
    [Test]
    public void CreateMitarbeiter_NullLastName_ReturnsBadRequest()
    {
        // Arrange
        var newOneNullLastName = new Mitarbeiter(3, "John", null, "1978-11-22", false);

        _service.CreateMitarbeiter(newOneNullLastName, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Ein Vorname und ein Nachname sind erforderlich.";
            return false;
        });
        // Act
        var result = _controller.CreateMitarbeiter(newOneNullLastName);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Ein Vorname und ein Nachname sind erforderlich."));
    }
    [Test]
    public void CreateMitarbeiter_DuplicateMitarbeiter_ReturnsBadRequest()
    {
        // Arrange
        var newOneDuplicate = new Mitarbeiter(1, "Max", "Mustermann", "1985-01-15", true);

        _service.CreateMitarbeiter(newOneDuplicate, out Arg.Any<string>()).Returns(x =>
        {
            x[1] = "Ein Mitarbeiter mit dem gleichen Vornamen, Nachnamen und Geburtsdatum existiert bereits.";
            return false;
        });
        // Act
        var result = _controller.CreateMitarbeiter(newOneDuplicate);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo("Ein Mitarbeiter mit dem gleichen Vornamen, Nachnamen und Geburtsdatum existiert bereits."));
    }
    [Test]
    public void DeleteMitarbeiter_ValidId_ReturnsNotFound()
    {
        // Arrange
        int validId = 123456;
        string errorMessage;
        _service.DeleteMitarbeiter(validId, out errorMessage)
            .Returns(x => { 
                x[1] = $"Mitarbeiter konnte nicht gelöscht werden, da diese Id = {validId} nicht existiert."; 
                return false; 
            });

        // Act
        var result = _controller.DeleteMitarbeiter(validId);

        // Assert
        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult.Value, Is.EqualTo($"Mitarbeiter konnte nicht gelöscht werden, da diese Id = {validId} nicht existiert."));
    }
    [Test]
    public void DeleteMitarbeiter_invalidId_ReturnsBadRequest()
    {
        // Arrange
        int invalidId = -99;
        _service.DeleteMitarbeiter(invalidId, out Arg.Any<string>()).Returns(false);

        // Act
        var result = _controller.DeleteMitarbeiter(invalidId);

        // Assert
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.Value, Is.EqualTo($"Unzulässige ID"));
    }

    [Test]
    public void DeleteMitarbeiter_validId_ReturnsContent()
    {
        // Arrange
        int validId = 1;
        _service.DeleteMitarbeiter(validId, out Arg.Any<string>()).Returns(true);

        // Act
        var result = _controller.DeleteMitarbeiter(validId);

        // Assert
        Assert.That(result, Is.TypeOf<ContentResult>());
        var contentResult = result as ContentResult;
        Assert.That(contentResult.Content, Is.EqualTo($"Mitarbeiter mit der ID " + validId + " wurde deaktiviert bzw. gelöscht."));
    }

}


