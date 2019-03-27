using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Dispatch;
using XchangeCrypt.Backend.WalletService.Processors;
using XchangeCrypt.Backend.WalletService.Processors.Command;
using XchangeCrypt.Backend.WalletService.Providers.ETH;
using XchangeCrypt.Backend.WalletService.Services;
using XchangeCrypt.Backend.WalletService.Services.Hosted;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace XchangeCrypt.Backend.WalletService
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
            services.AddAuthentication(AzureADB2CDefaults.BearerAuthenticationScheme)
                .AddAzureADB2CBearer(options => Configuration.Bind("AzureAdB2C", options));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Custom repositories
            services.AddTransient<WalletRepository>();
            services.AddTransient<EventHistoryRepository>();

            // Custom services
            services.AddTransient<WalletOperationService>();
            services.AddTransient<EventHistoryService>();
            services.AddTransient<RandomEntropyService>();

            // Custom singleton services
            services.AddSingleton<DataAccess>();
            services.AddSingleton<VersionControl>();

            // Command processors are made ad-hoc via factory
            services.AddTransient<ProcessorFactory>();

            // Wallet providers
            services.AddSingleton<EthereumProvider>();
            services.AddSingleton<IHostedService, EthereumProvider>(
                serviceProvider => serviceProvider.GetService<EthereumProvider>()
            );

            services.AddSingleton<BitcoinProvider>();
            services.AddSingleton<IHostedService, BitcoinProvider>(
                serviceProvider => serviceProvider.GetService<BitcoinProvider>()
            );

            services.AddSingleton<LitecoinProvider>();
            services.AddSingleton<IHostedService, LitecoinProvider>(
                serviceProvider => serviceProvider.GetService<LitecoinProvider>()
            );

            // Persistently running queue message handler
            services.AddSingleton<DispatchReceiver>();
            services.AddSingleton<IHostedService, DispatchReceiver>(
                serviceProvider => serviceProvider.GetService<DispatchReceiver>()
            );

            // Dispatch
            services.AddTransient<WalletOperationDispatch>();

            // Wallet provider-related event listener

            // Persistently running queue message handler
            services.AddSingleton<WalletEventListener>();
            services.AddSingleton<IHostedService, WalletEventListener>(
                serviceProvider => serviceProvider.GetService<WalletEventListener>()
            );
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
