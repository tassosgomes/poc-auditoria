package com.pocauditoria.contas.domain.repository;

import com.pocauditoria.contas.domain.entity.Conta;
import java.util.List;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

public interface ContaRepository extends JpaRepository<Conta, UUID> {
    List<Conta> findByUsuarioId(UUID usuarioId);
}
