# Parcel Tracker - Client

The client provided is a simple console application that connects to the cluster and allows you to interact with it. You could interact with the cluster on an individual commands (per provider) or in batch using a configuration file which will perform the required operations on the cluster.

### Batch Commands

Once connected to the cluster, you can load a configuration file:

```
load <providers/jobs> <path/to/file.json>
```

Sample configuration files are provided in the `samples` folder.
As of now there are two types of configuration files:

- `providers.json`: contains the list of providers to be used by the system

```json
[
  {
    "Name": "DHL",
    "URL": "http://www.provider1.com",
    "ConcurrentExecutions": 2
  },
  {
    "Name": "Fabrikam",
    "URL": "http://www.provider2.com",
    "ConcurrentExecutions": 4
  }
]
```

- `jobs.json`: contains the list of jobs to be executed by the system

```json
{
  "ProviderName": "Fabrikam",
  "tars": [
    {
      "Name": "Job1",
      "Priority": 10,
      "Description": "Job 1 description"
    },
    {
      "Name": "Job2",
      "Priority": 10,
      "Description": "Job 2 description"
    },
    {
      "Name": "Job3",
      "Priority": 20,
      "Description": "Job 3 description"
    }
  ]
}
```

__Note__: A provider name must match an exisitng grain implementation, these implementation are loaded in runtime.

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
inc/dec <provider name>
```
Add/Remove an addtional executor to the given provider.




```
get <provider>
```

Gets the next task/job from the given provider priority queue.

