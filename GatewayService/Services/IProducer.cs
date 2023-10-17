namespace GatewayService.Services;

public interface IProducer
{ 
    public Task<string> SendMessage < T > (T message, string queueName, string routingKey);
}