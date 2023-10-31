namespace GatewayService.Models.DTOs;

public class SniffingPointDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public Guid? TestObjectId { get; set; }
}