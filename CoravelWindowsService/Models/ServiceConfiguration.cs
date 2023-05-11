using CoravelWindowsService.JobDefinitions;

namespace CoravelWindowsService.Models;

public class ServiceConfiguration
{
    public List<EverySecondsJobDefinition> EverySecondJobDefinitions { get; set; } = new();
    public List<DailyAtJobDefinition> DailyAtJobDefinitions { get; set; } = new();

}
