namespace ParcelTracker.GrainInterfaces;

public interface IProviderConfigurationGrain : IGrainWithStringKey
{
    Task Initialize(ProviderConfiguration providerConfiguration);

    Task<ProviderConfiguration?> GetConfiguration();
}