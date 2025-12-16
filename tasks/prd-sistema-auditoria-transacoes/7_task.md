---
status: pending
parallelizable: false
blocked_by: ["2.0", "3.0"]
---

<task_context>
<domain>backend/database</domain>
<type>enhancement</type>
<scope>audit_persistence</scope>
<complexity>medium</complexity>
<dependencies>ms-contas, ms-transacoes, postgresql</dependencies>
<unblocks>none</unblocks>
</task_context>

# Tarefa 7.0: Persistência Local de Auditoria (Mesma Transação)

## Visão Geral

Adicionar persistência local dos eventos de auditoria em tabelas `audit_log` dentro de cada schema do PostgreSQL. A gravação deve ocorrer **na mesma transação** da operação principal, garantindo atomicidade. A publicação no RabbitMQ continua assíncrona (após commit).

<requirements>
- MS-Contas e MS-Transações já implementados (Tarefas 2.0 e 3.0)
- PostgreSQL com schemas `contas` e `transacoes` criados
- Interceptores de auditoria já funcionando
</requirements>

## Motivação

- **Garantia de persistência:** Auditoria não se perde se RabbitMQ estiver indisponível
- **Atomicidade:** Se a transação falhar, a auditoria também é revertida (rollback)
- **Isolamento:** Cada schema mantém seus próprios logs de auditoria
- **Compliance:** Requisito comum em sistemas financeiros

## Subtarefas

- [ ] 7.1 Criar tabela `audit_log` no schema `contas`
- [ ] 7.2 Criar tabela `audit_log` no schema `transacoes`
- [ ] 7.3 Atualizar init.sql com as novas tabelas
- [ ] 7.4 Criar entidade AuditLog no MS-Contas (Java)
- [ ] 7.5 Criar repositório AuditLogRepository no MS-Contas
- [ ] 7.6 Modificar AuditEventListener para salvar no banco (mesma transação)
- [ ] 7.7 Implementar publicação RabbitMQ após commit (TransactionSynchronization)
- [ ] 7.8 Criar entidade AuditLog no MS-Transações (.NET)
- [ ] 7.9 Criar AuditLogRepository no MS-Transações
- [ ] 7.10 Modificar AuditInterceptor para salvar no banco (mesma transação)
- [ ] 7.11 Implementar publicação RabbitMQ após commit (TransactionScope)
- [ ] 7.12 Testar rollback (auditoria não deve persistir se transação falhar)
- [ ] 7.13 Testar cenário RabbitMQ offline (auditoria local deve persistir)

## Sequenciamento

- **Bloqueado por:** 2.0, 3.0 (precisa dos interceptores implementados)
- **Desbloqueia:** Nenhuma
- **Paralelizável:** Parcialmente (Java e .NET podem ser feitos em paralelo)

## Detalhes de Implementação

### 7.1-7.3 Script SQL (init.sql)

Adicionar ao `infra/init.sql`:

```sql
-- =====================
-- TABELAS DE AUDITORIA
-- =====================

-- Auditoria no schema contas
CREATE TABLE contas.audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_name VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    operation VARCHAR(10) NOT NULL CHECK (operation IN ('INSERT', 'UPDATE', 'DELETE')),
    old_values JSONB,
    new_values JSONB,
    user_id VARCHAR(100),
    correlation_id UUID,
    source_service VARCHAR(50) DEFAULT 'ms-contas',
    published_to_queue BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_audit_contas_entity ON contas.audit_log(entity_name, entity_id);
CREATE INDEX idx_audit_contas_user ON contas.audit_log(user_id);
CREATE INDEX idx_audit_contas_created ON contas.audit_log(created_at);
CREATE INDEX idx_audit_contas_unpublished ON contas.audit_log(published_to_queue) WHERE published_to_queue = FALSE;

-- Auditoria no schema transacoes
CREATE TABLE transacoes.audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_name VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    operation VARCHAR(10) NOT NULL CHECK (operation IN ('INSERT', 'UPDATE', 'DELETE')),
    old_values JSONB,
    new_values JSONB,
    user_id VARCHAR(100),
    correlation_id UUID,
    source_service VARCHAR(50) DEFAULT 'ms-transacoes',
    published_to_queue BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_audit_transacoes_entity ON transacoes.audit_log(entity_name, entity_id);
CREATE INDEX idx_audit_transacoes_user ON transacoes.audit_log(user_id);
CREATE INDEX idx_audit_transacoes_created ON transacoes.audit_log(created_at);
CREATE INDEX idx_audit_transacoes_unpublished ON transacoes.audit_log(published_to_queue) WHERE published_to_queue = FALSE;
```

