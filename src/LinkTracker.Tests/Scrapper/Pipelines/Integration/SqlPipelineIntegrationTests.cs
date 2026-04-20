namespace LinkTracker.Tests.Scrapper.Pipelines.Integration;

public class SqlPipelineIntegrationTests(ScrapperWebApplicationFactory factory)
    : BasePipelineIntegrationTests(factory, "SQL");
