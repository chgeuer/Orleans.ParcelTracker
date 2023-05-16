namespace ParcelTracker.GrainInterfaces;

public interface IProviderBootstrapGrain : IGrainWithStringKey
{
    Task AddAndActivateProvider(ProviderConfiguration providerConfiguration);

    Task ActivateAllProviders();

    Task<IEnumerable<string>> GetProviders();
}