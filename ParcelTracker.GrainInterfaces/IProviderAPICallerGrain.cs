namespace ParcelTracker.GrainInterfaces;

public interface IProviderAPICallerGrain : IGrainWithIntegerCompoundKey
{
    Task Initialize(ProviderConfiguration providerConfiguration);
}