### 7.4-7.5 Entidade e Repositório (Java - MS-Contas)

```java
// src/main/java/com/poc/mscontas/domain/entity/AuditLog.java
package com.poc.mscontas.domain.entity;

import jakarta.persistence.*;
import java.time.OffsetDateTime;
import java.util.UUID;

@Entity
@Table(name = "audit_log", schema = "contas")
public class AuditLog {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;

    @Column(name = "entity_name", nullable = false, length = 100)
    private String entityName;

    @Column(name = "entity_id", nullable = false)
    private UUID entityId;

    @Column(nullable = false, length = 10)
    private String operation;

    @Column(name = "old_values", columnDefinition = "jsonb")
    private String oldValues;

    @Column(name = "new_values", columnDefinition = "jsonb")
    private String newValues;

    @Column(name = "user_id", length = 100)
    private String userId;

    @Column(name = "correlation_id")
    private UUID correlationId;

    @Column(name = "source_service", length = 50)
    private String sourceService = "ms-contas";

    @Column(name = "published_to_queue")
    private Boolean publishedToQueue = false;

    @Column(name = "created_at")
    private OffsetDateTime createdAt = OffsetDateTime.now();

    // Getters e Setters
    public UUID getId() { return id; }
    public void setId(UUID id) { this.id = id; }
    
    public String getEntityName() { return entityName; }
    public void setEntityName(String entityName) { this.entityName = entityName; }
    
    public UUID getEntityId() { return entityId; }
    public void setEntityId(UUID entityId) { this.entityId = entityId; }
    
    public String getOperation() { return operation; }
    public void setOperation(String operation) { this.operation = operation; }
    
    public String getOldValues() { return oldValues; }
    public void setOldValues(String oldValues) { this.oldValues = oldValues; }
    
    public String getNewValues() { return newValues; }
    public void setNewValues(String newValues) { this.newValues = newValues; }
    
    public String getUserId() { return userId; }
    public void setUserId(String userId) { this.userId = userId; }
    
    public UUID getCorrelationId() { return correlationId; }
    public void setCorrelationId(UUID correlationId) { this.correlationId = correlationId; }
    
    public String getSourceService() { return sourceService; }
    public void setSourceService(String sourceService) { this.sourceService = sourceService; }
    
    public Boolean getPublishedToQueue() { return publishedToQueue; }
    public void setPublishedToQueue(Boolean publishedToQueue) { this.publishedToQueue = publishedToQueue; }
    
    public OffsetDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(OffsetDateTime createdAt) { this.createdAt = createdAt; }
}
```

```java
// src/main/java/com/poc/mscontas/infrastructure/repository/AuditLogRepository.java
package com.poc.mscontas.infrastructure.repository;

import com.poc.mscontas.domain.entity.AuditLog;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.UUID;

@Repository
public interface AuditLogRepository extends JpaRepository<AuditLog, UUID> {
    
    List<AuditLog> findByPublishedToQueueFalse();
    
    @Modifying
    @Query("UPDATE AuditLog a SET a.publishedToQueue = true WHERE a.id = :id")
    void markAsPublished(UUID id);
}
```

### 7.6-7.7 AuditEventListener Modificado (Java)

