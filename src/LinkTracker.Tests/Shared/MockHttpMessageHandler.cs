namespace LinkTracker.Tests.Shared;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendLambda;

    public void SetSendLambda(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> predicate)
    {
        _sendLambda = predicate;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _sendLambda(request, cancellationToken);
    }
}