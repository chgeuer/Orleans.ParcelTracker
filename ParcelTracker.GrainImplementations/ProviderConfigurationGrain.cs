namespace ParcelTracker.GrainImplementations;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using GrainInterfaces;

[GenerateSerializer]
public class ProviderConfigurationGrainState
{
    public ProviderConfiguration? ProviderConfiguration { get; set; }

    public IEnumerable<int>? CurrentlyActivatedGrains { get; set; }
}

public class ProviderConfigurationGrain : IGrainBase, IProviderConfigurationGrain
{
    public IGrainContext GrainContext { get; init; }
    private readonly ILogger<IProviderConfigurationGrain> logger;
    private readonly IPersistentState<ProviderConfigurationGrainState> state;
    private readonly IClusterClient clusterClient;

    public ProviderConfigurationGrain(
        IGrainContext context,
        ILogger<IProviderConfigurationGrain> logger,
        [PersistentState(stateName: "providerConfiguration", storageName: "blobGrainStorage")]
        IPersistentState<ProviderConfigurationGrainState> state,
        IClusterClient clusterClient)
    {
        this.GrainContext = context;
        this.logger = logger;

        logger.LogInformation("Constructor ProviderConfigurationGrain {State}", state);
        this.state = state;
        this.clusterClient = clusterClient;
    }

    Task<ProviderConfiguration?> IProviderConfigurationGrain.GetConfiguration()
    {
        return Task.FromResult(state.State.ProviderConfiguration);
    }

    async Task IProviderConfigurationGrain.Initialize(ProviderConfiguration providerConfiguration)
    {
        logger.LogInformation("Initializing {Provider}", providerConfiguration.ProviderName);

        state.State.ProviderConfiguration = providerConfiguration;

        (int Id, Task Task) initialize(int primaryKey)
        {
            var client = clusterClient.GetGrain<IProviderAPICallerGrain>(
                    primaryKey: primaryKey,
                    keyExtension: providerConfiguration.ProviderName);

            logger.LogDebug("Instantiate {Provider}-{Id}", providerConfiguration.ProviderName, primaryKey);

            return (primaryKey, client.Initialize(providerConfiguration));
        }

        // Kick off the API caller grains...
        var initializers = Enumerable
            .Range(0, providerConfiguration.MaxConcurrency)
            .Select(initialize)
            .ToArray();

        await Task.WhenAll(initializers.Select(i => i.Task));

        state.State.CurrentlyActivatedGrains = initializers.Select(i => i.Id).ToArray();

        logger.LogInformation("Initialized {Provider} with {InstanceCount}",
            providerConfiguration.ProviderName,
            state.State.CurrentlyActivatedGrains.Count());

        await state.WriteStateAsync();
    }
}