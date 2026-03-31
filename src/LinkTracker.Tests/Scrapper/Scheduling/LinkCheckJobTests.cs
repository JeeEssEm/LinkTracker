using LinkTracker.Scrapper.ApiClients;
using LinkTracker.Scrapper.ApiClients.Abstractions;
using LinkTracker.Scrapper.ApiClients.BotNotifier;
using LinkTracker.Scrapper.ApiClients.Shared;
using LinkTracker.Scrapper.Domain;
using LinkTracker.Scrapper.Features.Shared.Abstractions;
using LinkTracker.Scrapper.Scheduling;
using FluentResults;
using LinkTracker.Scrapper.ApiClients.BotNotifier.Rest;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;

namespace LinkTracker.Tests.Scrapper.Scheduling;

public class LinkCheckJobTests
{
    private readonly ILinkStore _linkStore;
    private readonly IBotClient _botClient;
    private readonly LinkCheckerResolver _resolver;
    private readonly ILinkChecker _checker;
    private readonly LinkCheckJob _job;
    private readonly IJobExecutionContext _context;

    public LinkCheckJobTests()
    {
        _linkStore = Substitute.For<ILinkStore>();
        _botClient = Substitute.For<IBotClient>();
        _checker = Substitute.For<ILinkChecker>();
        _checker.Key.Returns("github-repository");
        _checker.CanHandle(Arg.Any<Uri>()).Returns(true);

        _resolver = new LinkCheckerResolver([_checker]);
        var logger = Substitute.For<ILogger<LinkCheckJob>>();

        _job = new LinkCheckJob(_linkStore, _resolver, _botClient, logger);
        _context = Substitute.For<IJobExecutionContext>();
    }

    [Fact]
    public async Task Execute_UpdateFound_NotifiesBot()
    {
        var chat = new Chat(1);
        var link = new Link(1, "https://github.com/user/repo", "github-repository", [], [], chat);

        _linkStore.GetAllLinksAsync().Returns([link]);
        _linkStore.UpdateLastCheckedAt(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<DateTimeOffset>())
            .Returns(Result.Ok(link));

        _checker.CheckAsync(Arg.Any<Uri>(), Arg.Any<DateTimeOffset?>())
            .Returns(new UpdateCheckResult(true, DateTimeOffset.UtcNow, "Updated"));

        await _job.Execute(_context);

        await _botClient.Received(1).SendUpdateAsync(Arg.Is<LinkUpdateRequest>(
            r => r.Url == "https://github.com/user/repo" && r.TgChatIds.Contains(1)));
    }

    [Fact]
    public async Task Execute_NoUpdate_DoesNotNotifyBot()
    {
        var chat = new Chat(1);
        var link = new Link(1, "https://github.com/user/repo", "github-repository", [], [], chat, DateTimeOffset.UtcNow);

        _linkStore.GetAllLinksAsync().Returns([link]);
        _linkStore.UpdateLastCheckedAt(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<DateTimeOffset>())
            .Returns(Result.Ok(link));

        _checker.CheckAsync(Arg.Any<Uri>(), Arg.Any<DateTimeOffset?>())
            .Returns(new UpdateCheckResult(false, null, ""));

        await _job.Execute(_context);

        await _botClient.DidNotReceive().SendUpdateAsync(Arg.Any<LinkUpdateRequest>());
    }

