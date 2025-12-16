package com.pocauditoria.contas.infra.audit;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.pocauditoria.contas.application.dto.AuditEventDTO;
import com.pocauditoria.contas.infra.messaging.RabbitMQConstants;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Component;

@Component
public class AuditEventPublisher {

    private static final Logger logger = LoggerFactory.getLogger(AuditEventPublisher.class);

    private final RabbitTemplate rabbitTemplate;
    private final ObjectMapper objectMapper;

    public AuditEventPublisher(RabbitTemplate rabbitTemplate, ObjectMapper objectMapper) {
        this.rabbitTemplate = rabbitTemplate;
        this.objectMapper = objectMapper;
    }

    @Async
    public void publishAsync(AuditEventDTO event) {
        try {
            String message = objectMapper.writeValueAsString(event);
            rabbitTemplate.convertAndSend(RabbitMQConstants.EXCHANGE, RabbitMQConstants.ROUTING_KEY, message);
            logger.info("Evento de auditoria publicado: {}", event.id());
        } catch (Exception e) {
            logger.error("Falha ao publicar evento, enviando para fila de erro", e);
            sendToErrorQueue(event);
        }
    }

    private void sendToErrorQueue(AuditEventDTO event) {
        try {
            String message = objectMapper.writeValueAsString(event);
            rabbitTemplate.convertAndSend(RabbitMQConstants.EXCHANGE, RabbitMQConstants.ERROR_ROUTING_KEY, message);
        } catch (Exception ex) {
            logger.error("Falha crítica ao enviar para fila de erro", ex);
        }
    }
}
