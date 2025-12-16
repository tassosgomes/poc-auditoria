using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MsTransacoes.Application.DTOs;
using MsTransacoes.Application.Interfaces;

namespace MsTransacoes.Infra.Audit;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IAuditEventPublisher _publisher;
    private readonly IUserContextAccessor _userContext;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly ILogger<AuditInterceptor> _logger;

    private readonly ConcurrentDictionary<Guid, List<AuditEventDTO>> _pendingAuditByContextId = new();

    public AuditInterceptor(
        IAuditEventPublisher publisher,
        IUserContextAccessor userContext,
        ICorrelationIdAccessor correlationId,
        ILogger<AuditInterceptor> logger)
    {
        _publisher = publisher;
        _userContext = userContext;
        _correlationId = correlationId;
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var auditEntries = new List<AuditEventDTO>();

        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var auditEvent = new AuditEventDTO
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Operation = GetOperationName(entry.State),
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetPrimaryKeyValue(entry),
                UserId = _userContext.GetCurrentUserId() ?? "system",
                OldValues = entry.State != EntityState.Added ? GetValues(entry.OriginalValues) : null,
                NewValues = entry.State != EntityState.Deleted ? GetValues(entry.CurrentValues) : null,
                ChangedFields = GetChangedFields(entry),
                SourceService = "ms-transacoes",
                CorrelationId = _correlationId.GetCorrelationId()
            };

            auditEntries.Add(auditEvent);
        }

        if (auditEntries.Count > 0)
        {
            _pendingAuditByContextId[context.ContextId.InstanceId] = auditEntries;
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        if (_pendingAuditByContextId.TryRemove(context.ContextId.InstanceId, out var auditEntries)
            && auditEntries.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _publisher.PublishBatchAsync(auditEntries);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao publicar eventos de auditoria");
                }
            }, CancellationToken.None);
        }

        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is not null)
        {
            _pendingAuditByContextId.TryRemove(context.ContextId.InstanceId, out _);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private static string GetOperationName(EntityState state)
    {
        return state switch
        {
            EntityState.Added => "INSERT",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => "UNKNOWN"
        };
    }

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var key = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return key?.CurrentValue?.ToString() ?? string.Empty;
    }

    private static Dictionary<string, object?> GetValues(PropertyValues values)
    {
        return values.Properties.ToDictionary(p => p.Name, p => values[p]);
    }

    private static List<string> GetChangedFields(EntityEntry entry)
    {
        if (entry.State != EntityState.Modified)
        {
            return [];
        }

        return entry.Properties
            .Where(p => p.IsModified)
            .Select(p => p.Metadata.Name)
            .ToList();
    }
}
