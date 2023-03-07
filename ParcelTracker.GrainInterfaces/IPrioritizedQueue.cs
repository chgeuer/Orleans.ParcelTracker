namespace ParcelTracker.GrainInterfaces;

using Orleans;

[GenerateSerializer]
public record Job<T>(int Priority, T JobDescription);

public interface IPrioritizedQueue<T> : IGrainWithStringKey
{
    Task AddJob(Job<T> job);

    Task<Job<T>?> GetJob();
}
