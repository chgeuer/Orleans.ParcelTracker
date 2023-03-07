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

    public async Task AddJob(int priority, T job)
    {
        logger.LogInformation("Add {Priority}: {Job}", priority, job);

        if (!this.state.State.ContainsKey(priority))
        {
            this.state.State[priority] = new();
        }

        this.state.State[priority].Enqueue(job);

        await this.state.WriteStateAsync();
    }

    public async Task<(int, T)?> GetJob()
    {
        foreach (var prio in this.state.State.Keys.Order())
        {
            var queue = this.state.State[prio];
            if (queue.Count > 0)
            {
                var x = (prio, queue.Dequeue());

                await this.state.WriteStateAsync();

                return x;
            }
        }

        return null;
    }
}