namespace Application.Provider
{
    public class ConnectionStringProvider
    {
        public static string GetConnectionString(string dataSource, string initialCatalog, string userId, string password)
        {
            return $"Data Source={dataSource};" +
                   $"Initial Catalog={initialCatalog};" +
                   $"User Id={userId};" +
                   $"Password={password};" +
                   "Encrypt=false;" +
                   "Connection Timeout=3600;";
        }
    }
}

