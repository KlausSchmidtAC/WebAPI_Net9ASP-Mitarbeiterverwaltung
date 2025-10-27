namespace WebAPI_NET9Tests;
using NSubstitute;
using NUnit.Framework;
using Data.SQL_DB;
using MySql.Data.MySqlClient;
using System;

[TestFixture]
public class SqlConnectionFactoryTests
{
    private IDatabaseInitializer _databaseInitializer; // KEIN readonly - wird in SetUp neu gesetzt!
    private SqlConnectionFactory? _sqlConnectionFactory; // Nullable weil erst in Tests initialisiert
    private const string TestConnectionString = "Server=localhost;Database=Mitarbeiter;Uid=testuser;Pwd=;";

    [SetUp]
    public void Setup()
    {
        // NSubstitute Mock erstellen - OHNE readonly möglich
        _databaseInitializer = Substitute.For<IDatabaseInitializer>();
        
        // Standard-Mock-Verhalten konfigurieren
        _databaseInitializer.InitializeDatabase().Returns(true);
        _databaseInitializer.GetApplicationConnectionString().Returns(TestConnectionString);
    }

    [Test]
    public void Constructor_DatabaseInitializationSuccessful_CreatesInstance()
    {
        // Arrange
        _databaseInitializer.InitializeDatabase().Returns(true);

        // Act
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer);

        // Assert
        Assert.That(_sqlConnectionFactory, Is.Not.Null);
        _databaseInitializer.Received(1).InitializeDatabase();
    }

    [Test]
    public void Constructor_DatabaseInitializationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        _databaseInitializer.InitializeDatabase().Returns(false);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new SqlConnectionFactory(_databaseInitializer));
        
        Assert.That(exception.Message, Is.EqualTo("Database initialization failed in SqlConnectionFactory."));
        _databaseInitializer.Received(1).InitializeDatabase();
    }

    [Test]
    public void Constructor_NullDatabaseInitializer_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SqlConnectionFactory(null!)); // null! für Nullable-Kontext
        
        Assert.That(exception.ParamName, Is.EqualTo("databaseInitializer"));
    }

    [Test]
    public void GetConnectionString_ReturnsCorrectConnectionString()
    {
        // Arrange
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer);

        // Act
        var connectionString = _sqlConnectionFactory.GetConnectionString();

        // Assert
        Assert.That(connectionString, Is.EqualTo(TestConnectionString));
        _databaseInitializer.Received(1).GetApplicationConnectionString();
    }

    [Test]
    public void CreateConnection_ReturnsMySqlConnection()
    {
        // Arrange
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer);

        // Act
        using var connection = _sqlConnectionFactory.CreateConnection();

        // Assert
        Assert.That(connection, Is.TypeOf<MySqlConnection>());
        Assert.That(connection.ConnectionString, Is.EqualTo(TestConnectionString));
        _databaseInitializer.Received(1).GetApplicationConnectionString();
    }

    [Test]
    public void CreateConnection_CallsGetApplicationConnectionString()
    {
        // Arrange
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer);

        // Act
        using var connection = _sqlConnectionFactory.CreateConnection();

        // Assert
        _databaseInitializer.Received().GetApplicationConnectionString();
    }

    [Test]
    public void CreateConnection_MultipleCallsReturnDifferentInstances()
    {
        // Arrange
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer);

        // Act
        using var connection1 = _sqlConnectionFactory.CreateConnection();
        using var connection2 = _sqlConnectionFactory.CreateConnection();

        // Assert
        Assert.That(connection1, Is.Not.SameAs(connection2));
        Assert.That(connection1.ConnectionString, Is.EqualTo(connection2.ConnectionString));
    }
}