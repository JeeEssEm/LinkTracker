namespace AiAgentService.Web.Configurations;

public class GroupingConfiguration
{
    public const string Position = "Grouping";
    public int WindowMs { get; set; } = 3000;
    public int MaxGroupBatchSize { get; set; } = 50;
}