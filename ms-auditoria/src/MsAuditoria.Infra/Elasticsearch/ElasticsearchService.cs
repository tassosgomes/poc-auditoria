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
    private readonly int _maxResults;

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

        _maxResults = int.TryParse(config["Elasticsearch:MaxResults"], out var maxResults)
            ? maxResults
            : 1000;
    }

    /// <inheritdoc />
    public async Task IndexEventAsync(AuditEventDTO auditEvent)
    {
        // Determinar índice baseado no serviço de origem
        var sourceService = string.IsNullOrWhiteSpace(auditEvent.SourceService)
            ? "unknown"
            : auditEvent.SourceService;

        var indexName = $"audit-{sourceService.ToLowerInvariant().Replace("_", "-")}";

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
                .Sort(sort => sort.Field(f => f.Timestamp, new FieldSort { Order = SortOrder.Desc }))
                .Size(_maxResults));
        }
        else
        {
            searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
                .Index("audit-*")
                .Query(q => q.MatchAll(new MatchAllQuery()))
                .Sort(sort => sort.Field(f => f.Timestamp, new FieldSort { Order = SortOrder.Desc }))
                .Size(_maxResults));
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
            .Query(q => q.Term(t => t.Field(f => f.Id).Value(id))));

        return searchResponse.Documents.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditEventDTO>> GetByEntityAsync(string entityName, string entityId)
    {
        var searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
            .Index("audit-*")
            .Query(q => q.Bool(b => b
                .Must(
                    m => m.Term(t => t.Field(f => f.EntityName).Value(entityName)),
                    m => m.Term(t => t.Field(f => f.EntityId).Value(entityId))
                )))
            .Sort(sort => sort.Field(f => f.Timestamp, new FieldSort { Order = SortOrder.Desc })));

        return searchResponse.Documents;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditEventDTO>> GetByUserAsync(string userId)
    {
        var searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
            .Index("audit-*")
            .Query(q => q.Term(t => t.Field(f => f.UserId).Value(userId)))
            .Sort(sort => sort.Field(f => f.Timestamp, new FieldSort { Order = SortOrder.Desc })));

        return searchResponse.Documents;
    }

    private List<Action<QueryDescriptor<AuditEventDTO>>> BuildMustClauses(AuditQueryParams query)
    {
        var mustClauses = new List<Action<QueryDescriptor<AuditEventDTO>>>();

        if (query.StartDate.HasValue || query.EndDate.HasValue)
        {
            mustClauses.Add(q => q.Range(r => r.DateRange(dr =>
            {
                dr.Field(f => f.Timestamp);

                if (query.StartDate.HasValue)
                {
                    dr.Gte(query.StartDate.Value);
                }

                if (query.EndDate.HasValue)
                {
                    dr.Lte(query.EndDate.Value);
                }
            })));
        }

        if (!string.IsNullOrEmpty(query.Operation))
        {
            mustClauses.Add(q => q.Term(t => t.Field(f => f.Operation).Value(query.Operation)));
        }

        if (!string.IsNullOrEmpty(query.EntityName))
        {
            mustClauses.Add(q => q.Term(t => t.Field(f => f.EntityName).Value(query.EntityName)));
        }

        if (!string.IsNullOrEmpty(query.UserId))
        {
            mustClauses.Add(q => q.Term(t => t.Field(f => f.UserId).Value(query.UserId)));
        }

        if (!string.IsNullOrEmpty(query.SourceService))
        {
            mustClauses.Add(q => q.Term(t => t.Field(f => f.SourceService).Value(query.SourceService)));
        }

        if (!string.IsNullOrEmpty(query.CorrelationId))
        {
            mustClauses.Add(q => q.Term(t => t.Field(f => f.CorrelationId).Value(query.CorrelationId)));
        }

        return mustClauses;
    }
}
