namespace ParcelTracker.GrainImplementations.ServiceImplementations;

public interface ITrackingClient
{
    Task<string> FetchStatus(ProviderConfiguration configuration, string parcelNumber);

    private static IEnumerable<(string Name, ITrackingClient Client)> GetTrackingClients(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            var attrs = type
                .GetCustomAttributes(typeof(TrackingClientAttribute), inherit: true)
                .Cast<TrackingClientAttribute>()
                .ToArray();

            if (attrs.Length > 0)
            {
                var instance = (ITrackingClient)Activator.CreateInstance(type)!;

                yield return (attrs[0].Name, Client: instance);
            }
        }
    }

    public static ITrackingClient GetTrackingClient(ProviderConfiguration config, Assembly? assembly = null)
        => GetTrackingClients(assembly).FirstOrDefault(x => x.Name == config.ProviderName).Client;
}
