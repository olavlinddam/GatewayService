namespace GatewayService.Services;

public interface IConsumer : IDisposable
{
    public void StartListening();
}