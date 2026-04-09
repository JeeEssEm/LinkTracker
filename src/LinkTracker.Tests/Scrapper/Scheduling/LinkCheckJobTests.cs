using LinkTracker.Scrapper.ApiClients;
using LinkTracker.Scrapper.ApiClients.Abstractions;
using LinkTracker.Scrapper.ApiClients.BotNotifier;
using LinkTracker.Scrapper.ApiClients.Shared;
using LinkTracker.Scrapper.Domain;
using LinkTracker.Scrapper.Scheduling;
using FluentResults;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.ApiClients.BotNotifier.Rest;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Configurations;
using LinkTracker.Scrapper.Domain.Chats.Entities;
using LinkTracker.Scrapper.Domain.Links.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Quartz;

namespace LinkTracker.Tests.Scrapper.Scheduling;

public class LinkCheckJobTests
{
    private readonly ILinkRepository _linkStore;
    private readonly IBotClient _botClient;
    private readonly LinkCheckerResolver _resolver;
    private readonly ILinkChecker _checker;
    private readonly LinkCheckJob _job;
    private readonly IJobExecutionContext _context;
    private readonly IOptions<SchedulerOptions> _schedulerOptions;
    private readonly ILogger<LinkCheckJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public LinkCheckJobTests()
    {
        _linkStore = Substitute.For<ILinkRepository>();
        _botClient = Substitute.For<IBotClient>();
        _checker = Substitute.For<ILinkChecker>();
        _schedulerOptions = Substitute.For<IOptions<SchedulerOptions>>();
        _logger = Substitute.For<ILogger<LinkCheckJob>>();

        _schedulerOptions.Value.Returns(new SchedulerOptions());
        _checker.Key.Returns("github-repository");
        _checker.CanHandle(Arg.Any<Uri>()).Returns(true);

        _resolver = new LinkCheckerResolver([_checker]);

        var services = new ServiceCollection();
        services.AddScoped<LinkCheckJob>();
        services.AddScoped<ILinkRepository>(_ => _linkStore);
        services.AddScoped<IBotClient>(_ => _botClient);
        services.AddScoped<LinkCheckerResolver>(_ => _resolver);

        var serviceProvider = services.BuildServiceProvider();
        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _job = new LinkCheckJob(_linkStore, _serviceScopeFactory, _schedulerOptions, _logger);
        _context = Substitute.For<IJobExecutionContext>();
    }

    [Fact]
    public async Task Execute_UpdateFound_NotifiesBot()
    {
        var link = new Link(1, "https://github.com/user/repo", "github-repository");

        _linkStore.GetLinksToCheck(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>()).Returns([]);
        _linkStore.GetLinksToCheck(1, Arg.Any<int>(), Arg.Any<DateTime>()).Returns([new LinkCheckEntry(1, link.Url, link.CheckerKey, DateTime.UtcNow, [1])]);

        _checker.CheckAsync(Arg.Any<Uri>(), Arg.Any<DateTime?>())
            .Returns(new UpdateCheckResult(true, DateTime.UtcNow, "Updated"));

        await _job.Execute(_context);

        await _botClient.Received(1).SendUpdateAsync(Arg.Is<LinkUpdateRequest>(r => r.Url == "https://github.com/user/repo" && r.TgChatIds.Contains(1)));
    }

    [Fact]
    public async Task Execute_NoUpdate_DoesNotNotifyBot()
    {
        var link = new Link(1, "https://github.com/user/repo", "github-repository");

        _linkStore.GetLinksToCheck(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>()).Returns([]);
        _linkStore.GetLinksToCheck(1, Arg.Any<int>(), DateTime.UtcNow).Returns([new LinkCheckEntry(1, link.Url, link.CheckerKey, DateTime.UtcNow, [])]);
        await _linkStore.UpdateCheckDatesAt(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>());

        _checker.CheckAsync(Arg.Any<Uri>(), Arg.Any<DateTime?>())
            .Returns(new UpdateCheckResult(false, null, ""));

        await _job.Execute(_context);

        await _botClient.DidNotReceive().SendUpdateAsync(Arg.Any<LinkUpdateRequest>());
    }

