namespace ParcelTracker.GrainInterfaces;

[GenerateSerializer]
public record Job<T>(
    int Priority,
    T JobDescription);
