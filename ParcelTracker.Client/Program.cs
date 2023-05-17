namespace ParcelTracker.Client;

using GrainInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using System.Text.Json;

internal class Program
{
    private class TaR
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
        public string? Description { get; set; }
        public const string DefaultTaRDescription = "DefaultDescription";
    }

    private class TaRs
    {
        public string? ProviderName { get; set; }
        public List<TaR>? tars { get; set; }
    }

    private class ProviderConfig
    {
        // default provider name constant
        public const string DefaultProviderName = "DefaultProvider";
        // default provider url constant
        public const string DefaultProviderURL = "http://www.defaultprovider.com";
        public string? Name { get; set; }
        public string? URL { get; set; }
        public int ConcurrentExecutions { get; set; }
    }

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

        Dictionary<string, IPrioritizedQueueGrain<string>> clients = new();
        IPrioritizedQueueGrain<string> getClient(string provider)
        {
            if (!clients.ContainsKey(provider))
            {
                clients[provider] = clusterClient.GetGrain<IPrioritizedQueueGrain<string>>(primaryKey: provider);
            }
            return clients[provider];
        }

        async Task<string> ReadFileAsync(string filePath)
        {
            return await File.ReadAllTextAsync(filePath);
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
                if (line == "exit")
                {
                    break;
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
                else if (segments.Length == 3 && segments[0] == "start" && int.TryParse(segments[2], out var concurrency))
                {
                    // start DHL 3
                    var provider = segments[1];
                    var configGrain = clusterClient.GetGrain<IProviderConfigurationGrain>(primaryKey: provider);
                    await configGrain.Initialize(new(
                        MaxConcurrency: concurrency,
                        ProviderName: provider,
                        ProviderURL: $"https://{provider}.com"));
                }
                // command would be: load providers file_name
                else if (segments.Length == 3 && segments[0] == "load" && segments[1] == "providers")
                {
                    // load the provider configuration from the json file provided in segments[2]
                    string providers = await ReadFileAsync(segments[2]);
                    ProviderConfig[]? list = JsonSerializer.Deserialize<ProviderConfig[]>(providers);
                    if (list == null || list.Length == 0)
                    {
                        Console.WriteLine("No providers found in the configuration file");
                        continue;
                    }
                    Console.WriteLine($"{list.Length} providers found in the configuration file, creating grains");
                    foreach (var _provider in list!)
                    {

                        var configGrain = clusterClient.GetGrain<IProviderConfigurationGrain>(primaryKey: _provider.Name);
                        await configGrain.Initialize(new(
                            MaxConcurrency: _provider.ConcurrentExecutions,
                            ProviderName: _provider.Name ?? ProviderConfig.DefaultProviderName,
                            ProviderURL: _provider.URL ?? ProviderConfig.DefaultProviderURL));
                    }
                    continue;

                }
                else if (segments.Length == 3 && segments[0] == "load" && segments[1] == "jobs")
                {
                    // load the jobs from the json file provided in segments[2]
                    string jobs = await ReadFileAsync(segments[2]);
                    TaRs? list = JsonSerializer.Deserialize<TaRs>(jobs);
                    if (list == null || list.tars == null || list.tars.Count == 0)
                    {
                        Console.WriteLine("No jobs found in the configuration file");
                        continue;
                    }
                    Console.WriteLine($"{list.tars.Count} jobs found for {list.ProviderName} provider in the configuration file, adding to the queue");
                    foreach (var tar in list.tars)
                    {
                        var prioritizedQueue = getClient(list.ProviderName ?? ProviderConfig.DefaultProviderName);
                        var job = new Job<string>(tar.Priority, tar.Description ?? TaR.DefaultTaRDescription);
                        await prioritizedQueue.AddJob(job);
                    }
                    continue;
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
                else if (segments.Length == 2 && segments[0] == "inc")
                {
                    var provider = segments[1];
                    var configGrain = clusterClient.CreateProviderBootstrapGrainClient();
                    var oldCfg = await configGrain.GetConfiguration(provider);
                    if (oldCfg != null)
                    {
                        var newConcurrency = oldCfg.MaxConcurrency + 1;
                        await configGrain.SetProvider(oldCfg with { MaxConcurrency = newConcurrency });
                        await Console.Out.WriteLineAsync($"Set concurrency for {provider} to {newConcurrency}");
                    }
                }
                else if (segments.Length == 2 && segments[0] == "dec")
                {
                    var provider = segments[1];

                    var configGrain = clusterClient.CreateProviderBootstrapGrainClient();
                    var oldCfg = await configGrain.GetConfiguration(provider);
                    if (oldCfg != null)
                    {
                        var newConcurrency = Math.Max(oldCfg.MaxConcurrency - 1, 1);
                        await configGrain.SetProvider(oldCfg with { MaxConcurrency = newConcurrency });
                        await Console.Out.WriteLineAsync($"Set concurrency for {provider} to {newConcurrency}");
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