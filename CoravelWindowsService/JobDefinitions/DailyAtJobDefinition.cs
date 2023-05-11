namespace CoravelWindowsService.JobDefinitions;

public class DailyAtJobDefinition : Job
{
    public int AtHour { get; set; }
    public int AtMinute { get; set; }
}