```java
// src/main/java/com/poc/mscontas/infrastructure/audit/AuditEventListener.java
package com.poc.mscontas.infrastructure.audit;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.poc.mscontas.domain.entity.AuditLog;
import com.poc.mscontas.infrastructure.repository.AuditLogRepository;
import com.poc.mscontas.infrastructure.messaging.AuditEventPublisher;
import com.poc.mscontas.infrastructure.context.RequestContextHolder;
import org.hibernate.event.spi.*;
import org.hibernate.persister.entity.EntityPersister;
import org.springframework.stereotype.Component;
import org.springframework.transaction.support.TransactionSynchronization;
import org.springframework.transaction.support.TransactionSynchronizationManager;

import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

@Component
public class AuditEventListener implements 
        PostInsertEventListener, 
        PostUpdateEventListener, 
        PostDeleteEventListener {

    private final AuditLogRepository auditLogRepository;
    private final AuditEventPublisher eventPublisher;
    private final ObjectMapper objectMapper;

    public AuditEventListener(
            AuditLogRepository auditLogRepository,
            AuditEventPublisher eventPublisher,
            ObjectMapper objectMapper) {
        this.auditLogRepository = auditLogRepository;
        this.eventPublisher = eventPublisher;
        this.objectMapper = objectMapper;
    }

    @Override
    public void onPostInsert(PostInsertEvent event) {
        if (isAuditableEntity(event.getEntity())) {
            AuditLog auditLog = createAuditLog(event.getEntity(), "INSERT", null, event.getState(), event.getPersister());
            saveAndPublishAfterCommit(auditLog);
        }
    }

    @Override
    public void onPostUpdate(PostUpdateEvent event) {
        if (isAuditableEntity(event.getEntity())) {
            AuditLog auditLog = createAuditLog(event.getEntity(), "UPDATE", event.getOldState(), event.getState(), event.getPersister());
            saveAndPublishAfterCommit(auditLog);
        }
    }

    @Override
    public void onPostDelete(PostDeleteEvent event) {
        if (isAuditableEntity(event.getEntity())) {
            AuditLog auditLog = createAuditLog(event.getEntity(), "DELETE", event.getDeletedState(), null, event.getPersister());
            saveAndPublishAfterCommit(auditLog);
        }
    }

    private boolean isAuditableEntity(Object entity) {
        // Não auditar a própria tabela de auditoria
        return !(entity instanceof AuditLog);
    }

    private AuditLog createAuditLog(Object entity, String operation, Object[] oldState, Object[] newState, EntityPersister persister) {
        AuditLog auditLog = new AuditLog();
        auditLog.setEntityName(entity.getClass().getSimpleName());
        auditLog.setEntityId(getEntityId(entity));
        auditLog.setOperation(operation);
        auditLog.setOldValues(toJson(toMap(oldState, persister)));
        auditLog.setNewValues(toJson(toMap(newState, persister)));
        auditLog.setUserId(RequestContextHolder.getCurrentUser());
        auditLog.setCorrelationId(RequestContextHolder.getCorrelationId());
        auditLog.setSourceService("ms-contas");
        auditLog.setPublishedToQueue(false);
        return auditLog;
    }

    private void saveAndPublishAfterCommit(AuditLog auditLog) {
        // 1. Salvar na mesma transação (síncrono)
        AuditLog saved = auditLogRepository.save(auditLog);

        // 2. Publicar no RabbitMQ APÓS o commit (assíncrono)
        if (TransactionSynchronizationManager.isSynchronizationActive()) {
            TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
                @Override
                public void afterCommit() {
                    try {
                        eventPublisher.publish(toAuditEvent(saved));
                        auditLogRepository.markAsPublished(saved.getId());
                    } catch (Exception e) {
                        // Log do erro - a auditoria local já está salva
                        // Um job de retry pode republicar depois
                        System.err.println("Falha ao publicar no RabbitMQ: " + e.getMessage());
                    }
                }
            });
        }
    }

    private UUID getEntityId(Object entity) {
        try {
            var method = entity.getClass().getMethod("getId");
            return (UUID) method.invoke(entity);
        } catch (Exception e) {
            return null;
        }
    }

    private Map<String, Object> toMap(Object[] state, EntityPersister persister) {
        if (state == null) return null;
        Map<String, Object> map = new HashMap<>();
        String[] propertyNames = persister.getPropertyNames();
        for (int i = 0; i < propertyNames.length; i++) {
            map.put(propertyNames[i], state[i]);
        }
        return map;
    }

    private String toJson(Object obj) {
        try {
            return obj == null ? null : objectMapper.writeValueAsString(obj);
        } catch (Exception e) {
            return null;
        }
    }

    private AuditEvent toAuditEvent(AuditLog log) {
        return new AuditEvent(
            log.getId(),
            log.getEntityName(),
            log.getEntityId(),
            log.getOperation(),
            log.getOldValues(),
            log.getNewValues(),
            log.getUserId(),
            log.getCorrelationId(),
            log.getSourceService(),
            log.getCreatedAt()
        );
    }

    @Override
    public boolean requiresPostCommitHandling(EntityPersister persister) {
        return false; // Usamos TransactionSynchronization manualmente
    }
}
```

