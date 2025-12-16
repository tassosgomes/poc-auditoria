using MsAuditoria.Application.DTOs;
using MsAuditoria.Application.Interfaces;

namespace MsAuditoria.Application.Services;

/// <summary>
/// Implementação do serviço de auditoria
/// </summary>
public class AuditService : IAuditService
{
    private readonly IElasticsearchService _elasticsearchService;

    public AuditService(IElasticsearchService elasticsearchService)
    {
        _elasticsearchService = elasticsearchService;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditEventDTO>> SearchAsync(AuditQueryParams query)
    {
        return await _elasticsearchService.SearchAsync(query);
    }

    /// <inheritdoc />
    public async Task<AuditEventDTO?> GetByIdAsync(string id)
    {
        return await _elasticsearchService.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditEventDTO>> GetByEntityAsync(string entityName, string entityId)
    {
        return await _elasticsearchService.GetByEntityAsync(entityName, entityId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditEventDTO>> GetByUserAsync(string userId)
    {
        return await _elasticsearchService.GetByUserAsync(userId);
    }
}
