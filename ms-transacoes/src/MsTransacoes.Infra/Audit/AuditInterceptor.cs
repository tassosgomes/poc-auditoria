using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MsTransacoes.Application.DTOs;
using MsTransacoes.Application.Interfaces;
using MsTransacoes.Domain.Entities;
using MsTransacoes.Infra.Persistence;

namespace MsTransacoes.Infra.Audit;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserContextAccessor _userContext;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly ILogger<AuditInterceptor> _logger;

    private readonly ConcurrentDictionary<Guid, List<AuditLog>> _pendingAuditByContextId = new();

    public AuditInterceptor(
        IServiceProvider serviceProvider,
        IUserContextAccessor userContext,
        ICorrelationIdAccessor correlationId,
        ILogger<AuditInterceptor> logger)
    {
        _serviceProvider = serviceProvider;
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

        var auditLogs = new List<AuditLog>();

        // Capturar as entries antes de iterar para evitar "Collection was modified"
        var entriesToAudit = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is not AuditLog)
            .ToList();

        foreach (var entry in entriesToAudit)
        {
            var auditLog = CreateAuditLog(entry);
            auditLogs.Add(auditLog);
            
            // Adicionar ao contexto para salvar na MESMA transação
            context.Set<AuditLog>().Add(auditLog);
        }

        if (auditLogs.Count > 0)
        {
            _pendingAuditByContextId[context.ContextId.InstanceId] = auditLogs;
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        // Após o commit, publicar no RabbitMQ
        if (_pendingAuditByContextId.TryRemove(context.ContextId.InstanceId, out var auditLogs)
            && auditLogs.Count > 0)
        {
            _ = Task.Run(async () => await PublishToRabbitMQAsync(auditLogs), CancellationToken.None);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
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

    private AuditLog CreateAuditLog(EntityEntry entry)
    {
        var operation = entry.State switch
        {
            EntityState.Added => "INSERT",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => "UNKNOWN"
        };

        var entityId = GetPrimaryKeyValue(entry);
        
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entry.Entity.GetType().Name,
            EntityId = Guid.TryParse(entityId, out var guid) ? guid : Guid.Empty,
            Operation = operation,
            OldValues = GetOldValues(entry),
            NewValues = GetNewValues(entry),
            UserId = _userContext.GetCurrentUserId() ?? "system",
            CorrelationId = ParseCorrelationId(_correlationId.GetCorrelationId()),
            SourceService = "ms-transacoes",
            PublishedToQueue = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task PublishToRabbitMQAsync(List<AuditLog> auditLogs)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IAuditEventPublisher>();
            var dbContext = scope.ServiceProvider.GetRequiredService<TransacoesDbContext>();

            foreach (var auditLog in auditLogs)
            {
                try
                {
                    var auditEvent = new AuditEventDTO
                    {
                        Id = auditLog.Id.ToString(),
                        Timestamp = auditLog.CreatedAt.UtcDateTime,
                        Operation = auditLog.Operation,
                        EntityName = auditLog.EntityName,
                        EntityId = auditLog.EntityId.ToString(),
                        UserId = auditLog.UserId,
                        OldValues = DeserializeJson(auditLog.OldValues),
                        NewValues = DeserializeJson(auditLog.NewValues),
                        ChangedFields = ComputeChangedFields(auditLog.OldValues, auditLog.NewValues),
                        SourceService = auditLog.SourceService,
                        CorrelationId = auditLog.CorrelationId?.ToString()
                    };

                    await publisher.PublishAsync(auditEvent);
                    
                    // Marcar como publicado
                    var logEntry = await dbContext.AuditLogs.FindAsync(auditLog.Id);
                    if (logEntry != null)
                    {
                        logEntry.PublishedToQueue = true;
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log do erro - a auditoria local já está salva
                    _logger.LogError(ex, "Falha ao publicar no RabbitMQ para auditLog {AuditLogId}", auditLog.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao acessar RabbitMQ");
        }
    }

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var key = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return key?.CurrentValue?.ToString() ?? string.Empty;
    }

    private static string? GetOldValues(EntityEntry entry)
    {
        if (entry.State == EntityState.Added) return null;

        var values = entry.Properties
            .Where(p => !p.Metadata.IsPrimaryKey())
            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);

        return JsonSerializer.Serialize(values);
    }

    private static string? GetNewValues(EntityEntry entry)
    {
        if (entry.State == EntityState.Deleted) return null;

        var values = entry.Properties
            .Where(p => !p.Metadata.IsPrimaryKey())
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

        return JsonSerializer.Serialize(values);
    }

    private static Dictionary<string, object?>? DeserializeJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static List<string> ComputeChangedFields(string? oldValuesJson, string? newValuesJson)
    {
        var oldValues = DeserializeJson(oldValuesJson);
        var newValues = DeserializeJson(newValuesJson);

        if (oldValues == null || newValues == null)
            return new List<string>();

        var changed = new List<string>();
        foreach (var key in newValues.Keys)
        {
            if (oldValues.TryGetValue(key, out var oldValue))
            {
                var newValue = newValues[key];
                if (!Equals(oldValue, newValue))
                {
                    changed.Add(key);
                }
            }
        }

        return changed;
    }

    private static Guid? ParseCorrelationId(string? correlationId)
    {
        if (string.IsNullOrEmpty(correlationId)) return null;
        return Guid.TryParse(correlationId, out var guid) ? guid : null;
    }
}
