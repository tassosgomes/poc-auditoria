package com.pocauditoria.contas.infra.messaging;

import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.DirectExchange;
import org.springframework.amqp.core.Queue;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitMQConfig {

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
