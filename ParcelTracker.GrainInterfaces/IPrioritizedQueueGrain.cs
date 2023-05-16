namespace ParcelTracker.GrainInterfaces;

public interface IPrioritizedQueueGrain<T> : IGrainWithStringKey
{
    Task AddJob(Job<T> job);

    Task<Job<T>?> GetJob();
}
