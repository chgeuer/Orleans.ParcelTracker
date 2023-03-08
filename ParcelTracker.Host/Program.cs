namespace ParcelTracker.Host;

using Azure.Identity;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;


internal class Program
{

    static async Task Main(string[] args)
    {
        Console.Title = "Host";
       
        using var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(ConfigureApp)            
            .UseOrleans( (context, sb) => sb               
                .UseLocalhostClustering()
                .AddAzureBlobGrainStorage("blobGrainStorage", o =>
                {
                    o.ConfigureBlobServiceClient(
                        serviceUri: new(context.Configuration.GetValue<string>("SERVICE_URI") ?? string.Empty),
                        tokenCredential: new ClientSecretCredential(
                            tenantId: context.Configuration.GetValue<string>("ORLEANS_TENANT_ID"),
                            clientId: context.Configuration.GetValue<string>("ORLEANS_GRAIN_STORAGE_CLIENT_ID"),
                            clientSecret: context.Configuration.GetValue<string>("ORLEANS_GRAIN_STORAGE_CLIENT_SECRET")));
                    o.ContainerName = "grainstate";
                })
            )
            .Build();

        await host.StartAsync();

        await Console.Out.WriteLineAsync("Started");
        _ = await Console.In.ReadLineAsync();
        await host.StopAsync();
    }

    private static void ConfigureApp(HostBuilderContext hostBuilderContext, IConfigurationBuilder configurationBuilder)
    {
        // try and load env params from appsettings.json
        configurationBuilder.AddJsonFile("appsettings.json");
        CheckConfiguration(configurationBuilder.Build());
    }

    private static void CheckConfiguration(IConfiguration configuration)
    {
        // confirm all environment variables are set
        if( string.IsNullOrEmpty(configuration.GetValue<string>("ORLEANS_TENANT_ID")) ||
            string.IsNullOrEmpty(configuration.GetValue<string>("ORLEANS_GRAIN_STORAGE_CLIENT_ID")) ||
            string.IsNullOrEmpty(configuration.GetValue<string>("ORLEANS_GRAIN_STORAGE_CLIENT_SECRET")) ||
            string.IsNullOrEmpty(configuration.GetValue<string>("SERVICE_URI")))
            {
                throw new ArgumentNullException("Missing Mandatory Environment Variables");        
            }                   
    }
           
}