    [Fact]
    public async Task Execute_OnlySubscribedUsersReceiveUpdate()
    {
        var link1 = new Link(1, "https://github.com/user/repo", "github-repository");
        var link3 = new Link(3, "https://github.com/other/project", "github-repository");

        _linkStore.GetLinksToCheck(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>()).Returns([]);
        _linkStore.GetLinksToCheck(1, Arg.Any<int>(), Arg.Any<DateTime>()).Returns([
            new LinkCheckEntry(1, link1.Url, link1.CheckerKey, DateTime.UtcNow, [1, 2]),
            new LinkCheckEntry(3, link3.Url, link3.CheckerKey, DateTime.UtcNow, [3])
        ]);

        _linkStore.UpdateCheckDatesAt(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(ci => Task.CompletedTask);

        _checker.CheckAsync(
                Arg.Is<Uri>(u => u.ToString() == "https://github.com/user/repo"),
                Arg.Any<DateTime?>())
            .Returns(new UpdateCheckResult(true, DateTime.UtcNow, "repo updated"));

        _checker.CheckAsync(
                Arg.Is<Uri>(u => u.ToString() == "https://github.com/other/project"),
                Arg.Any<DateTime?>())
            .Returns(new UpdateCheckResult(false, null, ""));

        await _job.Execute(_context);

        await _botClient.Received().SendUpdateAsync(Arg.Is<LinkUpdateRequest>(r => r.Url == "https://github.com/user/repo"
                                                                                   && r.TgChatIds.Contains(1)
                                                                                   && r.TgChatIds.Contains(2)
                                                                                   && !r.TgChatIds.Contains(3)));

        await _botClient.DidNotReceive().SendUpdateAsync(Arg.Is<LinkUpdateRequest>(r => r.Url == "https://github.com/other/project"));
    }

    [Fact]
    public async Task Execute_InvalidUrl_SkipsLink()
    {
        var link = new Link(1, "not-a-valid-url", "github-repository");

        _linkStore.GetLinksToCheck(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>()).Returns([]);
        _linkStore.GetLinksToCheck(1, Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns([new LinkCheckEntry(1, link.Url, link.CheckerKey, DateTime.UtcNow, [1])]);

        await _job.Execute(_context);

        await _botClient.DidNotReceive().SendUpdateAsync(Arg.Any<LinkUpdateRequest>());
    }

    [Fact]
    public async Task Execute_UnknownCheckerKey_SkipsLink()
    {
        var link = new Link(1, "https://github.com/user/repo", "unknown-checker");

        _linkStore.GetLinksToCheck(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>()).Returns([]);
        _linkStore.GetLinksToCheck(1, Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns([new LinkCheckEntry(1, link.Url, link.CheckerKey, DateTime.UtcNow, [])]);

        await _job.Execute(_context);

        await _botClient.DidNotReceive().SendUpdateAsync(Arg.Any<LinkUpdateRequest>());
    }

    [Fact]
    public async Task Execute_CheckerThrows_ContinuesWithNextLink()
    {
        var link1 = new Link(1, "https://github.com/user/repo1", "github-repository");
        var link2 = new Link(2, "https://github.com/user/repo2", "github-repository");

        _linkStore.GetLinksToCheck(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>()).Returns([]);
        _linkStore.GetLinksToCheck(1, Arg.Any<int>(), Arg.Any<DateTime>()).Returns(
            [
                new LinkCheckEntry(1, link1.Url, link1.CheckerKey, DateTime.UtcNow, []),
                new LinkCheckEntry(2, link2.Url, link2.CheckerKey, DateTime.UtcNow, [])
            ]
        );

        _linkStore.UpdateCheckDatesAt(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(_ => Task.CompletedTask);

        _checker.CheckAsync(
                Arg.Is<Uri>(u => u.AbsolutePath.Contains("repo1")),
                Arg.Any<DateTime?>())
            .Returns(_ => Result.Fail("some api error"));

        _checker.CheckAsync(
                Arg.Is<Uri>(u => u.AbsolutePath.Contains("repo2")),
                Arg.Any<DateTime?>())
            .Returns(new UpdateCheckResult(true, DateTime.UtcNow, "Updated"));

        await _job.Execute(_context);

        await _botClient.Received().SendUpdateAsync(Arg.Is<LinkUpdateRequest>(r => r.Url == "https://github.com/user/repo2"));
    }

    [Fact]
    public async Task Execute_BotNotificationFails_DoesNotCrash()
    {
        var link = new Link(1, "https://github.com/user/repo", "github-repository");

        _linkStore.GetLinksToCheck(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>()).Returns([]);
        _linkStore.GetLinksToCheck(1, Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns([new LinkCheckEntry(1, link.Url, link.CheckerKey, DateTime.UtcNow, [1])]);

        _linkStore.UpdateCheckDatesAt(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(_ => Task.CompletedTask);

        _checker.CheckAsync(Arg.Any<Uri>(), Arg.Any<DateTime?>())
            .Returns(new UpdateCheckResult(true, DateTime.UtcNow, "Updated"));

        _botClient.SendUpdateAsync(Arg.Any<LinkUpdateRequest>()).Returns(Result.Fail("any error"));

        await _job.Execute(_context);

        await _linkStore.Received().UpdateCheckDatesAt(1, Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }
}