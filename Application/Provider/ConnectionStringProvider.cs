namespace Application.Provider
{
    public class ConnectionStringProvider
    {
        public static string GetConnectionString(string dataSource, string initialCatalog, string userId, string password)
        {
            return $"Host={dataSource};Database={initialCatalog};Username={userId};Password={password};Port=5432;";

        }
    }
}

