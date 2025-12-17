package com.pocauditoria.contas.infra.audit;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.pocauditoria.contas.application.dto.AuditEventDTO;
import com.pocauditoria.contas.domain.entity.AuditLog;
import com.pocauditoria.contas.domain.entity.Conta;
import com.pocauditoria.contas.domain.entity.Usuario;
import com.pocauditoria.contas.domain.repository.AuditLogRepository;
import com.pocauditoria.contas.infra.context.UserContextHolder;
import java.time.Instant;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
import org.hibernate.Hibernate;
import org.hibernate.collection.spi.PersistentCollection;
import org.hibernate.event.spi.PostDeleteEvent;
import org.hibernate.event.spi.PostDeleteEventListener;
import org.hibernate.event.spi.PostInsertEvent;
import org.hibernate.event.spi.PostInsertEventListener;
import org.hibernate.event.spi.PostUpdateEvent;
import org.hibernate.event.spi.PostUpdateEventListener;
import org.hibernate.persister.entity.EntityPersister;
import org.hibernate.proxy.HibernateProxy;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.ObjectProvider;
import com.pocauditoria.contas.application.service.AuditService;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Component;
import org.springframework.transaction.support.TransactionSynchronization;
import org.springframework.transaction.support.TransactionSynchronizationManager;

@Component
public class AuditEventListener implements
        PostInsertEventListener,
        PostUpdateEventListener,
        PostDeleteEventListener {

    private static final Logger logger = LoggerFactory.getLogger(AuditEventListener.class);

    private final ObjectProvider<AuditLogRepository> auditLogRepositoryProvider;
    private final ObjectProvider<AuditService> auditServiceProvider;
    private final AuditEventPublisher publisher;
    private final ObjectMapper objectMapper;
    private final JdbcTemplate jdbcTemplate;

    public AuditEventListener(
            ObjectProvider<AuditLogRepository> auditLogRepositoryProvider,
            ObjectProvider<AuditService> auditServiceProvider,
            AuditEventPublisher publisher,
            ObjectMapper objectMapper,
            JdbcTemplate jdbcTemplate) {
        this.auditLogRepositoryProvider = auditLogRepositoryProvider;
        this.auditServiceProvider = auditServiceProvider;
        this.publisher = publisher;
        this.objectMapper = objectMapper;
        this.jdbcTemplate = jdbcTemplate;
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
        return (entity instanceof Usuario || entity instanceof Conta) && !(entity instanceof AuditLog);
    }

    private AuditLog createAuditLog(Object entity, String operation, Object[] oldState, Object[] newState, EntityPersister persister) {
        AuditLog auditLog = new AuditLog();
        auditLog.setEntityName(entity.getClass().getSimpleName());
        auditLog.setEntityId(getEntityId(entity));
        auditLog.setOperation(operation);
        auditLog.setOldValues(toJson(toMap(oldState, persister)));
        auditLog.setNewValues(toJson(toMap(newState, persister)));
        auditLog.setUserId(UserContextHolder.getCurrentUserId());
        auditLog.setCorrelationId(parseCorrelationId(UserContextHolder.getCorrelationId()));
        auditLog.setSourceService("ms-contas");
        auditLog.setPublishedToQueue(false);
        auditLog.setCreatedAt(OffsetDateTime.now());
        return auditLog;
    }

    private void saveAndPublishAfterCommit(AuditLog auditLog) {
        // 1. Salvar em NOVA transação via JDBC para evitar ConcurrentModificationException
        try {
            if (auditLog.getId() == null) {
                auditLog.setId(UUID.randomUUID());
            }
            AuditService auditService = auditServiceProvider.getObject();
            auditService.saveInNewTransaction(auditLog);
        } catch (Exception e) {
            logger.error("Erro ao salvar log de auditoria", e);
            throw e; 
        }

        // 2. Publicar no RabbitMQ APÓS o commit da transação PRINCIPAL
        if (TransactionSynchronizationManager.isSynchronizationActive()) {
            TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
                @Override
                public void afterCommit() {
                    try {
                        AuditEventDTO auditEvent = toAuditEvent(auditLog);
                        publisher.publishAsync(auditEvent);
                        
                        // Marcar como publicado via JDBC direto (nova transação implícita)
                        jdbcTemplate.update("UPDATE contas.audit_log SET published_to_queue = true WHERE id = ?", auditLog.getId());
                        
                    } catch (Exception e) {
                        logger.error("Falha ao publicar no RabbitMQ: " + e.getMessage(), e);
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
            map.put(propertyNames[i], normalizeValue(state[i]));
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

    private AuditEventDTO toAuditEvent(AuditLog log) {
        try {
            Map<String, Object> oldValues = log.getOldValues() != null 
                ? objectMapper.readValue(log.getOldValues(), Map.class) 
                : Map.of();
            Map<String, Object> newValues = log.getNewValues() != null 
                ? objectMapper.readValue(log.getNewValues(), Map.class) 
                : Map.of();
            
            return AuditEventDTO.builder()
                .id(log.getId().toString())
                .timestamp(log.getCreatedAt().toInstant())
                .operation(log.getOperation())
                .entityName(log.getEntityName())
                .entityId(log.getEntityId().toString())
                .userId(log.getUserId())
                .oldValues(oldValues)
                .newValues(newValues)
                .changedFields(computeChangedFields(oldValues, newValues))
                .sourceService(log.getSourceService())
                .correlationId(log.getCorrelationId() != null ? log.getCorrelationId().toString() : null)
                .build();
        } catch (Exception e) {
            logger.error("Erro ao converter AuditLog para AuditEventDTO", e);
            return null;
        }
    }

    private UUID parseCorrelationId(String correlationId) {
        try {
            return correlationId != null ? UUID.fromString(correlationId) : null;
        } catch (Exception e) {
            return null;
        }
    }

    private List<String> computeChangedFields(Map<String, Object> oldValues, Map<String, Object> newValues) {
        if (oldValues.isEmpty() || newValues.isEmpty()) {
            return List.of();
        }
        List<String> changed = new ArrayList<>();
        for (String key : newValues.keySet()) {
            if (!Objects.equals(oldValues.get(key), newValues.get(key))) {
                changed.add(key);
            }
        }
        return changed;
    }

    private Object normalizeValue(Object value) {
        if (value == null) {
            return null;
        }
        if (value instanceof HibernateProxy proxy) {
            return proxy.getHibernateLazyInitializer().getIdentifier();
        }
        if (value instanceof PersistentCollection) {
            return "collection";
        }
        if (value instanceof Usuario usuario) {
            return usuario.getId();
        }
        if (value instanceof Conta conta) {
            return conta.getId();
        }
        if (value instanceof Enum<?> e) {
            return e.name();
        }
        if (Hibernate.isInitialized(value)) {
            return value;
        }
        return value.toString();
    }

    @Override
    public boolean requiresPostCommitHandling(EntityPersister persister) {
        return false; // Usamos TransactionSynchronization manualmente
    }
}
