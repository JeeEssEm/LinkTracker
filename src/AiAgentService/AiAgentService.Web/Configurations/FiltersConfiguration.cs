using AiAgentService.Web.Pipeline.Handlers.Summarization;

namespace AiAgentService.Web.Configurations;

public class FiltersConfiguration
{
    public const string Position = "Filtering";
    public List<string> StopWords { get; set; } = new();
    public List<string> ExcludedAuthors { get; set; } = new();
    public int MinLength { get; set; } = 20;
    public SummarizationConfiguration Summarization { get; set; } = new();
}
