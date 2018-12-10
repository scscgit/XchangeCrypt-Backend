using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace XchangeCrypt.Backend.TradingBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        private static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
//                .ConfigureLogging(logging =>
//                {
//                    logging.ClearProviders();
//                    logging.AddConsole();
//                })
                .Build();
    }
}
