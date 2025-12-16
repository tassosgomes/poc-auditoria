package com.pocauditoria.contas.infra.audit;

import java.util.List;
import java.util.Map;
import org.hibernate.jpa.boot.spi.IntegratorProvider;
import org.springframework.boot.autoconfigure.orm.jpa.HibernatePropertiesCustomizer;
import org.springframework.stereotype.Component;

@Component
public class HibernateListenerConfigurer implements HibernatePropertiesCustomizer {

    private final AuditEventListener auditEventListener;

    public HibernateListenerConfigurer(AuditEventListener auditEventListener) {
        this.auditEventListener = auditEventListener;
    }

    @Override
    public void customize(Map<String, Object> hibernateProperties) {
        hibernateProperties.put("hibernate.integrator_provider",
            (IntegratorProvider) () -> List.of(new AuditIntegrator(auditEventListener)));
    }
}
