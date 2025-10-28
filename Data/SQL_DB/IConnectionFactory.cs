namespace Data.SQL_DB; 
using MySql.Data.MySqlClient;

public interface IConnectionFactory
{
    Task<MySqlConnection> CreateConnection();
}