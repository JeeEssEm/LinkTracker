namespace AiAgentService.Web.Configurations;

public class AiApiConfiguration
{
    public const string Position = nameof(AiApiConfiguration);

    public required string Url { get; set; }
    public required string ApiKey { get; set; }
}