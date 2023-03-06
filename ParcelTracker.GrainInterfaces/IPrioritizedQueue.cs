namespace ParcelTracker.GrainInterfaces;

using Orleans;

public interface IPrioritizedQueue<T> : IGrainWithStringKey
{
    Task AddJob(int priority, T job);

    Task<(int, T)?> GetJob();
}