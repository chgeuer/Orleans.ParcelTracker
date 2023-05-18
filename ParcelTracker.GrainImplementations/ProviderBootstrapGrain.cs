using Microsoft.Extensions.Configuration;

namespace ParcelTracker.GrainImplementations;

public class IProviderBootstrap : IGrainBase, IProviderBootstrapGrain
{
    public IGrainContext GrainContext { get; init; }
    private readonly ILogger<IProviderBootstrap> logger;
    private readonly IClusterClient clusterClient;

    // TODO Check whether a https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2?view=net-7.0 would serialize into Grain state.
    private readonly IPersistentState<Dictionary<string, ProviderConfiguration>> state;

    public IProviderBootstrap(
        IGrainContext context,
        ILogger<IProviderBootstrap> logger,
        [PersistentState(
            stateName: ParcelTrackerConstants.StateType.ProviderBootstrap,
            storageName: ParcelTrackerConstants.GrainStorageName)]
        IPersistentState<Dictionary<string, ProviderConfiguration>> state,
        IClusterClient clusterClient
        )
    {
        (GrainContext, this.logger, this.state, this.clusterClient) = (context, logger, state, clusterClient);
    }

    async Task IProviderBootstrapGrain.SetProvider(ProviderConfiguration configuration)
    {
        logger.LogDebug("{GrainId} {MethodName} \"{ProviderName}\"",
            GrainContext.GrainId,
            nameof(IProviderBootstrapGrain.SetProvider),
            configuration.ProviderName);

        state.State[configuration.ProviderName] = configuration;
        await state.WriteStateAsync();
        await InitializeProviderGrain(configuration);
    }

    async Task IProviderBootstrapGrain.SetConcurrency(string providerName, int newConcurrency)
    {
        if (state.State.ContainsKey(providerName))
        {
            var providerConfigurationGrain = clusterClient.GetGrain<IProviderConfigurationGrain>(providerName);
            state.State[providerName] = state.State[providerName] with { MaxConcurrency = newConcurrency };

            await Task.WhenAll(
                state.WriteStateAsync(),
                providerConfigurationGrain.SetConcurrency(newConcurrency));
        }
    }

    Task<ProviderConfiguration> IProviderBootstrapGrain.GetConfiguration(string providerName)
        => Task.FromResult(state.State[providerName]);

    Task<IEnumerable<string>> IProviderBootstrapGrain.GetProviders()
        => Task.FromResult(this.state.State.Keys.AsEnumerable());

    Task IProviderBootstrapGrain.ActivateAllProviders()
    {
        logger.LogDebug("{GrainId} {MethodName}", GrainContext.GrainId, nameof(IProviderBootstrapGrain.ActivateAllProviders));

        return Task.WhenAll(state.State.Values.Select(InitializeProviderGrain));
    }

    private async Task InitializeProviderGrain(ProviderConfiguration configuration)
    {
        logger.LogDebug("{GrainId} {MethodName} \"{ProviderName}\"",
            GrainContext.GrainId, nameof(InitializeProviderGrain), configuration.ProviderName);

        var providerConfigurationGrain = clusterClient.GetGrain<IProviderConfigurationGrain>(configuration.ProviderName);
        await providerConfigurationGrain.Initialize(configuration);
    }
}