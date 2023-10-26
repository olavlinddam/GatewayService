namespace GatewayService.Models.DTOs;

public class SniffingPoint
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}