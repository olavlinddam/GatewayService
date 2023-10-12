using System.Text;
using GatewayService.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GatewayService.Services.LeakTestService.Producers;

public class AddSingleProducer : IProducer
{
    private readonly RabbitMqConfig _config;

    public AddSingleProducer(RabbitMqConfig config)
    {
        _config = config;
    }

    public void SendMessage<T>(T message, string routingKey)
    {
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
        
        using var channel = connection.CreateModel();
        
        // ------ This part is setting up the listening for replies. -------
        
        // The reply queue that we want the server to reply on. RMQ will set up the queue name so we leave it blank. 
        var replyQueue = channel.QueueDeclare(queue: "leaktest-response-queue", exclusive: true);

        // The request queue that we are going to be sending requests to. 
        channel.QueueDeclare(queue: "leaktest-request-queue", exclusive: false);

        // Setting up the consumer to consume the reply queue
        var consumer = new AsyncEventingBasicConsumer(channel);

        // Receiving the message and handling what it needs to do. It is async because the operation it has to call is
        // going to be an async operation, that needs to be awaited. 
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var reply = Encoding.UTF8.GetString(body);
            Console.WriteLine($"Received new message: {reply}");
        };

        channel.BasicConsume(queue: replyQueue.QueueName, autoAck: true, consumer: consumer);
        
        
        // ------- This part is going to publish the request onto the request queue. ---------
        
        // Preparing the message to be send
        var json = JsonConvert.SerializeObject(message, Formatting.Indented);
        var requestBody = Encoding.UTF8.GetBytes(json);

        // Setting the properties required for the server to know how to reply to the request. 
        var properties = channel.CreateBasicProperties();
        properties.ReplyTo = replyQueue.QueueName;
        properties.CorrelationId = Guid.NewGuid().ToString();
        
        channel.BasicPublish(
            exchange: _config.ExchangeName,
            routingKey: routingKey, 
            basicProperties: properties, 
            body: requestBody);
        
        // Log that we published a request here. 
        Console.WriteLine($"Sending request: {properties.CorrelationId}");
        
    }
    
}