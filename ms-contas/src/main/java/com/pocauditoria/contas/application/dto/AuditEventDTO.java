package com.pocauditoria.contas.application.dto;

import java.time.Instant;
import java.util.List;
import java.util.Map;
import lombok.Builder;

@Builder
public record AuditEventDTO(
        String id,
        Instant timestamp,
        String operation,
        String entityName,
        String entityId,
        String userId,
        Map<String, Object> oldValues,
        Map<String, Object> newValues,
        List<String> changedFields,
        String sourceService,
        String correlationId
) {
}
