namespace ParcelTracker.GrainInterfaces;

public interface IProviderConfigurationGrain : IGrainWithStringKey
{
    Task Initialize(ProviderConfiguration providerConfiguration);

    Task SetConcurrency(int concurrency);

    Task<ProviderConfiguration?> GetConfiguration();
}