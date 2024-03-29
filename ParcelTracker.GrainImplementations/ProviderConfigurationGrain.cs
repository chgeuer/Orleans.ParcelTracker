﻿namespace ParcelTracker.GrainImplementations;

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
            (int Id, Task Task) initialize(int primaryKey)
            {
                var client = clusterClient.GetGrain<IProviderAPICallerGrain>(
                        primaryKey: primaryKey,
                        keyExtension: newConfiguration.ProviderName);

                logger.LogDebug("Instantiate {Provider}-{Id}", newConfiguration.ProviderName, primaryKey);

                return (primaryKey, client.Start(newConfiguration));
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

            configuration = newConfiguration;
        }
        else
        {
            // The ProviderConfigurationGrain is initialized a 2nd time, potentially with a different configuration.
            // If we're running too many API workers, need to de-activate some.
            //
            var oldConcurrency = configuration.MaxConcurrency;
            var newConcurrency = newConfiguration.MaxConcurrency;
            var max = Math.Max(oldConcurrency, newConcurrency);

            foreach (var instanceNumber in Enumerable.Range(0, count: max))
            {
                var apiCallerGrain = clusterClient.GetGrain<IProviderAPICallerGrain>(
                    primaryKey: instanceNumber, keyExtension: newConfiguration.ProviderName);

                if (instanceNumber < newConfiguration.MaxConcurrency)
                {
                    // This one is certainly already running, will re-initialize.
                    await apiCallerGrain.Start(newConfiguration);
                }
                else
                {
                    var primaryKey = apiCallerGrain.GetPrimaryKeyString();
                    var gid = GrainContext.GrainId;
                    // newConfiguration.MaxConcurrency <= oldConfig.MaxConcurrency
                    logger.LogDebug("{GrainId} {MethodName} \"{ProviderName}\". Deactivating {WorkerGrainID}",
                       gid, nameof(IProviderConfigurationGrain.Initialize), newConfiguration.ProviderName, primaryKey);

                    await apiCallerGrain.Stop();
                }

                configuration = newConfiguration;
            }
        }
    }

    async Task IProviderConfigurationGrain.SetConcurrency(int newConcurrency)
    {
        if (configuration == null)
        {
            var errorMessage = $"Must call {nameof(IProviderConfigurationGrain.Initialize)} prior {nameof(IProviderConfigurationGrain.SetConcurrency)}";

            logger.LogCritical("{GrainId} {MethodName}: {ErrorMessage}",
                GrainContext.GrainId, nameof(IProviderConfigurationGrain.SetConcurrency), errorMessage);

            throw new NotSupportedException(errorMessage);
        }
        var oldConcurrency = configuration.MaxConcurrency;
        configuration = configuration with { MaxConcurrency = newConcurrency };

        if (oldConcurrency < newConcurrency)
        {
            for (var i = oldConcurrency; i < newConcurrency; i++)
            {
                var apiCallerGrain = clusterClient.GetGrain<IProviderAPICallerGrain>(primaryKey: i, keyExtension: configuration.ProviderName);
                await apiCallerGrain.Start(configuration);
            }
        }
        else if (newConcurrency < oldConcurrency)
        {
            for (var i = newConcurrency; i < oldConcurrency; i++)
            {
                var apiCallerGrain = clusterClient.GetGrain<IProviderAPICallerGrain>(primaryKey: i, keyExtension: configuration.ProviderName);
                await apiCallerGrain.Stop();
            }
        }
        else
        {
            // oldConcurrency == newConcurrency -> no change
        }
    }

    Task<ProviderConfiguration?> IProviderConfigurationGrain.GetConfiguration()
    {
        return Task.FromResult(configuration);
    }
}