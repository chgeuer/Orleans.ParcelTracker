namespace ParcelTracker.GrainInterfaces;

using Orleans;

[GenerateSerializer]
public record Job<T>(
    int Priority,
    T JobDescription);

public interface IPrioritizedQueue<T> : IGrainWithStringKey
{
    Task AddJob(Job<T> job);

    Task<Job<T>?> GetJob();
}

[GenerateSerializer]
public record ProviderConfiguration(
    int MaxConcurrency,
    string ProviderName,
    string ProviderURL);

public interface IProviderConfigurationGrain : IGrainWithStringKey
{
    Task Initialize(ProviderConfiguration providerConfiguration);

    Task<ProviderConfiguration?> GetConfiguration();
}

public interface IProviderAPICallerGrain : IGrainWithIntegerCompoundKey
{
    Task Initialize(ProviderConfiguration providerConfiguration);
}