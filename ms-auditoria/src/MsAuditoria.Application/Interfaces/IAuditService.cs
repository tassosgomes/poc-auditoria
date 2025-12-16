using MsAuditoria.Application.DTOs;

namespace MsAuditoria.Application.Interfaces;

/// <summary>
/// Interface para o serviço de auditoria
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Busca eventos de auditoria com filtros
    /// </summary>
    Task<IEnumerable<AuditEventDTO>> SearchAsync(AuditQueryParams query);

    /// <summary>
    /// Busca evento por ID
    /// </summary>
    Task<AuditEventDTO?> GetByIdAsync(string id);

    /// <summary>
    /// Busca histórico de alterações de uma entidade
    /// </summary>
    Task<IEnumerable<AuditEventDTO>> GetByEntityAsync(string entityName, string entityId);

    /// <summary>
    /// Busca eventos por usuário
    /// </summary>
    Task<IEnumerable<AuditEventDTO>> GetByUserAsync(string userId);
}
