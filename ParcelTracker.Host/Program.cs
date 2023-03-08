namespace ParcelTracker.Host;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Azure.Core;
using Azure.Identity;

internal class Program
{
    private static string? env(HostBuilderContext context,string name) => context.Configuration.GetValue<string>(name);

    static TokenCredential GetStorageCredential(HostBuilderContext context)
        => new ClientSecretCredential(
            tenantId: env(context, "ORLEANS_TENANT_ID"),
            clientId: env(context,"ORLEANS_GRAIN_STORAGE_CLIENT_ID"),
            clientSecret: env(context,"ORLEANS_GRAIN_STORAGE_CLIENT_SECRET"));

    static (string ServiceUrl, string ContainerName) GetStorage(HostBuilderContext context)
        => (env(context, "SERVICE_URI") ?? string.Empty, "grainstate");

    static async Task Main(string[] args)
    {
        Console.Title = "Host";

        using var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(ConfigureApp)
            .UseOrleans((hostBuilderContext, sb) => sb
                .UseLocalhostClustering()
                .UseInMemoryReminderService()
                .AddAzureBlobGrainStorage("blobGrainStorage", o =>
                {
                    var (serviceUrl, containerName) = GetStorage(hostBuilderContext);

                    o.ConfigureBlobServiceClient(
                        serviceUri: new(serviceUrl),
                        tokenCredential: GetStorageCredential(hostBuilderContext));
                    o.ContainerName = containerName;
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

        static void CheckConfiguration(IConfiguration configuration)
        {
            void EnsureConfig(string varName)
            {
                if (string.IsNullOrEmpty(configuration.GetValue<string>(varName)))
                {
                    throw new ArgumentNullException(paramName: varName, message: $"Missing configuration {varName}");
                }
            }
            foreach (var requiredConfig in new[] { "SERVICE_URI", "ORLEANS_TENANT_ID", "ORLEANS_GRAIN_STORAGE_CLIENT_ID", "ORLEANS_GRAIN_STORAGE_CLIENT_SECRET" })
            {
                EnsureConfig(requiredConfig);
            }
        }
    }
}