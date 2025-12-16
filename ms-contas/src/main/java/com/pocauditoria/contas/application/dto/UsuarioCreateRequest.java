package com.pocauditoria.contas.application.dto;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record UsuarioCreateRequest(
        @NotBlank @Size(max = 100) String nome,
        @NotBlank @Email @Size(max = 150) String email,
        @NotBlank @Size(min = 3, max = 255) String senha
) {
}
