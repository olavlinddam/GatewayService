namespace GatewayService.Models.DTOs;

public class TestObjectDto
{
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string SerialNr { get; set; }
        public Guid MachineId { get; set; }
        public string ImagePath { get; set; }
        public List<SniffingPointDto> SniffingPoints { get; set; }
        public Dictionary<string, string>? Links { get; set; }
}
