namespace ParcelTracker.Host;

using Azure.Identity;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using System;
using System.Threading.Tasks;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "Host";

        using var host = Host
            .CreateDefaultBuilder(args)
            .UseOrleans(sb => sb
                .UseLocalhostClustering()
                .AddAzureBlobGrainStorage("blobGrainStorage", o =>
                {
                    o.ConfigureBlobServiceClient(
                        serviceUri: new("https://chgpes1.blob.core.windows.net/"),
                        tokenCredential: new ClientSecretCredential(
                            tenantId: Environment.GetEnvironmentVariable("ORLEANS_TENANT_ID"),
                            clientId: Environment.GetEnvironmentVariable("ORLEANS_GRAIN_STORAGE_CLIENT_ID"),
                            clientSecret: Environment.GetEnvironmentVariable("ORLEANS_GRAIN_STORAGE_CLIENT_SECRET")));
                    o.ContainerName = "grainstate";
                })
            )
            .Build();

        await host.StartAsync();

        await Console.Out.WriteLineAsync("Started");
        _ = await Console.In.ReadLineAsync();
        await host.StopAsync();
    }
}