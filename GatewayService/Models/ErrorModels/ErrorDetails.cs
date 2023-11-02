namespace GatewayService.Models.ErrorModels;

public class ErrorDetails
{
    public string Name { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public string Item { get; set; }
    public string Id { get; set; }
}
