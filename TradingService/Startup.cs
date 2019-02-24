using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.TradingService.Dispatch;
using XchangeCrypt.Backend.TradingService.Processors;
using XchangeCrypt.Backend.TradingService.Processors.Event;
using XchangeCrypt.Backend.TradingService.Services;
using XchangeCrypt.Backend.TradingService.Services.Hosted;
using XchangeCrypt.Backend.TradingService.Services.Meta;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace XchangeCrypt.Backend.TradingService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Persistently running database handler for executing events
            services.AddSingleton<DatabaseGenerator>();
            services.AddSingleton<IHostedService, DatabaseGenerator>(
                serviceProvider => serviceProvider.GetService<DatabaseGenerator>()
            );

            // Persistently running queue message handler
            services.AddSingleton<DispatchReceiver>();
            services.AddSingleton<IHostedService, DispatchReceiver>(
                serviceProvider => serviceProvider.GetService<DispatchReceiver>()
            );

            // Dispatch
            services.AddTransient<TradeOrderDispatch>();

            // Meta-faculties
            services.AddSingleton<MonitorService>();

            // Command processors are made ad-hoc via factory
            services.AddTransient<ProcessorFactory>();

            // Event processors
            services.AddTransient<TradeEventProcessor>();

            // Custom services
            services.AddTransient<EventHistoryService>();
            services.AddTransient<TradingOrderService>();

            // Custom repositories
            services.AddTransient<AccountRepository>();
            services.AddTransient<EventHistoryRepository>();
            services.AddTransient<TradingRepository>();

            // Custom singleton services
            services.AddSingleton<DataAccess>();
            services.AddSingleton<VersionControl>();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // Enabling dot as a decimal separator symbol
            var cultureInfo = new CultureInfo("en-US")
            {
                NumberFormat = {CurrencySymbol = "€"}
            };
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc()
                // index.html for redirect
                .UseDefaultFiles()
                .UseStaticFiles();
        }
    }
}
