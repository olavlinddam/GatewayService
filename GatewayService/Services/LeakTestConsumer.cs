using System.Text;
using GatewayService.Configuration;
using GatewayService.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GatewayService.Services;

public class LeakTestConsumer : IConsumer, IDisposable
{
    private readonly LeakTestServiceConfig _config;
    private readonly IModel _channel;
    private readonly PendingRequestManager _pendingRequestManager;

    public LeakTestConsumer(IOptions<LeakTestServiceConfig> config, PendingRequestManager pendingRequestManager)
    {
        _config = config.Value;
        _pendingRequestManager = pendingRequestManager;
        
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
        

        //_channel.QueueDeclare("leaktest-response-queue", exclusive: false); // potentielt problem her?
    }
    
    public void StartListening()
    {
        var consumer = new EventingBasicConsumer(_channel);
    
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var response = Encoding.UTF8.GetString(body);
                var correlationId = ea.BasicProperties.CorrelationId;

                _pendingRequestManager.TryCompleteRequest(correlationId, response);

                if (!ea.Redelivered)
                {
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);  // Manuelt acknowledgment
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);  // Nack beskeden, så den kan blive behandlet igen
            }
        };
    }

    
    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
    }
}