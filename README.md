# CoravelWindowsService
[Coravel](https://docs.coravel.net/) is a lightweight scheduling library by James Hickey that allows for the extremely concise definition of scheduled jobs.

This .NET 7+ solution is a simple example illustrating how Coravel can be used to implement a Windows service which executes scheduled jobs. It also illustrates how to integrate [NLog](https://nlog-project.org/) into a Windows service.

## Scheduled Job Configuration 
The scheduled jobs are defined in a configuration file [service-config.json](/CoravelWindowsService/service-config.json). This file is read at service start and jobs are added to the Coravel scheduler based on these definitions. There are two types of job defined here. 

* 'EverySeconds' jobs which run once every defined number of seconds.
* 'DailyAt' jobs which run once every day at the defined hour and minute.

These correspond to the schedule methods of same names in Coravel. Obviously further types could be added to cater for the many other types of schedule in Coravel.

```javascript
{
  "EverySecondsJobDefinitions": [
    {
      "Name": "Every x Seconds Job 1",
      "EverySeconds": 15,
      "IsEnabled": true
    },
    {
      "Name": "Every x Seconds Job 2",
      "EverySeconds": 20,
      "IsEnabled": true
    }
  ],
  "DailyAtJobDefinitions": [
    {
      "Name": "Daily At HH:SS Job 1",
      "AtHour": 24,
      "AtMinute": 30,
      "IsEnabled": true
    },
    {
      "Name": "Daily At HH:SS Job 2",
      "AtHour": 13,
      "AtMinute": 0,
      "IsEnabled": true
    }
  ]
}
```

# Overview
This solution is based on the 'Worker Service' project from the standard template supplied by Microsoft. You can read about that [here.](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)

This template will give you a project with a  **program.cs** entry point, which is renamed to [Main.cs](/CoravelWindowsService/Main.cs) in this solution and a **worker.cs** class containing a worker class derived from **BackgroundService**, renamed to [CoravelService.cs](/CoravelWindowsService/CoravelService.cs) in this colution. 

## The Service Entry Point
[Main.cs](/CoravelWindowsService/Main.cs) shows how to add NLog logging and Coravel scheduling into the host container. No jobs are added to the scheduler here.

```csharp
IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Coravel Windows Service";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<CoravelService>().
        AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            loggingBuilder.AddNLog();
        });

        services.AddScheduler();
        services.AddTransient<EverySecondsInvocableJob>();
        services.AddTransient<DailyAtInvocableJob>();

    })
    .Build();
```

## The Actual Service
[CoravelService.cs](/CoravelWindowsService/CoravelService.cs) takes care of actually adding tasks to the schedule. Job definitions are deserialised from [service-config.json](/CoravelWindowsService/service-config.json) into an instance of the [ServiceConfiguration](/CoravelWindowsService/ServiceConfiguration.cs) class. That looks like this.

```csharp
public class ServiceConfiguration
{
    public List<EverySecondsJobDefinition> EverySecondsJobDefinitions { get; set; } = new();
    public List<DailyAtJobDefinition> DailyAtJobDefinitions { get; set; } = new();

}
```

Where the 'Daily At' job definition, for example, is:

```csharp
public class Job
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class DailyAtJobDefinition : Job
{
    public int AtHour { get; set; }
    public int AtMinute { get; set; }
}
```

Now there are two lists of job definitions but they are not invocable by the Coravel scheduler. To do that, invocable classes are defined for the two types of job like this:

```csharp
public class InvocableJob : IInvocable
{
    public static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public virtual Task Invoke()
    {
        throw new NotImplementedException();
    }
}

public class DailyAtInvocableJob : InvocableJob
{
    private readonly DailyAtJobDefinition _jobDefinition = new();

    public DailyAtInvocableJob(DailyAtJobDefinition jobDefinition)
    {
        _jobDefinition = jobDefinition;
    }

    public override Task Invoke()
    {
        _logger.Info($"Job '{_jobDefinition.Name}' was invoked.");
        return Task.CompletedTask;
    }
}

```

Now the lists of job definitions can be used together with the invocable classes to create Coravel jobs.

```csharp
foreach (DailyAtJobDefinition j in serviceConfiguration!.DailyAtJobDefinitions.Where(c => c.IsEnabled))
{
    _logger.LogInformation($"Adding job '{j.Name}' to run daily at hour={j.AtHour} minute={j.AtMinute}.");

    _serviceScheduler.ScheduleWithParams<DailyAtInvocableJob>(j)
        .DailyAt(j.AtHour, j.AtMinute)
        .Zoned(TimeZoneInfo.Local);
}
```



