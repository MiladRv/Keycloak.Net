using System.Net;
using System.Text;

namespace Keycloak.Net.Sdk.UnitTests.Helpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    public List<HttpRequestMessage> SentRequests { get; } = [];

    public void AddResponse(HttpStatusCode statusCode, string? jsonContent = null)
    {
        var message = new HttpResponseMessage(statusCode);
        if (jsonContent != null)
            message.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        _responses.Enqueue(message);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        if (!_responses.TryDequeue(out var response))
            throw new InvalidOperationException("No more responses configured for this test.");
        return Task.FromResult(response);
    }
}
