using System.ComponentModel.DataAnnotations;

namespace GatewayService.Models;

public class PendingRequest
{
    [Required] public string CorrelationId { get; set; }
    public TaskCompletionSource<string> CompletionSource { get; set; }
}
