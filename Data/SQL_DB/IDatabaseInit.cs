namespace Data.SQL_DB;

    public interface IDatabaseInitializer
    {
        Task<bool> InitializeDatabase();
        string GetApplicationConnectionString();
        string GetBootstrapConnectionString();
    }