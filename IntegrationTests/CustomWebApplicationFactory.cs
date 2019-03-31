using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.ConvergenceService;
using XchangeCrypt.Backend.ConvergenceService.Services;

namespace IntegrationTests
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<Startup>
    {
        // Logger is null even though we set it in the scope :(
        public ILogger Logger { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Create a new service provider.
//                var serviceProvider = new ServiceCollection()
//                    .AddEntityFrameworkInMemoryDatabase()
//                    .BuildServiceProvider();

                // Add a database context (AppDbContext) using an in-memory database for testing.
//                services.AddDbContext<AppDbContext>(options =>
//                {
//                    options.UseInMemoryDatabase("InMemoryAppDb");
//                    options.UseInternalServiceProvider(serviceProvider);
//                });

                // Build the service provider.
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database contexts
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    //var appDb = scopedServices.GetRequiredService<AppDbContext>();

                    Logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                    try
                    {
                        // Seed the database with some specific test data.
                        //SeedData.PopulateTestData(appDb);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "An error occurred seeding the " +
                                            "database with test messages. Error: {ex.Message}");
                    }
                }
            });
        }
    }
}
