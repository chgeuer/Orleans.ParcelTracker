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

Please see specific client commands [here](ParcelTracker.Client/README.md).



## Notes

- @ReubenBond: There are a few different kinds of rate limiters there. One of them is a concurrency limiter: https://github.com/ReubenBond/DistributedRateLimiting.Orleans
- Check Priority queue from .NET
- `[KeepAliveAttribute]` on API Grain?
