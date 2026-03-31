using LinkTracker.Scrapper.ApiClients;
using LinkTracker.Scrapper.ApiClients.Abstractions;
using LinkTracker.Scrapper.ApiClients.Shared;
using NSubstitute;

namespace LinkTracker.Tests.Scrapper.ApiClients;

public class LinkCheckerResolverTests
{
    private readonly ILinkChecker _githubChecker;
    private readonly ILinkChecker _stackOverflowChecker;
    private readonly LinkCheckerResolver _resolver;

    public LinkCheckerResolverTests()
    {
        _githubChecker = Substitute.For<ILinkChecker>();
        _githubChecker.Key.Returns("github-repository");
        _githubChecker.CanHandle(Arg.Is<Uri>(u => u.Host == "github.com")).Returns(true);

        _stackOverflowChecker = Substitute.For<ILinkChecker>();
        _stackOverflowChecker.Key.Returns("stackoverflow-question");
        _stackOverflowChecker.CanHandle(Arg.Is<Uri>(u => u.Host == "stackoverflow.com")).Returns(true);

        _resolver = new LinkCheckerResolver([_githubChecker, _stackOverflowChecker]);
    }

    [Fact]
    public void CanHandle_ValidGithubUrl_ReturnsTrue()
    {
        Assert.True(_resolver.CanHandle("https://github.com/user/repo"));
    }

    [Fact]
    public void CanHandle_ValidStackOverflowUrl_ReturnsTrue()
    {
        Assert.True(_resolver.CanHandle("https://stackoverflow.com/questions/12345"));
    }

    [Fact]
    public void CanHandle_UnsupportedUrl_ReturnsFalse()
    {
        Assert.False(_resolver.CanHandle("https://example.com/something"));
    }

    [Fact]
    public void CanHandle_InvalidUrl_ReturnsFalse()
    {
        Assert.False(_resolver.CanHandle("not-a-url"));
    }

    [Fact]
    public void Resolve_ValidGithubUrl_ReturnsGithubChecker()
    {
        var result = _resolver.Resolve("https://github.com/user/repo");

        Assert.True(result.IsSuccess);
        Assert.Equal("github-repository", result.Value.Key);
    }

    [Fact]
    public void Resolve_ValidStackOverflowUrl_ReturnsStackOverflowChecker()
    {
        var result = _resolver.Resolve("https://stackoverflow.com/questions/12345");

        Assert.True(result.IsSuccess);
        Assert.Equal("stackoverflow-question", result.Value.Key);
    }

    [Fact]
    public void Resolve_UnsupportedUrl_ReturnsFail()
    {
        var result = _resolver.Resolve("https://example.com/something");

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Resolve_InvalidUrl_ReturnsFail()
    {
        var result = _resolver.Resolve("not-a-url");

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void GetByKey_ExistingKey_ReturnsChecker()
    {
        var result = _resolver.GetByKey("github-repository");

        Assert.True(result.IsSuccess);
        Assert.Equal("github-repository", result.Value.Key);
    }

    [Fact]
    public void GetByKey_NonExistingKey_ReturnsFail()
    {
        var result = _resolver.GetByKey("unknown-key");

        Assert.True(result.IsFailed);
    }
}
