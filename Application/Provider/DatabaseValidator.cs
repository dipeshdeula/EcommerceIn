using Npgsql;

namespace Application.Provider
{
    public class DatabaseValidator
    {
        public static bool IsConnectionValid(string host, string database, string user, string password, int port = 5432)
        {
            string connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    connection.Close();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
