package com.pocauditoria.contas.application.dto;

import com.pocauditoria.contas.domain.entity.TipoConta;
import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.UUID;

public record ContaResponse(
        UUID id,
        String numeroConta,
        UUID usuarioId,
        BigDecimal saldo,
        TipoConta tipo,
        Boolean ativa,
        LocalDateTime criadoEm,
        LocalDateTime atualizadoEm
) {
}
