using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace XchangeCrypt.Backend.ConvergenceBackend
{
    /// <summary>
    /// Program
    /// </summary>
    public class Program
    {
        /// <summary>
        /// </summary>
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        /// <summary>
        /// </summary>
        private static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()
                .UseStartup<Startup>()
                .Build();
    }
}
