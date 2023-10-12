using System.Text;
using GatewayService.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace GatewayService.Services;

public class LeakTestProducer : IProducer, IDisposable
{
    private readonly LeakTestServiceConfig _config;
    private readonly IModel _channel;
    private readonly QueueDeclareOk _replyQueue;

    public LeakTestProducer(IOptions<LeakTestServiceConfig> config)
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
        _replyQueue = _channel.QueueDeclare(queue: "", exclusive: true);
    }

    public void SendMessage<T>(T message, string routingKey)
    {
        // Preparing the message to be send
        var json = JsonConvert.SerializeObject(message, Formatting.Indented);
        var requestBody = Encoding.UTF8.GetBytes(json);

        // Setting the properties required for the server to know how to reply to the request. 
        var properties = _channel.CreateBasicProperties();
        properties.ReplyTo = _replyQueue.QueueName;
        properties.CorrelationId = Guid.NewGuid().ToString();
        
        _channel.BasicPublish(
            exchange: "leaktest-exchange",
            routingKey: "request-queue", 
            basicProperties: properties, 
            body: requestBody);
        
        // Log that we published a request here. 
        Console.WriteLine($"Sending request: {properties.CorrelationId}");
        Console.WriteLine(Encoding.UTF8.GetString(requestBody));
    }

    public void Dispose()
    {
        _channel.Close();
        _channel.Dispose();
    }
}