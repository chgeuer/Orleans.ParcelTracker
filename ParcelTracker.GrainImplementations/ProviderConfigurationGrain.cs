namespace ParcelTracker.GrainImplementations;

public class ProviderConfigurationGrain : IGrainBase, IProviderConfigurationGrain
{
    public IGrainContext GrainContext { get; init; }
    private readonly ILogger<IProviderConfigurationGrain> logger;
    private readonly IClusterClient clusterClient;
    private ProviderConfiguration? configuration;

    public ProviderConfigurationGrain(
        IGrainContext grainContext,
        ILogger<IProviderConfigurationGrain> logger,
        IClusterClient clusterClient)
    {
        logger.LogDebug("{GrainType}()", nameof(ProviderConfigurationGrain));

        (GrainContext, this.logger, this.clusterClient) = (grainContext, logger, clusterClient);
    }

    async Task IProviderConfigurationGrain.Initialize(ProviderConfiguration newConfiguration)
    {
        logger.LogDebug("{GrainId} {MethodName} \"{ProviderName}\"",
           GrainContext.GrainId, nameof(IProviderConfigurationGrain.Initialize), newConfiguration.ProviderName);

        if (configuration == null)
        {
            configuration = newConfiguration;

            (int Id, Task Task) initialize(int primaryKey)
            {
                var client = clusterClient.GetGrain<IProviderAPICallerGrain>(
                        primaryKey: primaryKey,
                        keyExtension: newConfiguration.ProviderName);

                logger.LogDebug("Instantiate {Provider}-{Id}", configuration.ProviderName, primaryKey);

                return (primaryKey, client.Initialize(configuration));
            }

            // Kick off the API caller grains...
            var initializers = Enumerable
                .Range(0, newConfiguration.MaxConcurrency)
                .Select(initialize)
                .ToArray();

            await Task.WhenAll(initializers.Select(i => i.Task));

            logger.LogInformation("{GrainId} {MethodName} Initialized {Provider} with {InstanceCount} instances",
                GrainContext.GrainId, nameof(IProviderConfigurationGrain.Initialize),
                newConfiguration.ProviderName, initializers.Length);
        }
        else
        {
            // The ProviderConfigurationGrain is initialized a 2nd time, potentially with a different configuration.
            // If we're running too many API workers, need to de-activate some.
            //
            var max = Math.Max(newConfiguration.MaxConcurrency, configuration.MaxConcurrency);
            foreach (var instanceNumber in Enumerable.Range(0, count: max))
            {
                var client = clusterClient.GetGrain<IProviderAPICallerGrain>(
                    primaryKey: instanceNumber, keyExtension: newConfiguration.ProviderName);

                if (instanceNumber < newConfiguration.MaxConcurrency)
                {
                    // This one is certainly already running, will re-initialize.
                    await client.Initialize(newConfiguration);
                }
                else
                {
                    // newConfiguration.MaxConcurrency >= current Configuration's MaxConcurrency
                    await client.Deactivate();
                }
            }
        }
    }

    Task<ProviderConfiguration?> IProviderConfigurationGrain.GetConfiguration()
    {
        return Task.FromResult(configuration);
    }
}