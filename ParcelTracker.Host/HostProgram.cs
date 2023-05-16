namespace ParcelTracker.Host;

using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using GrainInterfaces;
using System;
using System.Threading.Tasks;
using ParcelTracker.GrainImplementations.ServiceImplementations;

internal class OrleansGrainStorageSettings
{
    public string? ServiceURI { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

internal class ParcelTrackerSettings
{
    public OrleansGrainStorageSettings? OrleansGrainStorage { get; set; }
}

internal class HostProgram
{
    internal record DefaultProviderConfig(Dictionary<string, ProviderConfiguration> Providers);

    static async Task<DefaultProviderConfig?> LoadFromLocalConfig(string path = "providers.json")
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var json = await File.ReadAllTextAsync(path);

        return JsonConvert.DeserializeObject<DefaultProviderConfig>(json);
    }

    static async Task BootstrapProviders(bool loadInitialConfiguration = false)
    {
        using var clientHost = Host
            .CreateDefaultBuilder()
            .UseOrleansClient(cb => cb.UseLocalhostClustering())
            .Build();
        await clientHost.StartAsync();

        var clusterClient = clientHost.Services.GetRequiredService<IClusterClient>();

        var providerBootstrap = clusterClient.GetGrain<IProviderBootstrapGrain>(primaryKey: "bootstrapSingleton");
        await providerBootstrap.ActivateAllProviders();

        if (loadInitialConfiguration)
        {
            var cfg = await LoadFromLocalConfig();
            if (cfg != default)
            {
                foreach (var (_, provider) in cfg.Providers)
                {
                    await providerBootstrap.AddAndActivateProvider(provider);
                }
            }
        }
    }

    private static readonly string ParcelTrackerConfigSectionName = "ParcelTracker";
    static async Task Main(string[] args)
    {
        Console.Title = "Host";

        using var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
            {
                configurationBuilder
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
            })
            .UseOrleans((HostBuilderContext hostBuilderContext, ISiloBuilder sb) => sb
                .UseLocalhostClustering()
                .UseInMemoryReminderService()
                .AddAzureBlobGrainStorage(ParcelTrackerConstants.GrainStorageName, o =>
                {
                    var ogs = hostBuilderContext.Configuration
                        .GetSection(ParcelTrackerConfigSectionName)
                        .Get<ParcelTrackerSettings>()!
                        .OrleansGrainStorage!;

                    o.ConfigureBlobServiceClient(
                        serviceUri: new(ogs.ServiceURI!),
                        tokenCredential: new ClientSecretCredential(
                            tenantId: ogs.TenantId,
                            clientId: ogs.ClientId,
                            clientSecret: ogs.ClientSecret));
                    o.ContainerName = "grainstate";
                })
            )
            .Build();

        await host.StartAsync();

        await BootstrapProviders(loadInitialConfiguration: true);

        await Console.Out.WriteLineAsync("Started");
        _ = await Console.In.ReadLineAsync();
        await host.StopAsync();
    }
}