namespace ParcelTracker.GrainInterfaces;

[GenerateSerializer]
public record ProviderConfiguration(
    int MaxConcurrency,
    string ProviderName,
    string ProviderURL);
