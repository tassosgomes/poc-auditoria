package com.pocauditoria.contas.application.dto;

import java.time.LocalDateTime;
import java.util.UUID;

public record UsuarioResponse(
        UUID id,
        String nome,
        String email,
        Boolean ativo,
        LocalDateTime criadoEm,
        LocalDateTime atualizadoEm
) {
}
