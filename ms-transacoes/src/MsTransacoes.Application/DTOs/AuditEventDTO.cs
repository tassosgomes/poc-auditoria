namespace MsTransacoes.Application.DTOs;

public sealed class AuditEventDTO
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Operation { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, object?>? OldValues { get; set; }
    public Dictionary<string, object?>? NewValues { get; set; }
    public List<string>? ChangedFields { get; set; }
    public string SourceService { get; set; } = "ms-transacoes";
    public string? CorrelationId { get; set; }
}
