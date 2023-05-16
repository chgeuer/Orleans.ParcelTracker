namespace ParcelTracker;

public static class ParcelTrackerConstants
{
    public const string GrainStorageName = "blobGrainStorage";
    public static class StateType
    {
        public const string ProviderBootstrap = "providerBootstrap";
        public const string PrioritizedQueue = "prioritizedQueue";
    }
}