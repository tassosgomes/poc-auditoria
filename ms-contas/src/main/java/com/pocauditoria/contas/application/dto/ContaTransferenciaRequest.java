package com.pocauditoria.contas.application.dto;

import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Positive;
import java.math.BigDecimal;
import java.util.UUID;

public record ContaTransferenciaRequest(
        @NotNull UUID contaOrigemId,
        @NotNull UUID contaDestinoId,
        @NotNull @Positive BigDecimal valor
) {
}
