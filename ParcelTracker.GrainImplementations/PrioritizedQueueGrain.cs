namespace ParcelTracker.GrainImplementations;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using GrainInterfaces;
using Orleans.Runtime;

public class PrioritizedQueueGrain<T> : Grain, IPrioritizedQueue<T>
{
    private readonly ILogger<PrioritizedQueueGrain<T>> logger;
    private readonly IPersistentState<Dictionary<int, Queue<T>>> state;

    public PrioritizedQueueGrain(
        ILogger<PrioritizedQueueGrain<T>> logger,
        [PersistentState(stateName: "prioritizedQueue", storageName: "blobGrainStorage")]
        IPersistentState<Dictionary<int, Queue<T>>> state)
    {
        this.logger = logger;
        this.state = state;
    }

    public async Task AddJob(Job<T> job)
    {
        logger.LogInformation("Added job {Job} to {Id}", job, this.GetPrimaryKeyString());

        if (!state.State.ContainsKey(job.Priority))
        {
            state.State[job.Priority] = new();
        }

        state.State[job.Priority].Enqueue(job.JobDescription);

        await state.WriteStateAsync();
    }

    public async Task<Job<T>?> GetJob()
    {
        foreach (var prio in state.State.Keys.Order())
        {
            var queue = state.State[prio];
            if (queue.Count > 0)
            {
                // Remove item from state, save updated state and return result.
                Job<T> job = new(prio, queue.Dequeue());

                await state.WriteStateAsync();

                logger.LogInformation("Returning job {Job} from {Id}", job, this.GetPrimaryKeyString());

                return job;
            }
        }

        logger.LogInformation("Poll against empty {Id}", this.GetPrimaryKeyString());
        return null;
    }
}