### 7.8-7.9 Entidade e Repositório (.NET - MS-Transações)

```csharp
// src/Domain/Entities/AuditLog.cs
namespace MsTransacoes.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string SourceService { get; set; } = "ms-transacoes";
    public bool PublishedToQueue { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

```csharp
// src/Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsTransacoes.Domain.Entities;

namespace MsTransacoes.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log", "transacoes");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Operation).HasMaxLength(10).IsRequired();
        builder.Property(x => x.OldValues).HasColumnType("jsonb");
        builder.Property(x => x.NewValues).HasColumnType("jsonb");
        builder.Property(x => x.UserId).HasMaxLength(100);
        builder.Property(x => x.SourceService).HasMaxLength(50);
        
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
```

### 7.10-7.11 AuditInterceptor Modificado (.NET)

```csharp
// src/Infrastructure/Audit/AuditInterceptor.cs
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MsTransacoes.Domain.Entities;
using MsTransacoes.Infrastructure.Messaging;
using MsTransacoes.Infrastructure.Context;

namespace MsTransacoes.Infrastructure.Audit;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<AuditLog> _pendingAuditLogs = new();

    public AuditInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return ValueTask.FromResult(result);
        
        var context = eventData.Context;
        _pendingAuditLogs.Clear();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Não auditar a própria tabela de auditoria
            if (entry.Entity is AuditLog) continue;
            
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var auditLog = CreateAuditLog(entry);
                _pendingAuditLogs.Add(auditLog);
                
                // Adicionar ao contexto para salvar na MESMA transação
                context.Set<AuditLog>().Add(auditLog);
            }
        }

        return ValueTask.FromResult(result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        // Após o commit, publicar no RabbitMQ
        if (_pendingAuditLogs.Any())
        {
            await PublishToRabbitMQAsync();
        }

        return result;
    }

    private AuditLog CreateAuditLog(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var operation = entry.State switch
        {
            EntityState.Added => "INSERT",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => "UNKNOWN"
        };

        var entityId = GetEntityId(entry);
        
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entry.Entity.GetType().Name,
            EntityId = entityId,
            Operation = operation,
            OldValues = GetOldValues(entry),
            NewValues = GetNewValues(entry),
            UserId = RequestContext.CurrentUser,
            CorrelationId = RequestContext.CorrelationId,
            SourceService = "ms-transacoes",
            PublishedToQueue = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task PublishToRabbitMQAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMQPublisher>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var auditLog in _pendingAuditLogs)
            {
                try
                {
                    var auditEvent = new AuditEvent
                    {
                        Id = auditLog.Id,
                        EntityName = auditLog.EntityName,
                        EntityId = auditLog.EntityId,
                        Operation = auditLog.Operation,
                        OldValues = auditLog.OldValues,
                        NewValues = auditLog.NewValues,
                        UserId = auditLog.UserId,
                        CorrelationId = auditLog.CorrelationId,
                        SourceService = auditLog.SourceService,
                        Timestamp = auditLog.CreatedAt
                    };

                    await publisher.PublishAsync(auditEvent);
                    
                    // Marcar como publicado
                    var logEntry = await dbContext.Set<AuditLog>().FindAsync(auditLog.Id);
                    if (logEntry != null)
                    {
                        logEntry.PublishedToQueue = true;
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log do erro - a auditoria local já está salva
                    Console.WriteLine($"Falha ao publicar no RabbitMQ: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao acessar RabbitMQ: {ex.Message}");
        }
    }

    private Guid GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        return idProperty?.CurrentValue is Guid guid ? guid : Guid.Empty;
    }

    private string? GetOldValues(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.State == EntityState.Added) return null;

        var values = entry.Properties
            .Where(p => !p.Metadata.IsPrimaryKey())
            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);

        return JsonSerializer.Serialize(values);
    }

    private string? GetNewValues(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.State == EntityState.Deleted) return null;

        var values = entry.Properties
            .Where(p => !p.Metadata.IsPrimaryKey())
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

        return JsonSerializer.Serialize(values);
    }
}
```

### Job de Retry (Opcional)

Para reprocessar eventos não publicados:

```java
// Java - MS-Contas
@Scheduled(fixedDelay = 60000) // A cada 1 minuto
public void retryUnpublishedAuditLogs() {
    List<AuditLog> unpublished = auditLogRepository.findByPublishedToQueueFalse();
    for (AuditLog log : unpublished) {
        try {
            eventPublisher.publish(toAuditEvent(log));
            auditLogRepository.markAsPublished(log.getId());
        } catch (Exception e) {
            // Log e continua
        }
    }
}
```

```csharp
// .NET - MS-Transações (BackgroundService)
public class AuditRetryService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RetryUnpublishedAsync();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

