# Orleans Parcel tracker

## Configuration

### Environment variables

In case you want to use environment variables, set the following ones (with proper values):

```
ParcelTracker__OrleansGrainStorage__ServiceURI=https://chgpes1.blob.core.windows.net/
ParcelTracker__OrleansGrainStorage__TenantId=xxx.onmicrosoft.com
ParcelTracker__OrleansGrainStorage__ClientId=6a429abb-8570-4747-abbe-816745c65af8...
ParcelTracker__OrleansGrainStorage__ClientSecret=...
```

The demo uses a service principal to persist grain state in Azure blob storage.


## Client Commands

### Batch Commands

Once connected to the cluster, you can load a configuration file:

```
load <providers/jobs> <path/to/file.json>
```

Sample configuration files are provided in the `samples` folder.
As of now there are two types of configuration files:

- `providers.json`: contains the list of providers to be used by the system

- `jobs.json`: contains the list of jobs to be executed by the system

### Individual Commands

```
exit
```

Shuts down the client.



```
add <Provider name> <Priority> <Description>
```

Adding a task/job to a providor with the given priority and description.



```
start <Provider> <Number of executors>
```

Starts the given provider with the given number of executors.


```
get <Provider>
```

Gets the next task/job from the given provider priority queue.


## Notes

- @ReubenBond: There are a few different kinds of rate limiters there. One of them is a concurrency limiter: https://github.com/ReubenBond/DistributedRateLimiting.Orleans
- Check Priority queue from .NET
- `[KeepAliveAttribute]` on API Grain?
