using System.Net;

namespace TheCanonry.ApiClients.Tests;

/// <summary>
/// Test helper that returns pre-built HTTP responses. Captures the request for assertions.
/// Supports sequential responses for multi-step workflows (submit -> poll -> download).
/// </summary>
internal sealed class MockHandler : DelegatingHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
    private readonly List<HttpRequestMessage> _requests = [];

    /// <summary>All requests sent through this handler, in order.</summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    /// <summary>Content of the first request, read as a string (call after the client method completes).</summary>
    public string? FirstRequestBody { get; private set; }

    /// <summary>
    /// Enqueue a response that will be returned for the next request (FIFO order).
    /// </summary>
    public MockHandler Respond(HttpStatusCode status, string content, string contentType = "application/json")
    {
        _responses.Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, contentType),
        });
        return this;
    }

    /// <summary>
    /// Enqueue a response with byte[] content (for image downloads).
    /// </summary>
    public MockHandler RespondBytes(HttpStatusCode status, byte[] data, string contentType = "image/png")
    {
        _responses.Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = new ByteArrayContent(data) { Headers = { { "Content-Type", contentType } } },
        });
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);

        // Capture first request body
        if (_requests.Count == 1 && request.Content is not null)
        {
            FirstRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        if (_responses.Count == 0)
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("No mock response configured"),
            };
        }

        var factory = _responses.Dequeue();
        return factory(request);
    }
}
