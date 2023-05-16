namespace ParcelTracker.GrainImplementations;

using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System.Threading.Tasks;



public class ProviderConfigurationGrain : IGrainBase, IProviderConfigurationGrain
{
    public IGrainContext GrainContext { get; init; }
    private readonly ILogger<IProviderConfigurationGrain> logger;
    private readonly IClusterClient clusterClient;
    private ProviderConfiguration? state;

    public ProviderConfigurationGrain(
        IGrainContext grainContext,
        ILogger<IProviderConfigurationGrain> logger,
        IClusterClient clusterClient)
    {
        logger.LogDebug("{GrainType}({State})", nameof(ProviderConfigurationGrain), state);

        (GrainContext, this.logger, this.clusterClient) = (grainContext, logger, clusterClient);
    }

    Task<ProviderConfiguration?> IProviderConfigurationGrain.GetConfiguration()
    {
        return Task.FromResult(state);
    }

    async Task IProviderConfigurationGrain.Initialize(ProviderConfiguration providerConfiguration)
    {
        logger.LogDebug("{GrainId} {MethodName} \"{ProviderName}\"",
           GrainContext.GrainId, nameof(IProviderConfigurationGrain.Initialize), providerConfiguration.ProviderName);

        state = providerConfiguration;

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

        logger.LogInformation("{GrainId} {MethodName} Initialized {Provider} with {InstanceCount} instances",
            GrainContext.GrainId, nameof(IProviderConfigurationGrain.Initialize),
            providerConfiguration.ProviderName, initializers.Length);
    }
}