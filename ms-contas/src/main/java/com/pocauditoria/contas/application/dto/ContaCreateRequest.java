package com.pocauditoria.contas.application.dto;

import com.pocauditoria.contas.domain.entity.TipoConta;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.math.BigDecimal;
import java.util.UUID;

public record ContaCreateRequest(
        @NotBlank @Size(max = 20) String numeroConta,
        @NotNull UUID usuarioId,
        @NotNull TipoConta tipo,
        BigDecimal saldo
) {
}
