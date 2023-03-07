namespace ParcelTracker.Host;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Azure.Core;
using Azure.Identity;
using Orleans.Hosting;

internal class Program
{
    private static string? env(string name) => Environment.GetEnvironmentVariable(name);

    static TokenCredential GetStorageCredential()
        => new ClientSecretCredential(
            tenantId: env("ORLEANS_TENANT_ID"),
            clientId: env("ORLEANS_GRAIN_STORAGE_CLIENT_ID"),
            clientSecret: env("ORLEANS_GRAIN_STORAGE_CLIENT_SECRET"));

    static (string ServiceUrl, string ContainerName) GetStorage()
        => ("https://chgpes1.blob.core.windows.net/", "grainstate");

    static async Task Main(string[] args)
    {
        Console.Title = "Host";

        var storage = GetStorage();

        using var host = Host
            .CreateDefaultBuilder(args)
            .UseOrleans(sb => sb
                .UseLocalhostClustering()
                .UseInMemoryReminderService()
                .AddAzureBlobGrainStorage("blobGrainStorage", o =>
                {
                    o.ConfigureBlobServiceClient(serviceUri: new(storage.ServiceUrl), tokenCredential: GetStorageCredential());
                    o.ContainerName = storage.ContainerName;
                })
            )
            .Build();

        await host.StartAsync();

        await Console.Out.WriteLineAsync("Started");
        _ = await Console.In.ReadLineAsync();
        await host.StopAsync();
    }
}