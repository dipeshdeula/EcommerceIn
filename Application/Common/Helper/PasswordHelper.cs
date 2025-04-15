using System.Security.Cryptography;
using System.Text;

namespace Application.Common.Helper
{
    public class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);

        }

        public static bool verifyPassword(string enteredPassword, string storedHashed)
        { 
            var hasOfInput = HashPassword(enteredPassword);
            return string.Equals(hasOfInput, storedHashed);
        }
    }
}
