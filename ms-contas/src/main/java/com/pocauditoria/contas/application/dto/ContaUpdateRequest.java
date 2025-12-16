package com.pocauditoria.contas.application.dto;

import com.pocauditoria.contas.domain.entity.TipoConta;
import jakarta.validation.constraints.Size;

public record ContaUpdateRequest(
        @Size(max = 20) String numeroConta,
        TipoConta tipo,
        Boolean ativa
) {
}
