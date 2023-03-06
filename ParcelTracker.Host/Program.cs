namespace ParcelTracker.Host;

using System;
using System.Threading.Tasks;
using Orleans.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "Host";
        using IHost host = Host
            .CreateDefaultBuilder()
            .UseOrleans(siloBuilder => siloBuilder.UseLocalhostClustering())
            .ConfigureLogging(logging => logging.AddConsole())
            .Build();

        await host.StartAsync();

        await Console.Out.WriteLineAsync("Started");
        _ = await Console.In.ReadLineAsync();
        await host.StopAsync();
    }
}