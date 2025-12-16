using System.Text.Json.Serialization;

namespace MsAuditoria.Application.DTOs;

/// <summary>
/// DTO que representa um evento de auditoria
/// </summary>
public record AuditEventDTO
{
    /// <summary>
    /// Identificador único do evento
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Data e hora do evento
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Tipo de operação (INSERT, UPDATE, DELETE)
    /// </summary>
    [JsonPropertyName("operation")]
    public string Operation { get; init; } = string.Empty;

    /// <summary>
    /// Nome da entidade auditada
    /// </summary>
    [JsonPropertyName("entityName")]
    public string EntityName { get; init; } = string.Empty;

    /// <summary>
    /// ID da entidade auditada
    /// </summary>
    [JsonPropertyName("entityId")]
    public string EntityId { get; init; } = string.Empty;

    /// <summary>
    /// ID do usuário que realizou a operação
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Valores anteriores (para UPDATE e DELETE)
    /// </summary>
    [JsonPropertyName("oldValues")]
    public Dictionary<string, object?>? OldValues { get; init; }

    /// <summary>
    /// Novos valores (para INSERT e UPDATE)
    /// </summary>
    [JsonPropertyName("newValues")]
    public Dictionary<string, object?>? NewValues { get; init; }

    /// <summary>
    /// Lista de campos alterados
    /// </summary>
    [JsonPropertyName("changedFields")]
    public List<string>? ChangedFields { get; init; }

    /// <summary>
    /// Nome do serviço de origem do evento
    /// </summary>
    [JsonPropertyName("sourceService")]
    public string SourceService { get; init; } = string.Empty;

    /// <summary>
    /// ID de correlação para rastreamento
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;
}
