using System.Text;
using GatewayService.Configuration;
using GatewayService.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GatewayService.Services;

public class TestObjectProducer : IProducer, IDisposable
{
    private readonly TestObjectServiceConfig _config;
    private readonly IModel _channel;

    public TestObjectProducer(IOptions<TestObjectServiceConfig> config)
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
        _channel.ExchangeDeclare(_config.ExchangeName, "direct", durable: true, autoDelete: false, arguments: null);
    }

    public Task<string> SendMessage<T>(T message, string queueName, string routingKey)
    {
        _channel.QueueBind(queueName, _config.ExchangeName, routingKey);

        var tcs = new TaskCompletionSource<string>();
        
        var replyQueue = _channel.QueueDeclare("", exclusive:true);
        
        // Preparing the message to be send
        string json;
        if (message is Guid || (message is string strMessage && Guid.TryParse(strMessage, out _)))
        {
            json = message.ToString();
        }
        else
        {
            json = JsonConvert.SerializeObject(message, Formatting.Indented);
        }
        
        var requestBody = Encoding.UTF8.GetBytes(json);

        // Setting the properties required for the server to know how to reply to the request. 
        var properties = _channel.CreateBasicProperties();
        properties.ReplyTo = replyQueue.QueueName;
        properties.CorrelationId = Guid.NewGuid().ToString();
        
        Console.WriteLine($"Sending request: {properties.CorrelationId}");
        
        _channel.BasicPublish(
            exchange: _config.ExchangeName,
            routingKey: routingKey, 
            basicProperties: properties, 
            body: requestBody);
        
        // Log that we published a request here. 
        
        _channel.QueueDeclare(queueName, exclusive:false);
        
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