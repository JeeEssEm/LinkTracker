namespace AiAgentService.Web.Configurations;

public class PrioritizationConfiguration
{
    public const string Position = "Prioritization";
    public List<string> HighKeywords { get; set; } = [];
    public List<string> LowKeywords { get; set; } = [];
    public bool AiPrioritizationEnabled { get; set; } = false;
    public string Model { get; set; } = "meta-llama/llama-3.3-70b-instruct";
    public string Prompt { get; set; } =
        """
        # Context
        You are expert in prioritization. You have lists specific words by which you identify priority of the message.
        If a message contains at least 1 word from one of these lists, then if should be marked properly.
        If it does not contain any word from any list, then it should be marked as `Medium`.
        
        ## Words by which you identify `Low` priority:
        {0}
        
        ## Words by which you identify `High` priority:
        {1}
        
        # Examples
        
        ## Example 1
        Text: "critical bug fix"
        You should mark this as `High` priority.
        
        ## Example 2
        Text: "fix typo in readme"
        You should mark this as `Low` priority.
        
        # Rules
        You must return only 1 word - priority without any formatting
        """;
    public int DegreeOfParallelism { get; set; } = 1;
    public int BoundedCapacity { get; set; } = 10;
}
