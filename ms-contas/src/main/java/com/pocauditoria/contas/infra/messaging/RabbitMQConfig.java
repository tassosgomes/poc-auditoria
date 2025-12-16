package com.pocauditoria.contas.infra.messaging;

import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.DirectExchange;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitAdmin;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.DependsOn;

@Configuration
public class RabbitMQConfig {

    private static final Logger log = LoggerFactory.getLogger(RabbitMQConfig.class);

    @Bean
    public RabbitAdmin rabbitAdmin(ConnectionFactory connectionFactory,
                                   DirectExchange auditExchange,
                                   Queue auditQueue,
                                   Queue auditErrorQueue,
                                   Binding auditBinding,
                                   Binding auditErrorBinding) {
        RabbitAdmin admin = new RabbitAdmin(connectionFactory);
        
        // Forçar declaração dos componentes
        admin.declareExchange(auditExchange);
        admin.declareQueue(auditQueue);
        admin.declareQueue(auditErrorQueue);
        admin.declareBinding(auditBinding);
        admin.declareBinding(auditErrorBinding);
        
        log.info("RabbitMQ: Exchange, filas e bindings declarados com sucesso");
        return admin;
    }

    @Bean
    public DirectExchange auditExchange() {
        return new DirectExchange(RabbitMQConstants.EXCHANGE, true, false);
    }

    @Bean
    public Queue auditQueue() {
        return new Queue(
                RabbitMQConstants.QUEUE,
                true,
                false,
                false,
                java.util.Map.of(
                        "x-dead-letter-exchange", RabbitMQConstants.EXCHANGE,
                        "x-dead-letter-routing-key", RabbitMQConstants.ERROR_ROUTING_KEY
                )
        );
    }

    @Bean
    public Queue auditErrorQueue() {
        return new Queue(RabbitMQConstants.ERROR_QUEUE, true);
    }

    @Bean
    public Binding auditBinding(DirectExchange auditExchange, Queue auditQueue) {
        return BindingBuilder.bind(auditQueue).to(auditExchange).with(RabbitMQConstants.ROUTING_KEY);
    }

    @Bean
    public Binding auditErrorBinding(DirectExchange auditExchange, Queue auditErrorQueue) {
        return BindingBuilder.bind(auditErrorQueue).to(auditExchange).with(RabbitMQConstants.ERROR_ROUTING_KEY);
    }
}
