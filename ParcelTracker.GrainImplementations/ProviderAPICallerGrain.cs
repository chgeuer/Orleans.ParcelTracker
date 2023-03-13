namespace ParcelTracker.GrainImplementations;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using GrainInterfaces;
using System.Threading;
using Orleans.Runtime;

[KeepAlive]
public class ProviderAPICallerGrain : IGrainBase, IProviderAPICallerGrain, IRemindable
{
    public IGrainContext GrainContext { get; init; }
    private readonly ILogger<IProviderAPICallerGrain> logger;
    private readonly IClusterClient clusterClient;
    private ProviderConfiguration? providerConfiguration;

    public ProviderAPICallerGrain(
        IGrainContext context,
        ILogger<IProviderAPICallerGrain> logger,
        IClusterClient clusterClient)
    {
        this.GrainContext = context;
        this.logger = logger;
        this.clusterClient = clusterClient;

        this.RegisterOrUpdateReminder(
            reminderName: "reminder123",
            dueTime: TimeSpan.FromSeconds(3),
            period: TimeSpan.FromSeconds(61));
    }

    Task IGrainBase.OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("OnActivateAsync \"{Provider}\" Number \"{Number}\" state",
          this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

        return Task.CompletedTask;
    }

    Task IGrainBase.OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        logger.LogInformation("OnDeactivateAsync \"{Provider}\" Number \"{Number}\" state",
         this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

        return Task.CompletedTask;
    }

    private bool initialized = false;
    Task IProviderAPICallerGrain.Initialize(ProviderConfiguration providerConfiguration)
    {
        logger.LogInformation("Initializing API Caller ProviderI \"{Provider}\" Number \"{Number}\" state: {Initialized}",
            this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), initialized);

        initialized = true;
        this.providerConfiguration = providerConfiguration;

        //this.RegisterTimer(this.Timer, "hallo", dueTime: TimeSpan.FromSeconds(1), period: TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }

    private Task Timer(object o)
    {
        logger.LogInformation("Timer \"{Provider}\" Number \"{Number}\" Object {object}",
            this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), o);

        return Task.CompletedTask;
    }

    Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    {
        logger.LogInformation("ReceiveReminder \"{Provider}\" Number \"{Number}\" ReminderName {reminderName} TickStatus {TickStatus}",
            this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), reminderName, status);

        return Task.CompletedTask;
    }
}