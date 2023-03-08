using AzureServerlessPDFConverter;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AzureFileConverter.Startup))]
namespace AzureFileConverter;

internal class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddOptions<GraphApiAuthOptions>().Configure<IConfiguration>((setttings, configuration) => 
        {
            configuration.GetSection("graph")
                         .Bind(setttings);
        });
        builder.Services.AddOptions<SiteOptions>().Configure<IConfiguration>((setttings, configuration) =>
        {
            configuration.GetSection("pdf")
                         .Bind(setttings);
        });
        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = null;
        });
        builder.Services.AddTransient<GraphItemsHandler>();
    }
}
