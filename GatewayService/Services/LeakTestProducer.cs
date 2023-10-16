using System.Text;
using GatewayService.Configuration;
using GatewayService.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GatewayService.Services;

public class LeakTestProducer : IProducer, IDisposable
{
    private readonly LeakTestServiceConfig _config;
    private readonly IModel _channel;

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
        _channel.ExchangeDeclare("leaktest-exchange", "direct", durable: true, autoDelete: false, arguments: null);
        _channel.QueueBind("leaktest-request-queue", "leaktest-exchange", "leaktest-request-queue");
    }

    public Task<string> SendMessage<T>(T message)
    {
        var tcs = new TaskCompletionSource<string>();
        
        var replyQueue = _channel.QueueDeclare("", exclusive:true);
        
        // Preparing the message to be send
        var json = JsonConvert.SerializeObject(message, Formatting.Indented);
        var requestBody = Encoding.UTF8.GetBytes(json);

        // Setting the properties required for the server to know how to reply to the request. 
        var properties = _channel.CreateBasicProperties();
        properties.ReplyTo = replyQueue.QueueName;
        properties.CorrelationId = Guid.NewGuid().ToString();
        
        Console.WriteLine($"Sending request: {properties.CorrelationId}");
        
        _channel.BasicPublish(
            exchange: "leaktest-exchange",
            routingKey: "leaktest-request-queue", 
            basicProperties: properties, 
            body: requestBody);
        
        // Log that we published a request here. 
        
        _channel.QueueDeclare("leaktest-request-queue", exclusive:false);
        
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body);
            Console.WriteLine($"Reply Recieved: {response}");
            
            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            tcs.SetResult(response);
        };
        
        _channel.BasicConsume(queue: replyQueue.QueueName, autoAck: false, consumer: consumer);
        return tcs.Task;
    }

    public void Dispose()
    {
        _channel.Close();
        _channel.Dispose();
    }
}