namespace Data.SQL_DB;

    public interface IDatabaseInitializer
    {
        bool InitializeDatabase();
        string GetApplicationConnectionString();
    }
    
