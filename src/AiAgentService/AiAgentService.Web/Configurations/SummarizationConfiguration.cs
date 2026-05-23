namespace AiAgentService.Web.Configurations;

public class SummarizationConfiguration
{
    public int Threshold { get; set; } = 200;
    public bool AiSummarizationEnabled { get; set; } = false;
    public string Model { get; set; } = "meta-llama/llama-3.3-70b-instruct";
    public string SummarizationPrompt { get; set; } = "Summarize the following update in 2–3 sentences";

    public float Temperature { get; set; } = 0.3f;
    public int MaxTokens { get; set; } = 8192;
    public int NumberOfParallelRequests { get; set; } = 1;
    public int BoundedCapacity { get; set; } = 5;
}
