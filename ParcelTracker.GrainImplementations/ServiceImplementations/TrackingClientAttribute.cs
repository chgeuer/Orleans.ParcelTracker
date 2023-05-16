namespace ParcelTracker.GrainImplementations.ServiceImplementations;

[AttributeUsage(AttributeTargets.Class)]
public class TrackingClientAttribute : Attribute
{
    public TrackingClientAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; private set; }
}