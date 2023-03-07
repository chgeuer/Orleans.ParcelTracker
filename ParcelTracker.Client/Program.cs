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

        await Console.Out.WriteLineAsync("Press <Enter> to connect to Orleans cluster...");
        _ = await Console.In.ReadLineAsync();

        using var host = Host.CreateDefaultBuilder()
            .UseOrleansClient(cb => cb.UseLocalhostClustering())
            .Build();
        await host.StartAsync();

        var clusterClient = host.Services.GetRequiredService<IClusterClient>();

        Dictionary<string, IPrioritizedQueue<string>> clients = new();
        IPrioritizedQueue<string> getClient(string provider)
        {
            if (!clients.ContainsKey(provider))
            {
                clients[provider] = clusterClient.GetGrain<IPrioritizedQueue<string>>(primaryKey: provider);
            }
            return clients[provider];
        }

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
                    var prioritizedQueue = getClient(provider);


                    // add DHL 1 Hello world
                    // Job<string> job = new() { Priority = prio, JobDescription = string.Join(" ", segments.Skip(3)) };
                    Job<string> job = new(prio, string.Join(" ", segments.Skip(3)));

                    await Console.Out.WriteLineAsync($"Adding Prio: {job.Priority}, Job {job.JobDescription}");
                    await prioritizedQueue.AddJob(job);
                }
                else if (segments.Length == 2 && segments[0] == "get")
                {
                    var provider = segments[1];
                    var prioritizedQueue = getClient(provider);

                    var job = await prioritizedQueue.GetJob();
                    if (job == null)
                    {
                        await Console.Error.WriteLineAsync($"No job available");
                    }
                    else
                    {
                        await Console.Out.WriteLineAsync($"Prio: {job.Priority}, Job {job.JobDescription}");
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