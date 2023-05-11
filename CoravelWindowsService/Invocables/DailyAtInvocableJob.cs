using CoravelWindowsService.JobDefinitions;

namespace CoravelWindowsService.Invocables;

public class DailyAtInvocableJob : InvocableJob
{
    private readonly DailyAtJobDefinition _jobDefinition = new();

    public DailyAtInvocableJob(DailyAtJobDefinition jobDefinition)
    {
        _jobDefinition = jobDefinition;
    }

    public override Task Invoke()
    {
        _logger.Info($"Job {_jobDefinition.Name} was invoked.");
        return Task.CompletedTask;
    }
}
