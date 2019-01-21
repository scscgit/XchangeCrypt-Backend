using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace XchangeCrypt.Backend.ConvergenceService
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
            CreateWebHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// </summary>
        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()
                .UseStartup<Startup>();
    }
}
