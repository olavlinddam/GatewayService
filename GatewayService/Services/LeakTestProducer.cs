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
        _channel.ExchangeDeclare(_config.ExchangeName, "direct", durable: true, autoDelete: false, arguments: null);
    }

    public async Task<string> SendMessage<T>(T message, string queueName, string routingKey)
    {
        // Declare the queue and set up the binding
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: queueName, exchange: _config.ExchangeName, routingKey: routingKey);


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
        
        //_channel.QueueDeclare(queueName, exclusive:false);
        
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            if (ea.BasicProperties.CorrelationId == properties.CorrelationId)
            {
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Reply Received: {response}");

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                tcs.TrySetResult(response);
            }
            else
            {
                Console.WriteLine($"Received unmatched reply. Expected Correlation ID: {properties.CorrelationId}, Received: {ea.BasicProperties.CorrelationId}");
                // Log the event here
            }
        };

        
        _channel.BasicConsume(queue: replyQueue.QueueName, autoAck: false, consumer: consumer);
        
        var taskToWait = tcs.Task;
        if (await Task.WhenAny(taskToWait, Task.Delay(TimeSpan.FromSeconds(2))) == taskToWait)
        {
            // Task completed within the timeout
            return await taskToWait;
        }
        
        // Timeout logic
        throw new TimeoutException("The service is currently unavailable, please try again later.");
    }

    public void Dispose()
    {
        _channel.Close();
        _channel.Dispose();
    }
}