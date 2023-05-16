namespace ParcelTracker.GrainImplementations;

using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using ParcelTracker.GrainImplementations.ServiceImplementations;
using System.Threading;
using System.Threading.Tasks;

[KeepAlive]
public class ProviderAPICallerGrain : IGrainBase, IProviderAPICallerGrain, IRemindable
{
    public IGrainContext GrainContext { get; init; }
    private readonly ILogger<IProviderAPICallerGrain> logger;
    private readonly IClusterClient clusterClient;
    private readonly IPrioritizedQueueGrain<Job<string>> queueClient;
    private ProviderConfiguration? providerConfiguration;
    private bool initialized = false;
    ITrackingClient? trackingClient = null;

    public ProviderAPICallerGrain(
        IGrainContext context,
        ILogger<IProviderAPICallerGrain> logger,
        IClusterClient clusterClient)
    {
        (GrainContext, this.logger, this.clusterClient) = (context, logger, clusterClient);

        queueClient = clusterClient.GetGrain<IPrioritizedQueueGrain<Job<string>>>(grainId: context.GrainId);

        this.RegisterOrUpdateReminder(
            reminderName: "reminder123",
            dueTime: TimeSpan.FromSeconds(3),
            period: TimeSpan.FromSeconds(61));
    }

    Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" state",
            GrainContext.GrainId,
            nameof(IGrainBase.OnActivateAsync),
            this.GetPrimaryKeyString(),
            this.GetPrimaryKeyLong());

        return Task.CompletedTask;
    }

    Task IGrainBase.OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        logger.LogDebug("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\"",
            GrainContext.GrainId, nameof(IGrainBase.OnDeactivateAsync),
            this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

        return Task.CompletedTask;
    }

    Task IProviderAPICallerGrain.Initialize(ProviderConfiguration providerConfiguration)
    {
        if (initialized) {
            logger.LogWarning("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" called twice (already initialized).",
                GrainContext.GrainId, nameof(IProviderAPICallerGrain.Initialize),
                this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());
            return Task.CompletedTask;
        }

        logger.LogDebug("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\"",
           GrainContext.GrainId, nameof(IProviderAPICallerGrain.Initialize),
           this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

        initialized = true;
        this.providerConfiguration = providerConfiguration;
        this.trackingClient = ITrackingClient.GetTrackingClient(providerConfiguration);

        if (this.trackingClient != null)
        {
            logger.LogInformation("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" trackingClient: {TrackingClientType}",
               GrainContext.GrainId, nameof(IProviderAPICallerGrain.Initialize),
               this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), trackingClient.GetType().FullName);
        }
        else
        {
            logger.LogError("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" Could not instantiate tracking client for name {ProviderName}",
               GrainContext.GrainId, nameof(IProviderAPICallerGrain.Initialize),
               this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), providerConfiguration.ProviderName);
        }

        //this.RegisterTimer(this.Timer, "hallo", dueTime: TimeSpan.FromSeconds(1), period: TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }

    private Task Timer(object o)
    {
        logger.LogDebug("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" Object {object}",
            GrainContext.GrainId, nameof(this.Timer),
            this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), o);

        return Task.CompletedTask;
    }

    Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    {
        logger.LogDebug("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" ReminderName {reminderName} TickStatus {TickStatus}",
            GrainContext.GrainId, nameof(IRemindable.ReceiveReminder),
            this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), reminderName, status);

        return Task.CompletedTask;
    }

    async Task IProviderAPICallerGrain.Deactivate(int newMaxConcurrency)
    {
        logger.LogInformation("{GrainId} {MethodName}: Shutting down {Provider}/{Number} due to new maximum concurrency {MaxConcurrency}",
            GrainContext.GrainId, nameof(IProviderAPICallerGrain.Deactivate),
            this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), newMaxConcurrency);

        await GrainContext.DeactivateAsync(new(DeactivationReasonCode.ApplicationRequested, "Scale-down requested"));
    }
}