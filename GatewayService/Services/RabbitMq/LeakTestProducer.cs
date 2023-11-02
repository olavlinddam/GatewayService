using System.Text;
using GatewayService.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GatewayService.Services.RabbitMq;


public class LeakTestProducer : IProducer
    {
        private readonly LeakTestServiceConfig _config;
        private readonly RabbitMqConnectionService _connectionService;
        private readonly string _exchangeName = "leaktest-exchange";

        public LeakTestProducer(IOptions<LeakTestServiceConfig> configOptions, RabbitMqConnectionService connectionService)
        {
            _config = configOptions.Value;
            _connectionService = connectionService;
        }

    public async Task<string> SendMessage<T>(T message, string queueName, string routingKey)
    {
        try
        {
            // Creating the channel to be used for this specific exchange of data. 
            using var channel = _connectionService.CreateChannel();

            // Declare the queue and set up the binding
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueBind(queue: queueName, exchange: _config.ExchangeName, routingKey: routingKey);


            var tcs = new TaskCompletionSource<string>();
            
            var replyQueue = channel.QueueDeclare("", exclusive:true);
            
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
            var properties = channel.CreateBasicProperties();
            properties.ReplyTo = replyQueue.QueueName;
            properties.CorrelationId = Guid.NewGuid().ToString();
            
            Console.WriteLine($"Sending request: {properties.CorrelationId}");
            
            channel.BasicPublish(
                exchange: _config.ExchangeName,
                routingKey: routingKey, 
                basicProperties: properties, 
                body: requestBody);
            
            // Log that we published a request here. 
            
            //_channel.QueueDeclare(queueName, exclusive:false);
            
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == properties.CorrelationId)
                {
                    var body = ea.Body.ToArray();
                    var response = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Reply Received: {response}");

                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    tcs.TrySetResult(response);
                }
                else
                {
                    Console.WriteLine($"Received unmatched reply. Expected Correlation ID: {properties.CorrelationId}, Received: {ea.BasicProperties.CorrelationId}");
                    // Log the event here
                }
            };
            
            channel.BasicConsume(queue: replyQueue.QueueName, autoAck: false, consumer: consumer);
            
            var taskToWait = tcs.Task;
            if (await Task.WhenAny(taskToWait, Task.Delay(TimeSpan.FromSeconds(2))) == taskToWait)
            {
                // Task completed within the timeout
                return await taskToWait;
            }
            
            // Timeout logic
            throw new TimeoutException("The service is currently unavailable, please try again later.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
}