using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace XchangeCrypt.Backend.ConvergenceBackend.Extensions.Authentication
{
    public static class AzureAdB2CAuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAzureAdB2C(this AuthenticationBuilder builder)
            => builder.AddAzureAdB2C(_ =>
            {
            });

        public static AuthenticationBuilder AddAzureAdB2C(this AuthenticationBuilder builder, Action<AzureAdB2COptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, OpenIdConnectOptionsSetup>();
            builder.AddOpenIdConnect();
            return builder;
        }
    }
}
