namespace ParcelTracker.GrainInterfaces;

public interface IProviderAPICallerGrain : IGrainWithIntegerCompoundKey
{
    Task Start(ProviderConfiguration providerConfiguration);

    /// <summary>
    /// Called when scale-down is needed.
    /// </summary>
    /// <returns></returns>
    Task Stop();
}