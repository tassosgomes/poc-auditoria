package com.pocauditoria.contas.application.dto;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.Size;

public record UsuarioUpdateRequest(
        @Size(max = 100) String nome,
        @Email @Size(max = 150) String email,
        Boolean ativo,
        @Size(min = 3, max = 255) String senha
) {
}
