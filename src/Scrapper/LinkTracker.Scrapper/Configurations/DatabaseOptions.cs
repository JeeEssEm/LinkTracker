namespace LinkTracker.Scrapper.Configurations;

public class DatabaseOptions
{
    public const string Position = nameof(DatabaseOptions);
    public required string ConnectionString { get; set; }
    public required string AccessType { get; set; }
}
