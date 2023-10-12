namespace GatewayService.Services;

public interface IProducer
{ 
    public void SendMessage < T > (T message, string routingKey);
}