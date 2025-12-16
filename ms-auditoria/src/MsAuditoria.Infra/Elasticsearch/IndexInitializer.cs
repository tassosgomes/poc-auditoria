using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MsAuditoria.Application.DTOs;

namespace MsAuditoria.Infra.Elasticsearch;

/// <summary>
/// Serviço para inicialização dos índices do Elasticsearch
/// </summary>
public class IndexInitializer : IHostedService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<IndexInitializer> _logger;

    private readonly string[] _indices = { "audit-ms-contas", "audit-ms-transacoes" };

    public IndexInitializer(
        IConfiguration config,
        ILogger<IndexInitializer> logger)
    {
        _logger = logger;

        var elasticUrl = config["Elasticsearch:Url"] ?? "http://localhost:9200";
        
        var settings = new ElasticsearchClientSettings(new Uri(elasticUrl))
            .RequestTimeout(TimeSpan.FromSeconds(60));

        _client = new ElasticsearchClient(settings);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando criação dos índices do Elasticsearch...");

        // Aguardar Elasticsearch estar disponível
        await WaitForElasticsearchAsync(cancellationToken);

        foreach (var indexName in _indices)
        {
            try
            {
                var exists = await _client.Indices.ExistsAsync(indexName, cancellationToken);

                if (!exists.Exists)
                {
                    var createResponse = await _client.Indices.CreateAsync(indexName, c => c
                        .Mappings(m => m
                            .Properties<AuditEventDTO>(p => p
                                .Keyword(k => k.Id)
                                .Date(d => d.Timestamp)
                                .Keyword(k => k.Operation)
                                .Keyword(k => k.EntityName)
                                .Keyword(k => k.EntityId)
                                .Keyword(k => k.UserId)
                                .Object(o => o.OldValues!)
                                .Object(o => o.NewValues!)
                                .Keyword(k => k.ChangedFields!)
                                .Keyword(k => k.SourceService)
                                .Keyword(k => k.CorrelationId)
                            ))
                        .Settings(s => s
                            .NumberOfShards(1)
                            .NumberOfReplicas(0)),
                        cancellationToken);

                    if (createResponse.IsValidResponse)
                    {
                        _logger.LogInformation("Índice {Index} criado com sucesso", indexName);
                    }
                    else
                    {
                        _logger.LogError("Falha ao criar índice {Index}: {Error}",
                            indexName, createResponse.DebugInformation);
                    }
                }
                else
                {
                    _logger.LogInformation("Índice {Index} já existe", indexName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar índice {Index}", indexName);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task WaitForElasticsearchAsync(CancellationToken cancellationToken)
    {
        var maxRetries = 30;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var ping = await _client.PingAsync(cancellationToken);
                if (ping.IsValidResponse)
                {
                    _logger.LogInformation("Elasticsearch está disponível");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Elasticsearch ainda não está disponível: {Message}", ex.Message);
            }

            retryCount++;
            await Task.Delay(2000, cancellationToken);
        }

        _logger.LogError("Elasticsearch não ficou disponível após {MaxRetries} tentativas", maxRetries);
    }
}
