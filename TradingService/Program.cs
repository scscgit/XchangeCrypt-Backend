using System.Threading;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace XchangeCrypt.Backend.TradingService
{
    public static class Program
    {
        private static readonly CancellationTokenSource CancelTokenSource = new CancellationTokenSource();

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().RunAsync(CancelTokenSource.Token).Wait();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

        public static void Shutdown()
        {
            CancelTokenSource.Cancel();
        }
    }
}
