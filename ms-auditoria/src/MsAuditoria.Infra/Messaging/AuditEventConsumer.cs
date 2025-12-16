using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MsAuditoria.Application.DTOs;
using MsAuditoria.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MsAuditoria.Infra.Messaging;

/// <summary>
/// Consumer de eventos de auditoria via RabbitMQ
/// </summary>
public class AuditEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditEventConsumer> _logger;
    private readonly IConfiguration _config;

    private IConnection? _connection;
    private IModel? _channel;

    private const string QueueName = "audit-queue";
    private const string ExchangeName = "audit-events";
    private const string RoutingKey = "audit";
    private const string ErrorRoutingKey = "audit.error";

    public AuditEventConsumer(
        IConfiguration config,
        IServiceProvider serviceProvider,
        ILogger<AuditEventConsumer> logger)
    {
        _config = config;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando consumer de auditoria...");

        // Aguardar RabbitMQ estar disponível
        await WaitForRabbitMQAsync(stoppingToken);

        if (_channel == null)
        {
            _logger.LogError("Não foi possível conectar ao RabbitMQ");
            return;
        }

        try
        {
            // Aguardar fila ser criada pelos produtores
            await WaitForQueueAsync(stoppingToken);

            // Configurar prefetch para processar uma mensagem por vez
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao configurar fila RabbitMQ");
            return;
        }

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                var auditEvent = JsonSerializer.Deserialize<AuditEventDTO>(message, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (auditEvent != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var elasticService = scope.ServiceProvider
                        .GetRequiredService<IElasticsearchService>();

                    await elasticService.IndexEventAsync(auditEvent);

                    _logger.LogInformation(
                        "Evento de auditoria processado: {EventId} | {Operation} em {EntityName}",
                        auditEvent.Id,
                        auditEvent.Operation,
                        auditEvent.EntityName);
                }

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento de auditoria: {Message}", message);
                // Rejeitar e não reprocessar (enviaria para DLQ se configurada)
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Consumer de auditoria iniciado e aguardando mensagens...");

        // Manter o serviço rodando
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer de auditoria finalizado");
        }
    }

    private async Task WaitForRabbitMQAsync(CancellationToken cancellationToken)
    {
        var maxRetries = 30;
        var retryCount = 0;

        while (retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _config["RabbitMQ:Host"] ?? "localhost",
                    UserName = _config["RabbitMQ:User"] ?? "guest",
                    Password = _config["RabbitMQ:Password"] ?? "guest",
                    Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672")
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _logger.LogInformation("Conectado ao RabbitMQ com sucesso");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ ainda não está disponível: {Message}", ex.Message);
                retryCount++;
                await Task.Delay(2000, cancellationToken);
            }
        }

        _logger.LogError("RabbitMQ não ficou disponível após {MaxRetries} tentativas", maxRetries);
    }

    private async Task WaitForQueueAsync(CancellationToken cancellationToken)
    {
        var maxRetries = 60;
        var retryCount = 0;

        while (retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Passive declare apenas verifica se a fila existe
                _channel!.QueueDeclarePassive(QueueName);
                _logger.LogInformation("Fila {QueueName} encontrada", QueueName);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Fila {QueueName} ainda não existe: {Message}", QueueName, ex.Message);
                
                // Reconectar pois o canal é fechado após erro
                try
                {
                    _channel?.Dispose();
                    _channel = _connection!.CreateModel();
                }
                catch { }

                retryCount++;
                await Task.Delay(2000, cancellationToken);
            }
        }

        throw new Exception($"Fila {QueueName} não foi criada após {maxRetries} tentativas");
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
