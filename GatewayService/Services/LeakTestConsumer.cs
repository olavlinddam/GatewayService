using System.Text;
using GatewayService.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GatewayService.Services;

public class LeakTestConsumer : IConsumer, IDisposable
{
    private readonly LeakTestServiceConfig _config;
    private readonly IModel _channel;
    private readonly QueueDeclareOk _replyQueue;


    public LeakTestConsumer(IOptions<LeakTestServiceConfig> config)
    {
        _config = config.Value;
        
        var factory = new ConnectionFactory
        {
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,
            HostName = _config.HostName,
            Port = int.Parse(_config.Port),
            ClientProvidedName = _config.ClientProvidedName
        };
        
        var connection = factory.CreateConnection();
        _channel = connection.CreateModel();
        
        _replyQueue = _channel.QueueDeclare
        (
            queue: "leaktest-request-queue", 
            durable: false, 
            exclusive: false, 
            autoDelete: false, 
            arguments: null
        );
    }
    
    public void Listen()
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var reply = Encoding.UTF8.GetString(body);
            Console.WriteLine($"Received new message: {reply}");
            
            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };
        _channel.BasicConsume(queue: _replyQueue.QueueName, autoAck: false, consumer: consumer);
    }
    
    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
    }
}