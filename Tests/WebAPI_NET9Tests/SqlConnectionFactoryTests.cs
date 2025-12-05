namespace WebAPI_NET9Tests;
using NSubstitute;
using NUnit.Framework;
using Data.SQL_DB;
using MySql.Data.MySqlClient;
using System;
using Microsoft.Extensions.Logging;

[TestFixture]
public class SqlConnectionFactoryTests
{
    private IDatabaseInitializer _databaseInitializer; // NSubstitute Mock
    private ILogger<SqlConnectionFactory> _logger; // Logger Mock
    private SqlConnectionFactory? _sqlConnectionFactory; // Nullable: erst in Tests initialisiert
    private const string TestConnectionString = "Server=localhost;Database=Mitarbeiter;Uid=testuser;Pwd=";

    [SetUp]
    public void Setup()
    {
        // NSubstitute Mock erstellen - OHNE readonly möglich
        _databaseInitializer = Substitute.For<IDatabaseInitializer>();
        _logger = Substitute.For<ILogger<SqlConnectionFactory>>();
        _databaseInitializer.GetApplicationConnectionString().Returns(TestConnectionString);
        _databaseInitializer.InitializeDatabase().Returns(true);
    }


    [Test]  // Constructor mit Null-Übergabe
    public void Constructor_NullDatabaseInitializer_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SqlConnectionFactory(null!, _logger)); // null! für Nullable-Kontext
        
        Assert.That(exception.ParamName, Is.EqualTo("databaseInitializer"));
    }

    [Test]  // GetConnectionString gibt korrekten Wert zurück
    public void GetConnectionString_ReturnsCorrectConnectionString()
    {
        // Arrange
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer, _logger);

        // Act
        var connectionString = _sqlConnectionFactory.GetConnectionString();

        // Assert
        Assert.That(connectionString, Is.EqualTo(TestConnectionString));
        _databaseInitializer.Received(1).GetApplicationConnectionString();
    }

    [Test] // CreateConnection gibt MySqlConnection zurück
    public async Task CreateConnection_ReturnsMySqlConnection()
    {
        // Arrange
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer, _logger);

        // Act
        using var connection = await _sqlConnectionFactory.CreateConnection();

        // Assert
        Assert.That(connection, Is.TypeOf<MySqlConnection>());
        
        // MySQL normalisiert Connection String - wichtige Eigenschaften prüfen
        Assert.That(connection.ConnectionString, Does.Contain("server=localhost"));
        Assert.That(connection.ConnectionString, Does.Contain("database=Mitarbeiter"));
        Assert.That(connection.ConnectionString, Does.Contain("user id=testuser"));
        _databaseInitializer.Received(1).GetApplicationConnectionString();
    }

    [Test] // CreateConnection ruft GetApplicationConnectionString auf
    public async Task CreateConnection_CallsGetApplicationConnectionString()
    {
        // Arrange
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer, _logger);

        // Act
        using var connection = await _sqlConnectionFactory.CreateConnection();

        // Assert
        _databaseInitializer.Received().GetApplicationConnectionString();
    }

    [Test]  // CreateConnection gibt bei mehreren Aufrufen unterschiedliche Instanzen zurück
    public async Task CreateConnection_MultipleCallsReturnDifferentInstances()
    {
        // Arrange
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer, _logger);

        // Act
        using var connection1 = await _sqlConnectionFactory.CreateConnection();
        using var connection2 = await _sqlConnectionFactory.CreateConnection();

        // Assert
        Assert.That(connection1, Is.Not.SameAs(connection2));
        Assert.That(connection1.ConnectionString, Is.EqualTo(connection2.ConnectionString));
    }
}