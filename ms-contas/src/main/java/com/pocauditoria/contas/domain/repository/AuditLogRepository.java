package com.pocauditoria.contas.domain.repository;

import com.pocauditoria.contas.domain.entity.AuditLog;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.UUID;

@Repository
public interface AuditLogRepository extends JpaRepository<AuditLog, UUID> {
    
    List<AuditLog> findByPublishedToQueueFalse();
    
    @Modifying
    @Query("UPDATE AuditLog a SET a.publishedToQueue = true WHERE a.id = :id")
    void markAsPublished(UUID id);
}
