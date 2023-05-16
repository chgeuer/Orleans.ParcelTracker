namespace ParcelTracker.GrainImplementations.ServiceImplementations;

[TrackingClient("Contoso")]
internal class ContosoShipping : ITrackingClient
{
    async Task<string> ITrackingClient.FetchStatus(ProviderConfiguration configuration, string parcelNumber)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        return $"{nameof(ContosoShipping)}: Tracking {configuration.ProviderName} for {parcelNumber}";
    }
}

[TrackingClient("Fabrikam")]
internal class FabrikamShipping : ITrackingClient
{
    async Task<string> ITrackingClient.FetchStatus(ProviderConfiguration configuration, string parcelNumber)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        return $"{nameof(FabrikamShipping)}: Tracking {configuration.ProviderName} for {parcelNumber}";
    }
}

[TrackingClient("DHL")]
internal class DhlShipping : ITrackingClient
{
    async Task<string> ITrackingClient.FetchStatus(ProviderConfiguration configuration, string parcelNumber)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(10));

        return $"{nameof(DhlShipping)}: Tracking {configuration.ProviderName} for {parcelNumber}";
    }
}
