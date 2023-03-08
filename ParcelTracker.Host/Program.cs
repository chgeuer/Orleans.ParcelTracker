namespace ParcelTracker.Host;

using System;
using Azure.Core;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;



internal class Program
{
    private static string? env(HostBuilderContext context,string name) => context.Configuration.GetValue<string>(name);

    static TokenCredential GetStorageCredential(HostBuilderContext context)
        => new ClientSecretCredential(
            tenantId: env(context, "ORLEANS_TENANT_ID"),
            clientId: env(context,"ORLEANS_GRAIN_STORAGE_CLIENT_ID"),
            clientSecret: env(context,"ORLEANS_GRAIN_STORAGE_CLIENT_SECRET"));

    static (string ServiceUrl, string ContainerName) GetStorage(HostBuilderContext context)
        => (env(context,"SERVICE_URI")?? string.Empty, "grainstate");


    static async Task Main(string[] args)
    {
        Console.Title = "Host";
       
        // var storage = GetStorage();

        var storage = GetStorage();

        using var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(ConfigureApp)     
            .UseOrleans((context, sb) => sb
                .UseLocalhostClustering()
                .UseInMemoryReminderService()
                .AddAzureBlobGrainStorage("blobGrainStorage", o =>
                {
                    o.ConfigureBlobServiceClient(serviceUri: new(GetStorage(context).ServiceUrl), tokenCredential: GetStorageCredential(context));
                    o.ContainerName = GetStorage(context).ContainerName;

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
        configurationBuilder.AddJsonFile("appsettings_local.json");
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