using Coravel;
using CoravelWindowsService;
using CoravelWindowsService.JobDefinitions;
using NLog.Extensions.Logging;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Coravel Windows Service";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>().
        AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            loggingBuilder.AddNLog();
        });

        services.AddScheduler();
        services.AddTransient<EverySecondsJobDefinition>();
        services.AddTransient<DailyAtJobDefinition>();

    })
    .Build();

host.Run();
