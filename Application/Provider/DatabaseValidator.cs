using Microsoft.Data.SqlClient;

namespace Application.Provider
{
    public class DatabaseValidator
    {
        public static bool IsConnectionValid(string dataSource, string userId, string password)
        {
            // Create a connection string without initial catalog
            string connectionString = $"Data Source={dataSource};User Id={userId};Password={password};Encrypt=false;Connection Timeout=10;";
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    connection.Close();
                    return true;
                }
            }
            catch (SqlException)
            {
                return false;
            }
        }
    }
}

