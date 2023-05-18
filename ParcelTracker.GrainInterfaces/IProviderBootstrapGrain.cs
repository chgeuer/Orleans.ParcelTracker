namespace ParcelTracker.GrainInterfaces;

public interface IProviderBootstrapGrain : IGrainWithStringKey
{
    Task SetProvider(ProviderConfiguration providerConfiguration);

    Task SetConcurrency(string providerName, int concurrency);

    Task<ProviderConfiguration> GetConfiguration(string providerName);

    Task ActivateAllProviders();

    Task<IEnumerable<string>> GetProviders();
}

public static class IProviderBootstrapGrainExtensions
{
    public static IProviderBootstrapGrain CreateProviderBootstrapGrainClient(this IClusterClient clusterClient)
        => clusterClient.GetGrain<IProviderBootstrapGrain>(primaryKey: "bootstrapSingleton");
}