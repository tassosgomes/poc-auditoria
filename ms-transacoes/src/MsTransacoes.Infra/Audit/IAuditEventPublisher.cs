using MsTransacoes.Application.DTOs;

namespace MsTransacoes.Infra.Audit;

public interface IAuditEventPublisher
{
    Task PublishAsync(AuditEventDTO auditEvent);
    Task PublishBatchAsync(IEnumerable<AuditEventDTO> events);
}
