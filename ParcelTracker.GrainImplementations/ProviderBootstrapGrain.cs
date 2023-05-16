namespace ParcelTracker.GrainImplementations;

using GrainInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System.Collections.Generic;
using System.Threading.Tasks;

public class IProviderBootstrap : IGrainBase, IProviderBootstrapGrain
{
    public IGrainContext GrainContext { get; init; }
    private readonly ILogger<IProviderBootstrap> logger;

    // TODO Check whether a https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2?view=net-7.0 would serialize into Grain state.
    private readonly IPersistentState<Dictionary<string, ProviderConfiguration>> state;

    private readonly IClusterClient clusterClient;

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

    async Task IProviderBootstrapGrain.AddAndActivateProvider(ProviderConfiguration configuration)
    {
        logger.LogDebug("{GrainId} {MethodName} \"{ProviderName}\"",
            GrainContext.GrainId,
            nameof(IProviderBootstrapGrain.AddAndActivateProvider),
            configuration.ProviderName);

        state.State[configuration.ProviderName] = configuration;
        await state.WriteStateAsync();

        await ActivateSingleProvider(configuration);
    }

    Task<IEnumerable<string>> IProviderBootstrapGrain.GetProviders()
    {
        return Task.FromResult(this.state.State.Keys.AsEnumerable());
    }

    Task IProviderBootstrapGrain.ActivateAllProviders()
    {
        logger.LogDebug("{GrainId} {MethodName}", GrainContext.GrainId, nameof(IProviderBootstrapGrain.ActivateAllProviders));

        return Task.WhenAll(state.State.Values.Select(ActivateSingleProvider));
    }

    private async Task ActivateSingleProvider(ProviderConfiguration configuration)
    {
        logger.LogDebug("{GrainId} {MethodName} \"{ProviderName}\"",
            GrainContext.GrainId, nameof(ActivateSingleProvider), configuration.ProviderName);

        var providerConfigurationGrain = clusterClient.GetGrain<IProviderConfigurationGrain>(configuration.ProviderName);
        await providerConfigurationGrain.Initialize(configuration);
    }
}