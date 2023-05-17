namespace ParcelTracker.GrainImplementations.ServiceImplementations;

internal abstract class TrackingClientBase : ITrackingClient
{
    internal static class Speed
    {
        internal static readonly (int Low, int High) Fast = (10, 50);
        internal static readonly (int Low, int High) Slooow = (5000, 5500);
        internal static readonly (int Low, int High) Variance = (10, 5000);
    }
    private static readonly Random Rnd = new();

    internal abstract string ProviderName { get; }
    internal abstract (int Low, int High) LatencyMilliSeconds { get; }

    // Used to track how many concurrent requests are in flight (only works on single machine for debugging).
    // For proper cluster support, would need to implement a grain to store the number.
    internal abstract int StartOperation();
    internal abstract void EndOperation();

    async Task<string> ITrackingClient.FetchStatus(ProviderConfiguration configuration, string parcelNumber)
    {
        var requestsInFlight = StartOperation();
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(Rnd.Next(minValue: LatencyMilliSeconds.Low, maxValue: LatencyMilliSeconds.High)));

            return $"{ProviderName}: Tracking {configuration.ProviderName} for {parcelNumber} ({requestsInFlight} concurrent requests in flight).";
        }
        finally
        {
            EndOperation();
        }
    }
}

[TrackingClient("Contoso")]
internal class ContosoSlowShipping : TrackingClientBase
{
    internal override (int Low, int High) LatencyMilliSeconds => Speed.Slooow;
    internal override string ProviderName => nameof(ContosoSlowShipping);

    private static int RequestsInFlight = 0;
    internal override int StartOperation() => Interlocked.Increment(ref RequestsInFlight);
    internal override void EndOperation() => Interlocked.Decrement(ref RequestsInFlight);
}

[TrackingClient("Fabrikam")]
internal class FabrikamShipping : TrackingClientBase
{
    internal override (int Low, int High) LatencyMilliSeconds => Speed.Slooow;
    internal override string ProviderName => nameof(FabrikamShipping);

    private static int RequestsInFlight = 0;
    internal override int StartOperation() => Interlocked.Increment(ref RequestsInFlight);
    internal override void EndOperation() => Interlocked.Decrement(ref RequestsInFlight);
}

[TrackingClient("DHL")]
internal class TheDhlShippingImplementation : TrackingClientBase
{
    internal override (int Low, int High) LatencyMilliSeconds => Speed.Slooow;
    internal override string ProviderName => nameof(TheDhlShippingImplementation);

    private static int RequestsInFlight = 0;
    internal override int StartOperation() => Interlocked.Increment(ref RequestsInFlight);
    internal override void EndOperation() => Interlocked.Decrement(ref RequestsInFlight);
}
