using DotNetEnv;
using System.IO;

namespace Application.Provider
{
    public static class EnvProvider
    {
        public static void LoadEnv(string? envPath = null)
        {
            // Always load from the solution root .env file
            envPath ??= Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath); // Use the basic overload
            }
        }
    }
}