using Coravel;
using CoravelWindowsService;
using CoravelWindowsService.Invocables;
using NLog.Extensions.Logging;

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

await host.RunAsync();
