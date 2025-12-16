package com.pocauditoria.contas.application.dto;

import jakarta.validation.constraints.NotNull;
import java.math.BigDecimal;

public record ContaSaldoUpdateRequest(
        @NotNull BigDecimal saldo
) {
}
