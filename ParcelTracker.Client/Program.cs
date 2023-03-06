namespace ParcelTracker.Client;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using ParcelTracker.GrainInterfaces;

internal class Program
{
    static async Task Main()
    {
        Console.Title = "Client";

        Console.ReadLine();

        using var host = Host.CreateDefaultBuilder()
            .UseOrleansClient(cb => cb.UseLocalhostClustering())
            .Build();
        await host.StartAsync();

        var client = host.Services.GetRequiredService<IClusterClient>();

        try
        {
            while (true)
            {
                var line = await Console.In.ReadLineAsync();
                if (line == null)
                {
                    continue;
                }

                var segments = line.Split(' ');
                if (segments.Length > 3 && segments[0] == "add" && int.TryParse(segments[2], out var prio))
                {
                    var provider = segments[1];
                    var prioritizedQueue = client.GetGrain<IPrioritizedQueue<string>>(primaryKey: provider);


                    // add DHL 1 Hello world
                    var job = string.Join(" ", segments.Skip(3));

                    await Console.Out.WriteLineAsync($"Adding Prio: {prio}, Job {job}");
                    await prioritizedQueue.AddJob(priority: prio, job: job);
                }
                else if (segments.Length == 2 && segments[0] == "get")
                {
                    var provider = segments[1];
                    var prioritizedQueue = client.GetGrain<IPrioritizedQueue<string>>(primaryKey: provider);

                    (int, string)? result = await prioritizedQueue.GetJob();
                    if (result == null)
                    {
                        await Console.Error.WriteLineAsync($"No job available");
                    }
                    else
                    {
                        await Console.Out.WriteLineAsync($"Prio: {result.Value.Item1}, Job {result.Value.Item2}");
                    }
                }
                else
                {
                    await Console.Error.WriteLineAsync($"Could not parse \"{line}\"");
                }
            }
        }
        finally
        {
            await host.StopAsync();
        }
    }
}