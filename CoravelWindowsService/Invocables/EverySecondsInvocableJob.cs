using CoravelWindowsService.JobDefinitions;

namespace CoravelWindowsService.Invocables;

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
