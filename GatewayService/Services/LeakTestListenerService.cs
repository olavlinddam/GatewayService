namespace GatewayService.Services;

public class LeakTestListenerService : BackgroundService
{
    private readonly IConsumer _leakTestConsumer;


    public LeakTestListenerService(IConsumer leakTestConsumer)
    {
        _leakTestConsumer = leakTestConsumer;
        _leakTestConsumer.StartListening();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            // Logging and cleanup logic here
        });
        
        Console.WriteLine("Listening");
        
        // Start listening for messages
        _leakTestConsumer.StartListening();

        // Wait untill the service stops. 
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _leakTestConsumer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}