namespace ParcelTracker.GrainImplementations;

[KeepAlive]
public class ProviderAPICallerGrain : IGrainBase, IProviderAPICallerGrain
    //, IRemindable
{
    private readonly ILogger<IProviderAPICallerGrain> logger;
    private readonly IClusterClient clusterClient;

    public IGrainContext GrainContext { get; init; }
    private ProviderConfiguration? configuration;
    ITrackingClient? trackingClient = null;
    private CancellationTokenSource? cts;
    private CancellationToken innerCancellationToken;
    private Task? _backgroundTask;
    private IPrioritizedQueueGrain<string>? queueClient;

    public ProviderAPICallerGrain(
        IGrainContext context,
        ILogger<IProviderAPICallerGrain> logger,
        IClusterClient clusterClient)
    {
        (GrainContext, this.logger, this.clusterClient) = (context, logger, clusterClient);

        //this.RegisterOrUpdateReminder(
        //    reminderName: "reminder123",
        //    dueTime: TimeSpan.FromSeconds(3),
        //    period: TimeSpan.FromSeconds(61));
    }

    async Task IProviderAPICallerGrain.Start(ProviderConfiguration providerConfiguration)
    {
        logger.LogDebug("{GrainId} {MethodName} {Key}", GrainContext.GrainId, nameof(IProviderAPICallerGrain.Start), this.GetPrimaryKeyString());

        configuration = providerConfiguration;
        trackingClient = ITrackingClient.GetTrackingClient(providerConfiguration);
        if (trackingClient == null)
        {
            logger.LogError("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" Could not instantiate tracking client for name {ProviderName}",
                GrainContext.GrainId, nameof(IProviderAPICallerGrain.Start),
                this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), providerConfiguration.ProviderName);

            throw new NotSupportedException($"Could not instantiate {nameof(ITrackingClient)} implementation for {providerConfiguration.ProviderName}");
        }

        queueClient = clusterClient.GetGrain<IPrioritizedQueueGrain<string>>(primaryKey: providerConfiguration.ProviderName);

        //this.RegisterTimer(this.Timer, "hallo", dueTime: TimeSpan.FromSeconds(1), period: TimeSpan.FromSeconds(5));

        await StopBackgroundTask();

        this.cts = new CancellationTokenSource();//.CreateLinkedTokenSource(token: outerCancellationToken);
        this.innerCancellationToken = cts.Token;
        this.innerCancellationToken.Register(InnerCancellationTokenCancelled);
        this._backgroundTask = Loop();
    }

    Task IGrainBase.OnActivateAsync(CancellationToken outerCancellationToken)
    {
        logger.LogInformation("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" state",
            GrainContext.GrainId, nameof(IGrainBase.OnActivateAsync), this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

        return Task.CompletedTask;
    }

    private void InnerCancellationTokenCancelled()
    {
        logger.LogWarning("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" ######## Cancelled",
            GrainContext.GrainId, nameof(InnerCancellationTokenCancelled), this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());
    }

    private async Task Loop()
    {
        await Task.Yield();

        while (!innerCancellationToken.IsCancellationRequested)
        {
            if (trackingClient == null || configuration == null || queueClient == null)
            {
                logger.LogInformation("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" Loop is missing configuration}",
                    GrainContext.GrainId, nameof(Loop), this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

                await Task.Delay(millisecondsDelay: 100);
                continue;
            }

            var job = await queueClient.GetJob();
            if (job == null)
            {
                logger.LogInformation("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" Idling...",
                   GrainContext.GrainId, nameof(Loop), this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

                await Task.Delay(millisecondsDelay: 1000);
                continue;
            }

            var response = await trackingClient.FetchStatus(configuration, trackingJob: job);
            logger.LogInformation("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" Looooping... {Response}",
                GrainContext.GrainId, nameof(Loop), this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), response.Response);
        }
    }

    async Task IProviderAPICallerGrain.Stop()
    {
        logger.LogWarning("{GrainId} {MethodName}: Shutting down {Provider}/{Number} due to new maximum concurrency",
            GrainContext.GrainId, nameof(IProviderAPICallerGrain.Stop), this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

        await StopBackgroundTask();

        // await GrainContext.DeactivateAsync(new(DeactivationReasonCode.ApplicationRequested, "Scale-down requested"));
    }

    async Task StopBackgroundTask()
    {
        cts?.Cancel();
        if (_backgroundTask != null)
        {
            await _backgroundTask;
        }
        (cts, innerCancellationToken, _backgroundTask) = (null, CancellationToken.None, null);
    }

    Task IGrainBase.OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        logger.LogDebug("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\"", GrainContext.GrainId, nameof(IGrainBase.OnDeactivateAsync), this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

        // await StopBackgroundTask();

        return Task.CompletedTask;
    }

    //private Task Timer(object o)
    //{
    //    logger.LogDebug("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" Object {object}",
    //        GrainContext.GrainId, nameof(this.Timer),
    //        this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), o);
    //
    //    return Task.CompletedTask;
    //}
    //
    //Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    //{
    //    logger.LogDebug("{GrainId} {MethodName} \"{Provider}\" Number \"{Number}\" ReminderName {reminderName} TickStatus {TickStatus}", GrainContext.GrainId, nameof(IRemindable.ReceiveReminder), this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), reminderName, status);
    //
    //    return Task.CompletedTask;
    //}
}