    [Fact]
    public async Task Execute_OnlySubscribedUsersReceiveUpdate()
    {
        var chat1 = new Chat(1);
        var chat2 = new Chat(2);
        var chat3 = new Chat(3);

        var link1 = new Link(1, "https://github.com/user/repo", "github-repository", [], [], chat1);
        var link2 = new Link(2, "https://github.com/user/repo", "github-repository", [], [], chat2);
        var link3 = new Link(3, "https://github.com/other/project", "github-repository", [], [], chat3);

        _linkStore.GetAllLinksAsync().Returns([link1, link2, link3]);
        _linkStore.UpdateLastCheckedAt(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<DateTimeOffset>())
            .Returns(ci =>
            {
                var url = ci.ArgAt<string>(0);
                var ownerId = ci.ArgAt<long>(1);
                var links = new[] { link1, link2, link3 };
                var found = links.First(l => l.Url == url && l.Owner.Id == ownerId);
                return Result.Ok(found);
            });

        _checker.CheckAsync(
            Arg.Is<Uri>(u => u.ToString() == "https://github.com/user/repo"),
            Arg.Any<DateTimeOffset?>())
            .Returns(new UpdateCheckResult(true, DateTimeOffset.UtcNow, "repo updated"));

        _checker.CheckAsync(
            Arg.Is<Uri>(u => u.ToString() == "https://github.com/other/project"),
            Arg.Any<DateTimeOffset?>())
            .Returns(new UpdateCheckResult(false, null, ""));

        await _job.Execute(_context);

        await _botClient.Received().SendUpdateAsync(Arg.Is<LinkUpdateRequest>(
            r => r.Url == "https://github.com/user/repo"
                 && r.TgChatIds.Contains(1)
                 && r.TgChatIds.Contains(2)
                 && !r.TgChatIds.Contains(3)));

        await _botClient.DidNotReceive().SendUpdateAsync(Arg.Is<LinkUpdateRequest>(
            r => r.Url == "https://github.com/other/project"));
    }

    [Fact]
    public async Task Execute_InvalidUrl_SkipsLink()
    {
        var chat = new Chat(1);
        var link = new Link(1, "not-a-valid-url", "github-repository", [], [], chat);

        _linkStore.GetAllLinksAsync().Returns([link]);

        await _job.Execute(_context);

        await _botClient.DidNotReceive().SendUpdateAsync(Arg.Any<LinkUpdateRequest>());
    }

    [Fact]
    public async Task Execute_UnknownCheckerKey_SkipsLink()
    {
        var chat = new Chat(1);
        var link = new Link(1, "https://github.com/user/repo", "unknown-checker", [], [], chat);

        _linkStore.GetAllLinksAsync().Returns([link]);

        await _job.Execute(_context);

        await _botClient.DidNotReceive().SendUpdateAsync(Arg.Any<LinkUpdateRequest>());
    }

    [Fact]
    public async Task Execute_CheckerThrows_ContinuesWithNextLink()
    {
        var chat = new Chat(1);
        var link1 = new Link(1, "https://github.com/user/repo1", "github-repository", [], [], chat);
        var link2 = new Link(2, "https://github.com/user/repo2", "github-repository", [], [], chat);

        _linkStore.GetAllLinksAsync().Returns([link1, link2]);
        _linkStore.UpdateLastCheckedAt(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<DateTimeOffset>())
            .Returns(Result.Ok(link2));

        _checker.CheckAsync(
            Arg.Is<Uri>(u => u.AbsolutePath.Contains("repo1")),
            Arg.Any<DateTimeOffset?>())
            .Returns<UpdateCheckResult>(_ => throw new HttpRequestException("API error"));

        _checker.CheckAsync(
            Arg.Is<Uri>(u => u.AbsolutePath.Contains("repo2")),
            Arg.Any<DateTimeOffset?>())
            .Returns(new UpdateCheckResult(true, DateTimeOffset.UtcNow, "Updated"));

        await _job.Execute(_context);

        await _botClient.Received().SendUpdateAsync(Arg.Is<LinkUpdateRequest>(
            r => r.Url == "https://github.com/user/repo2"));
    }

    [Fact]
    public async Task Execute_BotNotificationFails_DoesNotCrash()
    {
        var chat = new Chat(1);
        var link = new Link(1, "https://github.com/user/repo", "github-repository", [], [], chat);

        _linkStore.GetAllLinksAsync().Returns([link]);
        _linkStore.UpdateLastCheckedAt(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<DateTimeOffset>())
            .Returns(Result.Ok(link));

        _checker.CheckAsync(Arg.Any<Uri>(), Arg.Any<DateTimeOffset?>())
            .Returns(new UpdateCheckResult(true, DateTimeOffset.UtcNow, "Updated"));

        _botClient.SendUpdateAsync(Arg.Any<LinkUpdateRequest>()).Returns(Result.Fail("any error"));

        await _job.Execute(_context);

        await _linkStore.Received().UpdateLastCheckedAt(
            "https://github.com/user/repo", 1, Arg.Any<DateTimeOffset>());
    }
}
