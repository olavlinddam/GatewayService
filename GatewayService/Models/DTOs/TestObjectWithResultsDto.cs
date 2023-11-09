namespace GatewayService.Models.DTOs;

public class TestObjectWithResultsDto
{
    public TestObjectDto? TestObjectDto { get; set; }
    public List<LeakTestDto?> LeakTestDto { get; set; }
}