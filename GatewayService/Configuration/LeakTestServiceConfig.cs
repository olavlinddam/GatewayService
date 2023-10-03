namespace GatewayService.Configuration;

public class LeakTestServiceConfig : RabbitMqConfig
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; }
    public string HostName { get; set; }
    public string Port { get; set; }
    public string ClientProvidedName { get; set; }
    public string ExchangeName { get; set; }
    public string RoutingKey { get; set; }
    public string RequestQueue { get; set; }
    public string ResponseQueue { get; set; }
}