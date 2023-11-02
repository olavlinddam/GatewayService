namespace GatewayService.Services.RabbitMq;

public interface IProducer
{ 
    public Task<string> SendMessage < T > (T message, string queueName, string routingKey);
}