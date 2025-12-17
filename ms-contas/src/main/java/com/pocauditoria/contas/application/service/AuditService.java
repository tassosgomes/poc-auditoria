package com.pocauditoria.contas.application.service;

import com.pocauditoria.contas.domain.entity.AuditLog;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Propagation;
import org.springframework.transaction.annotation.Transactional;

@Service
public class AuditService {

    private final JdbcTemplate jdbcTemplate;

    public AuditService(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    @Transactional(propagation = Propagation.REQUIRES_NEW)
    public AuditLog saveInNewTransaction(AuditLog auditLog) {
        String sql = """
            INSERT INTO contas.audit_log (
                id, entity_name, entity_id, operation, old_values, new_values, 
                user_id, correlation_id, source_service, published_to_queue, created_at
            ) VALUES (?, ?, ?, ?, ?::jsonb, ?::jsonb, ?, ?, ?, ?, ?)
        """;
        
        jdbcTemplate.update(sql,
            auditLog.getId() != null ? auditLog.getId() : java.util.UUID.randomUUID(),
            auditLog.getEntityName(),
            auditLog.getEntityId(),
            auditLog.getOperation(),
            auditLog.getOldValues(),
            auditLog.getNewValues(),
            auditLog.getUserId(),
            auditLog.getCorrelationId(),
            auditLog.getSourceService(),
            auditLog.getPublishedToQueue(),
            auditLog.getCreatedAt()
        );
        
        return auditLog;
    }
}
