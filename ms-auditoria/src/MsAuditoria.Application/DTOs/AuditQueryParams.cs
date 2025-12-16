namespace MsAuditoria.Application.DTOs;

/// <summary>
/// Parâmetros de consulta para eventos de auditoria
/// </summary>
public record AuditQueryParams
{
    /// <summary>
    /// Data inicial do período de busca
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Data final do período de busca
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Filtro por tipo de operação (INSERT, UPDATE, DELETE)
    /// </summary>
    public string? Operation { get; init; }

    /// <summary>
    /// Filtro por nome da entidade
    /// </summary>
    public string? EntityName { get; init; }

    /// <summary>
    /// Filtro por ID do usuário
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Filtro por serviço de origem
    /// </summary>
    public string? SourceService { get; init; }

    /// <summary>
    /// Filtro por ID de correlação
    /// </summary>
    public string? CorrelationId { get; init; }
}
