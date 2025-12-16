using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MsAuditoria.Application.DTOs;
using MsAuditoria.Application.Interfaces;
using System.Text.Json;

namespace MsAuditoria.Infra.Elasticsearch;

/// <summary>
/// Implementação do serviço de Elasticsearch
/// </summary>
public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(
        IConfiguration config,
        ILogger<ElasticsearchService> logger)
    {
        _logger = logger;

        var elasticUrl = config["Elasticsearch:Url"] ?? "http://localhost:9200";
        
        var settings = new ElasticsearchClientSettings(new Uri(elasticUrl))
            .DefaultIndex("audit-events")
            .DisableDirectStreaming()
            .RequestTimeout(TimeSpan.FromSeconds(30));

        _client = new ElasticsearchClient(settings);
    }

    /// <inheritdoc />
    public async Task IndexEventAsync(AuditEventDTO auditEvent)
    {
        // Determinar índice baseado no serviço de origem
        var indexName = $"audit-{auditEvent.SourceService.ToLowerInvariant().Replace("_", "-")}";

        var response = await _client.IndexAsync(auditEvent, idx => idx
            .Index(indexName)
            .Id(auditEvent.Id));

        if (!response.IsValidResponse)
        {
            _logger.LogError("Falha ao indexar evento: {Error}", response.DebugInformation);
            throw new Exception($"Falha ao indexar evento: {response.DebugInformation}");
        }

        _logger.LogDebug("Evento indexado: {EventId} no índice {Index}", auditEvent.Id, indexName);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditEventDTO>> SearchAsync(AuditQueryParams query)
    {
        var mustClauses = BuildMustClauses(query);
        
        SearchResponse<AuditEventDTO> searchResponse;
        
        if (mustClauses.Count > 0)
        {
            searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
                .Index("audit-*")
                .Query(q => q.Bool(b => b.Must(mustClauses.ToArray())))
                .Sort(sort => sort.Field("timestamp", new FieldSort { Order = SortOrder.Desc }))
                .Size(1000));
        }
        else
        {
            searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
                .Index("audit-*")
                .Query(q => q.MatchAll(new MatchAllQuery()))
                .Sort(sort => sort.Field("timestamp", new FieldSort { Order = SortOrder.Desc }))
                .Size(1000));
        }

        if (!searchResponse.IsValidResponse)
        {
            _logger.LogError("Falha na busca: {Error}", searchResponse.DebugInformation);
            return Enumerable.Empty<AuditEventDTO>();
        }

        return searchResponse.Documents;
    }

    /// <inheritdoc />
    public async Task<AuditEventDTO?> GetByIdAsync(string id)
    {
        var searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
            .Index("audit-*")
            .Query(q => q.Term(new TermQuery("id") { Value = id })));

        return searchResponse.Documents.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditEventDTO>> GetByEntityAsync(string entityName, string entityId)
    {
        var searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
            .Index("audit-*")
            .Query(q => q.Bool(b => b
                .Must(
                    m => m.Term(t => t.Field("entityName").Value(entityName)),
                    m => m.Term(t => t.Field("entityId").Value(entityId))
                )))
            .Sort(sort => sort.Field("timestamp", new FieldSort { Order = SortOrder.Desc })));

        return searchResponse.Documents;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditEventDTO>> GetByUserAsync(string userId)
    {
        var searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
            .Index("audit-*")
            .Query(q => q.Term(new TermQuery("userId") { Value = userId }))
            .Sort(sort => sort.Field("timestamp", new FieldSort { Order = SortOrder.Desc })));

        return searchResponse.Documents;
    }

    private List<Action<QueryDescriptor<AuditEventDTO>>> BuildMustClauses(AuditQueryParams query)
    {
        var mustClauses = new List<Action<QueryDescriptor<AuditEventDTO>>>();

        if (query.StartDate.HasValue || query.EndDate.HasValue)
        {
            mustClauses.Add(q => q.Range(r => r.DateRange(dr => dr
                .Field("timestamp")
                .Gte(query.StartDate)
                .Lte(query.EndDate))));
        }

        if (!string.IsNullOrEmpty(query.Operation))
        {
            mustClauses.Add(q => q.Term(t => t.Field("operation").Value(query.Operation)));
        }

        if (!string.IsNullOrEmpty(query.EntityName))
        {
            mustClauses.Add(q => q.Term(t => t.Field("entityName").Value(query.EntityName)));
        }

        if (!string.IsNullOrEmpty(query.UserId))
        {
            mustClauses.Add(q => q.Term(t => t.Field("userId").Value(query.UserId)));
        }

        if (!string.IsNullOrEmpty(query.SourceService))
        {
            mustClauses.Add(q => q.Term(t => t.Field("sourceService").Value(query.SourceService)));
        }

        if (!string.IsNullOrEmpty(query.CorrelationId))
        {
            mustClauses.Add(q => q.Term(t => t.Field("correlationId").Value(query.CorrelationId)));
        }

        return mustClauses;
    }
}
