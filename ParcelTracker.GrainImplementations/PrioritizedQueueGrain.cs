namespace ParcelTracker.GrainImplementations;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using GrainInterfaces;

public class PrioritizedQueueGrain<T> : Grain, IPrioritizedQueue<T>
{
    private readonly ILogger<PrioritizedQueueGrain<T>> logger;
    private readonly Dictionary<int, Queue<T>> jobs = new();

    public PrioritizedQueueGrain(ILogger<PrioritizedQueueGrain<T>> logger)
    {
        this.logger = logger;
    }

    public Task AddJob(int priority, T job)
    {
        logger.LogInformation("Add {Priority}: {Job}", priority, job);

        if (!jobs.ContainsKey(priority))
        {
            jobs[priority] = new();
        }

        jobs[priority].Enqueue(job);

        return Task.CompletedTask;
    }

    public Task<(int, T)?> GetJob()
    {
        foreach (var prio in jobs.Keys.Order())
        {
            var queue = jobs[prio];
            if (queue.Count > 0)
            {
                var x = (prio, queue.Dequeue());
                return Task.FromResult<(int, T)?>(x);
            }
        }

        return Task.FromResult<(int, T)?>(null);
    }
}