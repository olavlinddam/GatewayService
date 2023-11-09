namespace GatewayService.Models.DTOs;

public class LeakTestDto
{
    public DateTime TimeStamp { get; set; } 
    public Guid TestObjectId { get; set; }
    public string Status { get; set; } 
    public Guid MachineId { get; set;  } 
    public string TestObjectType { get; set; } 
    public string User { get; set; } 
    public Guid SniffingPoint { get; set; } 
    public string? Reason { get; set; } 
    public Guid? LeakTestId { get; set; }
    public string? Measurement { get; set; } 
    public Dictionary<string, string>? Links { get; set; }
}