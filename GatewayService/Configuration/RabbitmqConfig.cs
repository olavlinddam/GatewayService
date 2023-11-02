namespace GatewayService.Configuration;

public class RabbitMqConfig
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; }
    public string HostName { get; set; }
    public string Port { get; set; }
    public string ClientProvidedName { get; set; }
}