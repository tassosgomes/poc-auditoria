package com.pocauditoria.contas.infra.messaging;

public final class RabbitMQConstants {

    public static final String EXCHANGE = "audit-events";
    public static final String QUEUE = "audit-queue";
    public static final String ERROR_QUEUE = "audit-error-queue";
    public static final String ROUTING_KEY = "audit";
    public static final String ERROR_ROUTING_KEY = "audit.error";

    private RabbitMQConstants() {
    }
}
