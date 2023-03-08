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

public class ParcelTrackerSettings
{
    public OrleansGrainStorageSettings? OrleansGrainStorage { get; set; }
}

public class OrleansGrainStorageSettings
{
    public string? ServiceURI { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

