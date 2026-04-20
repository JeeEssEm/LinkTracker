using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.LinkCheckers;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Domain.Chats.Entities;
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
    private IStackOverflowStore StackOverflowStore => _scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();

    public Task InitializeAsync()
    {
        var testHost = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DatabaseOptions:AccessType", "ORM");
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
        var chatId = NextId();
        var questionId = NextId();

        await ChatRepo.AddChatAsync(new Chat(chatId));
        var link = await LinkRepo.CreateLink($"https://stackoverflow.com/questions/{questionId}", StackOverflowQuestionChecker.ConstKey);

        await Uow.BeginAsync();
        await StackOverflowStore.CreateQuestion(link.Id, questionId);
        await Uow.RollbackAsync();

        var mapping = await StackOverflowStore.GetQuestionIdsByLinkIdsAsync([link.Id]);
        Assert.False(mapping.ContainsKey(link.Id));
    }

    [Fact]
    public async Task StackOverflowStore_Commit_PersistsQuestion()
    {
        var chatId = NextId();
        var questionId = NextId();

        await ChatRepo.AddChatAsync(new Chat(chatId));
        var link = await LinkRepo.CreateLink($"https://stackoverflow.com/questions/{questionId}", StackOverflowQuestionChecker.ConstKey);

        await Uow.BeginAsync();
        await StackOverflowStore.CreateQuestion(link.Id, questionId);
        await Uow.CommitAsync();

        var mapping = await StackOverflowStore.GetQuestionIdsByLinkIdsAsync([link.Id]);
        Assert.True(mapping.ContainsKey(link.Id));
        Assert.Equal(questionId, mapping[link.Id]);
    }

    [Fact]
    public async Task GithubStore_Rollback_DoesNotPersistRepository()
    {
        var chatId = NextId();
        var owner = $"owner-{NextId()}";
        var repo = $"repo-{NextId()}";

        await ChatRepo.AddChatAsync(new Chat(chatId));
        var link = await LinkRepo.CreateLink($"https://github.com/{owner}/{repo}", GithubRepositoryChecker.ConstKey);

        await Uow.BeginAsync();
        await GithubStore.CreateRepository(link.Id, repo, owner);
        await Uow.RollbackAsync();

        var mapping = await GithubStore.GetRepositoriesByLinkIdsAsync([link.Id]);
        Assert.False(mapping.ContainsKey(link.Id));
    }

    [Fact]
    public async Task GithubStore_Commit_PersistsRepository()
    {
        var chatId = NextId();
        var owner = $"owner-{NextId()}";
        var repo = $"repo-{NextId()}";

        await ChatRepo.AddChatAsync(new Chat(chatId));
        var link = await LinkRepo.CreateLink($"https://github.com/{owner}/{repo}", GithubRepositoryChecker.ConstKey);

        await Uow.BeginAsync();
        await GithubStore.CreateRepository(link.Id, repo, owner);
        await Uow.CommitAsync();

        var mapping = await GithubStore.GetRepositoriesByLinkIdsAsync([link.Id]);
        Assert.True(mapping.ContainsKey(link.Id));
    }
}
