using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MsTransacoes.Application.DTOs;
using MsTransacoes.Infra.Audit;
using RabbitMQ.Client;

namespace MsTransacoes.Infra.Messaging;

public sealed class RabbitMQPublisher : IAuditEventPublisher, IDisposable
{
    private const string ExchangeName = "audit-events";
    private const string RoutingKey = "audit";
    private const string ErrorRoutingKey = "audit.error";

    private readonly ILogger<RabbitMQPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly object _sync = new();

    public RabbitMQPublisher(IConfiguration config, ILogger<RabbitMQPublisher> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:User"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ConfirmSelect();

        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);

        _channel.QueueDeclare(
            queue: "audit-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = ExchangeName,
                ["x-dead-letter-routing-key"] = ErrorRoutingKey
            });
        _channel.QueueBind("audit-queue", ExchangeName, RoutingKey);

        _channel.QueueDeclare(
            queue: "audit-error-queue",
            durable: true,
            exclusive: false,
            autoDelete: false);
        _channel.QueueBind("audit-error-queue", ExchangeName, ErrorRoutingKey);
    }

    public Task PublishAsync(AuditEventDTO auditEvent)
    {
        try
        {
            var message = JsonSerializer.SerializeToUtf8Bytes(auditEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            lock (_sync)
            {
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";

                _channel.BasicPublish(ExchangeName, RoutingKey, properties, message);
                _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            }

            _logger.LogInformation("Evento de auditoria publicado: {EventId}", auditEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar evento, enviando para fila de erro");
            SendToErrorQueue(auditEvent);
        }

        return Task.CompletedTask;
    }

    public async Task PublishBatchAsync(IEnumerable<AuditEventDTO> events)
    {
        foreach (var evt in events)
        {
            await PublishAsync(evt);
        }
    }

    private void SendToErrorQueue(AuditEventDTO auditEvent)
    {
        try
        {
            var message = JsonSerializer.SerializeToUtf8Bytes(auditEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            lock (_sync)
            {
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";

                _channel.BasicPublish(ExchangeName, ErrorRoutingKey, properties, message);
                _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Falha crítica ao enviar para fila de erro");
        }
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
