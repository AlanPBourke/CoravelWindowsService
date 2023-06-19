# Overview
This .NET 7+ solution is a simple example illustrating how the Coravel scheduling library can be used to implement a Windows service which executes scheduled jobs. It also illustrates how to integrate [NLog](https://nlog-project.org/) into a Windows service.

It is based on the 'Worker Service' template supplied by Microsoft. You can read about that [here.](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)

## Coravel
[Coravel](https://docs.coravel.net/) is a lightweight scheduling library by James Hickey that allows for the extremely concise definition of scheduled jobs.

# Project Structure

Creating a project based on the 'Worker Service' template will give you essentially a console application with a  **program.cs** entry point and a worker class **worker.cs** derived from **BackgroundService** . These are renamed to [Main.cs](/CoravelWindowsService/Main.cs) and [CoravelService.cs](/CoravelWindowsService/CoravelService.cs) in this colution. 

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

## Job Definitions
The scheduled jobs are defined in a configuration file [service-config.json](/CoravelWindowsService/service-config.json). There are two types of job defined in this file, corresponding to the scheduler methods of same names in Coravel. 

* 'EverySeconds' jobs which run once every defined number of seconds.
* 'DailyAt' jobs which run once every day at the defined hour and minute.

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

These job types are modelled with two classes.

```csharp
public class Job
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class EverySecondsJobDefinition : Job
{
    public int EverySeconds { get; set; }
}

public class DailyAtJobDefinition : Job
{
    public int AtHour { get; set; }
    public int AtMinute { get; set; }
}
```

A [ServiceConfiguration](/CoravelWindowsService/ServiceConfiguration.cs) class defines the structure that the [NewtonSoft Json.NET](https://www.newtonsoft.com/json) library will use to deserialise the configuration file. 

```csharp
public class ServiceConfiguration
{
    public List<EverySecondsJobDefinition> EverySecondsJobDefinitions { get; set; } = new();
    public List<DailyAtJobDefinition> DailyAtJobDefinitions { get; set; } = new();

}
```

To make the defined jobs invocable by Coravel, two further classes implementing the IInvocable interface are needed.

```csharp
public class InvocableJob : IInvocable
{
    public static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public virtual Task Invoke()
    {
        throw new NotImplementedException();
    }
}

public class EverySecondsInvocableJob : InvocableJob
{
    private readonly EverySecondsJobDefinition _jobDefinition = new();

    public EverySecondsInvocableJob(EverySecondsJobDefinition jobDefinition)
    {
        _jobDefinition = jobDefinition;
    }

    public override Task Invoke()
    {
        _logger.Info($"Job '{_jobDefinition.Name}' was invoked.");
        return Task.CompletedTask;
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

# The Service
The **ExecuteAsync** method in [CoravelService.cs](/CoravelWindowsService/CoravelService.cs) runs on service start and takes care of actually adding tasks to the schedule. 

```csharp
foreach (EverySecondsJobDefinition j in serviceConfiguration!.EverySecondsJobDefinitions.Where(c => c.IsEnabled))
{
    _logger.LogInformation($"Adding job '{j.Name}' to run every {j.EverySeconds} seconds.");

    _serviceScheduler.ScheduleWithParams<EverySecondsInvocableJob>(j)
        .EverySeconds(j.EverySeconds);
}

foreach (DailyAtJobDefinition j in serviceConfiguration!.DailyAtJobDefinitions.Where(c => c.IsEnabled))
{
    _logger.LogInformation($"Adding job '{j.Name}' to run daily at hour={j.AtHour} minute={j.AtMinute}.");

    _serviceScheduler.ScheduleWithParams<DailyAtInvocableJob>(j)
        .DailyAt(j.AtHour, j.AtMinute)
        .Zoned(TimeZoneInfo.Local);
}
```

# Installing, Starting, Stopping And Deleting The Service
Once built the service can be installed and removed using an administrator PowerShell prompt. Change directory to the location of the built executable, then to install the service:

```powershell
sc.exe create "Coravel Windows Service" binpath="\path\to\your\solution\bin\debug\net7.0\coravelwindowsservice.exe" 
```
Note use of the executable name so that PowerShell doesn't take it as an alias for 'Set-Content'.

Start it with:
```powershell
sc start "Coravel Windows Service"
```

Check it's running with:
```powershell
get-service "Coravel Windows Service"
```

Stop it with:
```powershell
sc stop "Coravel Windows Service"
```

Delete it before rebuilding a new version with:
```powershell
sc delete "Coravel Windows Service"
```

# Logging
Logging is to dated file in the 'logs' subdirectory beneath the service executable location.

```log
8 2023-05-12 10:06:27.2409 INFO Service is starting ...
8 2023-05-12 10:06:27.5666 INFO Adding job 'Every 15 Seconds Job' to run every 15 seconds.
8 2023-05-12 10:06:27.5666 INFO Adding job 'Every 38 Seconds Job' to run every 38 seconds.
8 2023-05-12 10:06:27.5666 INFO Adding job 'Daily At 10:07 Job' to run daily at hour=10 minute=7.
8 2023-05-12 10:06:27.5666 INFO Adding job 'Daily At 10:08 Job' to run daily at hour=10 minute=8.
8 2023-05-12 10:06:27.5666 INFO Service is started.
8 2023-05-12 10:06:27.5827 INFO Application started. Hosting environment: Production; 
11 2023-05-12 10:06:30.6172 INFO Job 'Every 15 Seconds Job' was invoked.
6 2023-05-12 10:06:38.5768 INFO Job 'Every 38 Seconds Job' was invoked.
8 2023-05-12 10:06:45.5798 INFO Job 'Every 15 Seconds Job' was invoked.
6 2023-05-12 10:07:00.5736 INFO Job 'Every 15 Seconds Job' was invoked.
6 2023-05-12 10:07:00.5736 INFO Job 'Daily At 10:07 Job' was invoked.
8 2023-05-12 10:07:15.5731 INFO Job 'Every 15 Seconds Job' was invoked.
13 2023-05-12 10:07:30.5852 INFO Job 'Every 15 Seconds Job' was invoked.
8 2023-05-12 10:07:38.5733 INFO Job 'Every 38 Seconds Job' was invoked.
8 2023-05-12 10:07:45.5764 INFO Job 'Every 15 Seconds Job' was invoked.
13 2023-05-12 10:08:00.5753 INFO Job 'Every 15 Seconds Job' was invoked.
13 2023-05-12 10:08:00.5753 INFO Job 'Daily At 10:08 Job' was invoked.
8 2023-05-12 10:08:15.5838 INFO Job 'Every 15 Seconds Job' was invoked.
```


