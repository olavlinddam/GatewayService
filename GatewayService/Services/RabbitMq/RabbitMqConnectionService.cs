using GatewayService.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace GatewayService.Services.RabbitMq;

public class RabbitMqConnectionService : IDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnectionService(IOptions<RabbitMqConfig> options)
    {
        var config = options.Value;

        var factory = new ConnectionFactory
        {
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost,
            HostName = config.HostName,
            Port = int.Parse(config.Port),
            ClientProvidedName = config.ClientProvidedName
        };
        _connection = factory.CreateConnection();
    }

    public IModel CreateChannel()
    {
        return _connection.CreateModel();
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
