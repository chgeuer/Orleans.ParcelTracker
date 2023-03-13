namespace ParcelTracker.GrainImplementations;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using GrainInterfaces;
using Orleans.Runtime;
using System.Runtime.CompilerServices;

public class PrioritizedQueueGrain<T> : IGrainBase, IPrioritizedQueue<T>
{
    public IGrainContext GrainContext { get; init; }
    private readonly ILogger<PrioritizedQueueGrain<T>> logger;

    // TODO Check whether a https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2?view=net-7.0 would serialize into Grain state.
    private readonly IPersistentState<Dictionary<int, Queue<T>>> state;

    public PrioritizedQueueGrain(
        IGrainContext context,
        ILogger<PrioritizedQueueGrain<T>> logger,
        [PersistentState(stateName: "prioritizedQueue", storageName: "blobGrainStorage")]
        IPersistentState<Dictionary<int, Queue<T>>> state //, IOptions<ParcelTrackerSettings> pt
        )
    {
        GrainContext = context;
        this.logger = logger;
        this.state = state;
    }

    Task IPrioritizedQueue<T>.AddJob(Job<T> job) => state.State.AddJob(job, state.WriteStateAsync);

    Task<Job<T>?> IPrioritizedQueue<T>.GetJob() => state.State.GetJob<T>(state.WriteStateAsync);
}

public static class PrioritizedQueueExtension
{
    // The idea of writing the logic as extension functions on the data type allows us to unit test the logic without having to
    // instantiate a PrioritizedQueueGrain.

    public static async Task AddJob<T>(this Dictionary<int, Queue<T>> queue, Job<T> job, Func<Task>? persistChanges = null)
    {
        if (!queue.ContainsKey(job.Priority))
        {
            queue[job.Priority] = new();
        }
        queue[job.Priority].Enqueue(job.JobDescription);

        if (persistChanges != null)
        {
            await persistChanges();
        }
    }

    public async static Task<Job<T>?> GetJob<T>(this Dictionary<int, Queue<T>> prioritizedQueue, Func<Task>? persistChanges = null)
    {
        foreach (var prio in prioritizedQueue.Keys.Order())
        {
            var queue = prioritizedQueue[prio];
            if (queue.Count > 0)
            {
                // Remove item from state, save updated state and return result.
                Job<T> job = new(prio, queue.Dequeue());

                if (persistChanges != null)
                {
                    await persistChanges();
                }

                return job;
            }
        }

        return null;
    }
}