namespace LinkTracker.Scrapper.Infrastructure.Migrations;


public static class Program
{
    public static void Main()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionString");
        if (connectionString is null)
        {
            throw new ArgumentException("Connection string is not set in environment!");
        }
        DatabaseInitializer.Initialize(connectionString);
    }
}
