namespace ParcelTracker.GrainInterfaces;

[GenerateSerializer]
public record ProviderConfiguration(
    string ProviderName,
    int MaxConcurrency,
    string ProviderURL);
