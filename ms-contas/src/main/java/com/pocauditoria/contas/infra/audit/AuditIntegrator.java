package com.pocauditoria.contas.infra.audit;

import org.hibernate.boot.Metadata;
import org.hibernate.engine.spi.SessionFactoryImplementor;
import org.hibernate.event.service.spi.EventListenerRegistry;
import org.hibernate.event.spi.EventType;
import org.hibernate.integrator.spi.Integrator;
import org.hibernate.service.spi.SessionFactoryServiceRegistry;

public class AuditIntegrator implements Integrator {

    private final AuditEventListener auditEventListener;

    public AuditIntegrator(AuditEventListener auditEventListener) {
        this.auditEventListener = auditEventListener;
    }

    @Override
    public void integrate(
            Metadata metadata,
            SessionFactoryImplementor sessionFactory,
            SessionFactoryServiceRegistry serviceRegistry
    ) {
        EventListenerRegistry registry = serviceRegistry.getService(EventListenerRegistry.class);
        registry.appendListeners(EventType.PRE_INSERT, auditEventListener);
        registry.appendListeners(EventType.PRE_UPDATE, auditEventListener);
        registry.appendListeners(EventType.PRE_DELETE, auditEventListener);
    }

    @Override
    public void disintegrate(SessionFactoryImplementor sessionFactory, SessionFactoryServiceRegistry serviceRegistry) {
        // no-op
    }
}