## Fluxo de Dados Atualizado

```
┌─────────────────────────────────────────────────────────────────┐
│                    MESMA TRANSAÇÃO                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────────┐  │
│  │   Operação   │───▶│ Interceptor  │───▶│ audit_log table  │  │
│  │   (CRUD)     │    │              │    │ (mesmo schema)   │  │
│  └──────────────┘    └──────────────┘    └──────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ APÓS COMMIT
                                ▼
                    ┌──────────────────────┐
                    │     RabbitMQ         │
                    │   (audit-events)     │
                    └──────────┬───────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │   MS-Auditoria       │
                    │   (Elasticsearch)    │
                    └──────────────────────┘
```

## Critérios de Sucesso

- [ ] Tabelas `audit_log` criadas em ambos os schemas
- [ ] Auditoria salva na mesma transação da operação
- [ ] Rollback reverte tanto a operação quanto a auditoria
- [ ] Publicação no RabbitMQ ocorre após commit
- [ ] Se RabbitMQ estiver offline, auditoria local persiste
- [ ] Campo `published_to_queue` permite identificar eventos não publicados
- [ ] Job de retry republica eventos pendentes

## Testes de Validação

```sql
-- Verificar auditoria local no schema contas
SELECT * FROM contas.audit_log ORDER BY created_at DESC LIMIT 10;

-- Verificar eventos não publicados
SELECT COUNT(*) FROM contas.audit_log WHERE published_to_queue = FALSE;

-- Verificar auditoria local no schema transacoes
SELECT * FROM transacoes.audit_log ORDER BY created_at DESC LIMIT 10;
```

## Estimativa

**Tempo:** 1 dia (8 horas)

---

**Referências:**
- Tech Spec: Seção "Interceptores de Auditoria"
- PRD: RF-01 a RF-10 (Requisitos de Auditoria)
