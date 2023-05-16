namespace ParcelTracker.Host;

using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ParcelTracker.GrainInterfaces;
using System;
using System.Threading.Tasks;

internal class Program
{
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
            //.ConfigureServices((hostBuilderContext, services) =>
            //{
            //    // not working, but might help with the IOptions<ParcelTracker> DI in the grain.
            //    // services!.Configure<ParcelTrackerSettings>(hostBuilderContext.Configuration.GetSection("ParcelTracker"));
            //    // working
            //    //var parcelTracker = hostBuilderContext.Configuration.GetSection("ParcelTracker").Get<ParcelTracker>();
            //    //services.AddSingleton(parcelTracker!);
            //})
            .UseOrleans((HostBuilderContext hostBuilderContext, ISiloBuilder sb) => sb
                .UseLocalhostClustering()
                .UseInMemoryReminderService()
                .AddAzureBlobGrainStorage("blobGrainStorage", o =>
                {
                    var ogs = hostBuilderContext.Configuration.GetSection("ParcelTracker").Get<ParcelTrackerSettings>()!.OrleansGrainStorage!;

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

        static async Task BootstrapProviders()
        {
            using var clientHost = Host
                .CreateDefaultBuilder()
                .UseOrleansClient(cb => cb.UseLocalhostClustering())
                .Build();
            await clientHost.StartAsync();

            var clusterClient = clientHost.Services.GetRequiredService<IClusterClient>();

            var providerBootstrap = clusterClient.GetGrain<IProviderBootstrapGrain>(primaryKey: "bootstrapSingleton");
            await providerBootstrap.ActivateAllProviders();

            await providerBootstrap.AddAndActivateProvider(new(
                MaxConcurrency: 2, ProviderName: "DHL", ProviderURL: "https://localhost/DHL"));
        }

        await BootstrapProviders();

        await Console.Out.WriteLineAsync("Started");
        _ = await Console.In.ReadLineAsync();
        await host.StopAsync();
    }
}