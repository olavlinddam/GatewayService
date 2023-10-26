namespace GatewayService.Models.DTOs;

public class TestObjectDto
{
    public Guid Id { get; set; }
    public List<SniffingPoint> SniffingPoints { get; set; }
    public string Type { get; set; }
    public string ImageUrl { get; set; }
    public string SerialNumber { get; set; }
}