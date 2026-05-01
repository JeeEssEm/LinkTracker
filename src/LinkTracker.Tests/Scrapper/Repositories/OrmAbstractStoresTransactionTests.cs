using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LinkTracker.Tests.Scrapper.Repositories;

public class OrmAbstractStoresTransactionTests(ScrapperWebApplicationFactory factory)
    : IClassFixture<ScrapperWebApplicationFactory>, IAsyncLifetime
{
    private AsyncServiceScope _scope;

    private static long _idSeed = 200_000;
    private static long NextId() => Interlocked.Increment(ref _idSeed);

    private IUnitOfWork Uow => _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    private IChatRepository ChatRepo => _scope.ServiceProvider.GetRequiredService<IChatRepository>();
    private ILinkRepository LinkRepo => _scope.ServiceProvider.GetRequiredService<ILinkRepository>();
    private IGithubStore GithubStore => _scope.ServiceProvider.GetRequiredService<IGithubStore>();
    private IGithubRepository GithubRepo => _scope.ServiceProvider.GetRequiredService<IGithubRepository>();
    private IStackOverflowStore StackOverflowStore => _scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();
    private IStackOverflowRepository StackOverflowRepo => _scope.ServiceProvider.GetRequiredService<IStackOverflowRepository>();

    public Task InitializeAsync()
    {
        var testHost = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DatabaseOptions:AccessType", "SQL");
        });

        _ = testHost.CreateClient();
        _scope = testHost.Services.CreateAsyncScope();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
    }

    [Fact]
    public async Task StackOverflowStore_Rollback_DoesNotPersistQuestion()
    {
        var chatId = new ChatId(NextId());
        var questionId = NextId();

        await ChatRepo.AddChatAsync(chatId);
        var link = await LinkRepo.GetOrCreateAsync(new Uri($"https://stackoverflow.com/questions/{questionId}"), LinkSourceType.StackOverflow);

        await Uow.BeginAsync();
        await StackOverflowRepo.CreateQuestionAsync(link.Id, questionId);
        await Uow.RollbackAsync();

        var mapping = await StackOverflowStore.GetQuestionIdsByLinkIdsAsync([link.Id.Value]);
        Assert.False(mapping.ContainsKey(link.Id.Value));
    }

    [Fact]
    public async Task StackOverflowStore_Commit_PersistsQuestion()
    {
        var chatId = new ChatId(NextId());
        var questionId = NextId();

        await ChatRepo.AddChatAsync(chatId);
        var link = await LinkRepo.GetOrCreateAsync(new Uri($"https://stackoverflow.com/questions/{questionId}"), LinkSourceType.StackOverflow);

        await Uow.BeginAsync();
        await StackOverflowRepo.CreateQuestionAsync(link.Id, questionId);
        await Uow.CommitAsync();

        var mapping = await StackOverflowStore.GetQuestionIdsByLinkIdsAsync([link.Id.Value]);
        Assert.True(mapping.ContainsKey(link.Id.Value));
        Assert.Equal(questionId, mapping[link.Id.Value]);
    }

    [Fact]
    public async Task GithubStore_Rollback_DoesNotPersistRepository()
    {
        var chatId = new ChatId(NextId());
        var owner = $"owner-{NextId()}";
        var repo = $"repo-{NextId()}";

        await ChatRepo.AddChatAsync(chatId);
        var link = await LinkRepo.GetOrCreateAsync(new Uri($"https://github.com/{owner}/{repo}"), LinkSourceType.Github);

        await Uow.BeginAsync();
        await GithubRepo.CreateAsync(link.Id, repo, owner);
        await Uow.RollbackAsync();

        var mapping = await GithubStore.GetRepositoriesByLinkIdsAsync([link.Id.Value]);
        Assert.False(mapping.ContainsKey(link.Id.Value));
    }

    [Fact]
    public async Task GithubStore_Commit_PersistsRepository()
    {
        var chatId = new ChatId(NextId());
        var owner = $"owner-{NextId()}";
        var repo = $"repo-{NextId()}";

        await ChatRepo.AddChatAsync(chatId);
        var link = await LinkRepo.GetOrCreateAsync(new Uri($"https://github.com/{owner}/{repo}"), LinkSourceType.Github);

        await Uow.BeginAsync();
        await GithubRepo.CreateAsync(link.Id, repo, owner);
        await Uow.CommitAsync();

        var mapping = await GithubStore.GetRepositoriesByLinkIdsAsync([link.Id.Value]);
        Assert.True(mapping.ContainsKey(link.Id.Value));
    }
}
