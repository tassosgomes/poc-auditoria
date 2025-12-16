package com.pocauditoria.contas.infra.audit;

import com.pocauditoria.contas.application.dto.AuditEventDTO;
import com.pocauditoria.contas.domain.entity.Conta;
import com.pocauditoria.contas.domain.entity.Usuario;
import com.pocauditoria.contas.infra.context.UserContextHolder;
import java.time.Instant;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.UUID;
import org.hibernate.Hibernate;
import org.hibernate.collection.spi.PersistentCollection;
import org.hibernate.event.spi.PreDeleteEvent;
import org.hibernate.event.spi.PreDeleteEventListener;
import org.hibernate.event.spi.PreInsertEvent;
import org.hibernate.event.spi.PreInsertEventListener;
import org.hibernate.event.spi.PreUpdateEvent;
import org.hibernate.event.spi.PreUpdateEventListener;
import org.hibernate.proxy.HibernateProxy;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

@Component
public class AuditEventListener implements
        PreInsertEventListener,
        PreUpdateEventListener,
        PreDeleteEventListener {

    private static final Logger logger = LoggerFactory.getLogger(AuditEventListener.class);

    private final AuditEventPublisher publisher;

    public AuditEventListener(AuditEventPublisher publisher) {
        this.publisher = publisher;
    }

    @Override
    public boolean onPreInsert(PreInsertEvent event) {
        if (isAuditableEntity(event.getEntity())) {
            publishAuditEvent(
                    "INSERT",
                    event.getEntity(),
                    null,
                    event.getState(),
                    event.getPersister().getPropertyNames()
            );
        }
        return false;
    }

    @Override
    public boolean onPreUpdate(PreUpdateEvent event) {
        if (isAuditableEntity(event.getEntity())) {
            publishAuditEvent(
                    "UPDATE",
                    event.getEntity(),
                    event.getOldState(),
                    event.getState(),
                    event.getPersister().getPropertyNames()
            );
        }
        return false;
    }

    @Override
    public boolean onPreDelete(PreDeleteEvent event) {
        if (isAuditableEntity(event.getEntity())) {
            publishAuditEvent(
                    "DELETE",
                    event.getEntity(),
                    event.getDeletedState(),
                    null,
                    event.getPersister().getPropertyNames()
            );
        }
        return false;
    }

    private boolean isAuditableEntity(Object entity) {
        return entity instanceof Usuario || entity instanceof Conta;
    }

    private void publishAuditEvent(
            String operation,
            Object entity,
            Object[] oldState,
            Object[] newState,
            String[] propertyNames
    ) {
        try {
            Map<String, Object> oldValues = buildValuesMap(oldState, propertyNames);
            Map<String, Object> newValues = buildValuesMap(newState, propertyNames);
            List<String> changedFields = computeChangedFields(oldValues, newValues);

            var auditEvent = AuditEventDTO.builder()
                    .id(UUID.randomUUID().toString())
                    .timestamp(Instant.now())
                    .operation(operation)
                    .entityName(entity.getClass().getSimpleName())
                    .entityId(extractEntityId(entity))
                    .userId(UserContextHolder.getCurrentUserId())
                    .oldValues(oldValues)
                    .newValues(newValues)
                    .changedFields(changedFields)
                    .sourceService("ms-contas")
                    .correlationId(UserContextHolder.getCorrelationId())
                    .build();

            publisher.publishAsync(auditEvent);
        } catch (Exception e) {
            logger.error("Erro ao publicar evento de auditoria", e);
            // não bloqueia a operação principal
        }
    }

    private Map<String, Object> buildValuesMap(Object[] state, String[] propertyNames) {
        if (state == null || propertyNames == null) {
            return Map.of();
        }
        Map<String, Object> map = new HashMap<>();
        for (int i = 0; i < propertyNames.length && i < state.length; i++) {
            map.put(propertyNames[i], normalizeValue(state[i]));
        }
        return map;
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

    private String extractEntityId(Object entity) {
        try {
            var method = entity.getClass().getMethod("getId");
            var id = method.invoke(entity);
            return id != null ? id.toString() : null;
        } catch (Exception ignored) {
            return null;
        }
    }
}
