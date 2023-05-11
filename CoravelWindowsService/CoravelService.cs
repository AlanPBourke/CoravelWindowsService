using Coravel.Scheduling.Schedule.Interfaces;
using CoravelWindowsService.Invocables;
using CoravelWindowsService.JobDefinitions;
using CoravelWindowsService.Models;
using Newtonsoft.Json;

namespace CoravelWindowsService;

public class CoravelService : BackgroundService
{
    private readonly ILogger<CoravelService> _logger;
    private IScheduler _serviceScheduler { get; set; }

    public CoravelService(ILogger<CoravelService> logger, IScheduler serviceScheduler)
    {
        _logger = logger;
        _serviceScheduler = serviceScheduler;
    }

    public Task StopAsync()
    {
        _logger.LogInformation("Service is stopping.");
        return Task.CompletedTask;
    }

    // The business end of things.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service is starting ...");

        string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "service-config.json");
        ServiceConfiguration? serviceConfiguration = new();

        if (!File.Exists(configFile))
        {
            _logger.LogCritical($"Cannot find {configFile} - service will not start.");
            await Task.FromException(new FileNotFoundException());
        }

        try
        {
            // Get the service configuration.
            serviceConfiguration = JsonConvert.DeserializeObject<ServiceConfiguration>(File.ReadAllText(configFile));

            // Schedule any enabled 'run every x seconds' jobs.
            foreach (EverySecondsJobDefinition j in serviceConfiguration!.EverySecondsJobDefinitions.Where(c => c.IsEnabled))
            {
                _logger.LogInformation($"Adding job '{j.Name}' to run every {j.EverySeconds} seconds.");

                _serviceScheduler.ScheduleWithParams<EverySecondsInvocableJob>(j)
                    .EverySeconds(j.EverySeconds);
            }

            // Schedule any enabled 'run daily at HH:MM' jobs.
            foreach (DailyAtJobDefinition j in serviceConfiguration!.DailyAtJobDefinitions.Where(c => c.IsEnabled))
            {
                _logger.LogInformation($"Adding job '{j.Name}' to run daily at hour={j.AtHour} minute={j.AtMinute}.");

                _serviceScheduler.ScheduleWithParams<DailyAtInvocableJob>(j)
                    .DailyAt(j.AtHour, j.AtMinute)
                    .Zoned(TimeZoneInfo.Local);
            }

            _logger.LogInformation("Service is started.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Critical: {ex.Message}");
        }

        await Task.CompletedTask;                   // Stop VS moaning about async method with no 'await's
    }
}