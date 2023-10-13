using System.Collections.Concurrent;

namespace GatewayService.Models;

public class PendingRequestManager
{
    private readonly ConcurrentDictionary<string, PendingRequest> _pendingRequests = new();

    public PendingRequest CreateRequest()
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<string>();
        var request = new PendingRequest { CorrelationId = correlationId, CompletionSource = tcs };
        _pendingRequests.TryAdd(correlationId, request);
        return request;
    }

    public bool TryCompleteRequest(string correlationId, string response)
    {
        if (_pendingRequests.TryRemove(correlationId, out var request))
        {
            request.CompletionSource.SetResult(response);
            return true;
        }
        return false;
    }
}