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
    private SqlConnectionFactory? _sqlConnectionFactory; // Nullable: initialized in tests only
    private const string TestConnectionString = "Server=localhost;Database=Employees;Uid=root;Pwd=;Port=3306"; // From configuration

    [SetUp]
    public void Setup()
    {
        // Create NSubstitute Mock - possible WITHOUT readonly
        _databaseInitializer = Substitute.For<IDatabaseInitializer>();
        _logger = Substitute.For<ILogger<SqlConnectionFactory>>();
        _databaseInitializer.GetApplicationConnectionString().Returns(TestConnectionString);
        _databaseInitializer.GetBootstrapConnectionString().Returns("Server=localhost;Uid=root;Pwd=;Port=3306");
        _databaseInitializer.InitializeDatabase().Returns(true);
    }


    [Test]  // Constructor with null parameter
    public void Constructor_NullDatabaseInitializer_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SqlConnectionFactory(null!, _logger)); // null! for nullable context
        
        Assert.That(exception.ParamName, Is.EqualTo("databaseInitializer"));
    }

    [Test]  // GetConnectionString returns correct value
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

    [Test] // CreateConnection returns MySqlConnection (Mock-based test)
    public async Task CreateConnection_ReturnsMySqlConnection()
    {
        // Arrange - Mock successful initialization to avoid DB connection
        _databaseInitializer.InitializeDatabase().Returns(Task.FromResult(true));
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer, _logger);

        // Act - This will trigger initialization since _isInitialized starts as false
        using var connection = await _sqlConnectionFactory.CreateConnection();

        // Assert
        Assert.That(connection, Is.TypeOf<MySqlConnection>());
        
        // Verify initialization was called since this is first call
        await _databaseInitializer.Received(1).InitializeDatabase();
        _databaseInitializer.Received().GetApplicationConnectionString();
    }

    [Test] // CreateConnection calls GetApplicationConnectionString
    public async Task CreateConnection_CallsGetApplicationConnectionString()
    {
        // Arrange
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer, _logger);

        // Act
        using var connection = await _sqlConnectionFactory.CreateConnection();

        // Assert
        _databaseInitializer.Received().GetApplicationConnectionString();
    }

    [Test]  // CreateConnection returns different instances on multiple calls
    public async Task CreateConnection_MultipleCallsReturnDifferentInstances()
    {
        // Arrange - Mock successful initialization
        _databaseInitializer.InitializeDatabase().Returns(Task.FromResult(true));
        _sqlConnectionFactory = new SqlConnectionFactory(_databaseInitializer, _logger);

        // Act - First call will initialize, second will use fast path
        using var connection1 = await _sqlConnectionFactory.CreateConnection();
        using var connection2 = await _sqlConnectionFactory.CreateConnection();

        // Assert
        Assert.That(connection1, Is.Not.SameAs(connection2));
        Assert.That(connection1.ConnectionString, Is.EqualTo(connection2.ConnectionString));
        
        // Verify initialization was called only once
        await _databaseInitializer.Received(1).InitializeDatabase();
    }
}