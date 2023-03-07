namespace ParcelTracker.GrainImplementations;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using GrainInterfaces;
using System.Threading;
using Orleans.Runtime;

public class ProviderAPICallerGrain : Grain, IProviderAPICallerGrain, IRemindable
{
    private readonly ILogger<IProviderAPICallerGrain> logger;
    private readonly IClusterClient clusterClient;
    private ProviderConfiguration? providerConfiguration;

    public ProviderAPICallerGrain(ILogger<IProviderAPICallerGrain> logger, IClusterClient clusterClient)
    {
        this.logger = logger;
        this.clusterClient = clusterClient;

        this.RegisterOrUpdateReminder(reminderName: "reminder123", dueTime: TimeSpan.FromSeconds(3), period: TimeSpan.FromSeconds(61));
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("OnActivateAsync \"{Provider}\" Number \"{Number}\" state",
          this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        logger.LogInformation("OnDeactivateAsync \"{Provider}\" Number \"{Number}\" state",
         this.GetPrimaryKeyString(), this.GetPrimaryKeyLong());

        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    private bool initialized = false;
    public Task Initialize(ProviderConfiguration providerConfiguration)
    {
        logger.LogInformation("Initializing API Caller ProviderI \"{Provider}\" Number \"{Number}\" state: {Initialized}",
            this.GetPrimaryKeyString(), this.GetPrimaryKeyLong(), initialized);

        initialized = true;
        this.providerConfiguration = providerConfiguration;

        //this.RegisterTimer(this.Timer, "hallo", dueTime: TimeSpan.FromSeconds(1), period: TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }

    public Task Timer(object o)
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