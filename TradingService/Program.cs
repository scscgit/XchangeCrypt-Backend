using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace XchangeCrypt.Backend.TradingService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

//                .ConfigureLogging(logging =>
//                {
//                    logging.ClearProviders();
//                    logging.AddConsole();
//                })
    }
}
