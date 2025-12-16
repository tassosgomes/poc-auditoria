package com.pocauditoria.contas.application.service;

import com.pocauditoria.contas.application.dto.UsuarioCreateRequest;
import com.pocauditoria.contas.application.dto.UsuarioResponse;
import com.pocauditoria.contas.application.dto.UsuarioUpdateRequest;
import com.pocauditoria.contas.domain.entity.Usuario;
import com.pocauditoria.contas.domain.repository.UsuarioRepository;
import java.util.List;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;

@Service
public class UsuarioService {

    private final UsuarioRepository usuarioRepository;

    public UsuarioService(UsuarioRepository usuarioRepository) {
        this.usuarioRepository = usuarioRepository;
    }

    @Transactional
    public UsuarioResponse criar(UsuarioCreateRequest request) {
        usuarioRepository.findByEmail(request.email()).ifPresent(u -> {
            throw new ResponseStatusException(HttpStatus.CONFLICT, "Email já cadastrado");
        });

        var usuario = new Usuario();
        usuario.setNome(request.nome());
        usuario.setEmail(request.email());
        usuario.setSenhaHash(request.senha());
        usuario.setAtivo(true);

        var saved = usuarioRepository.save(usuario);
        return toResponse(saved);
    }

    @Transactional(readOnly = true)
    public List<UsuarioResponse> listar() {
        return usuarioRepository.findAll().stream().map(this::toResponse).toList();
    }

    @Transactional(readOnly = true)
    public UsuarioResponse buscar(UUID id) {
        var usuario = usuarioRepository.findById(id)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Usuário não encontrado"));
        return toResponse(usuario);
    }

    @Transactional
    public UsuarioResponse atualizar(UUID id, UsuarioUpdateRequest request) {
        var usuario = usuarioRepository.findById(id)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Usuário não encontrado"));

        if (request.email() != null && !request.email().equalsIgnoreCase(usuario.getEmail())) {
            usuarioRepository.findByEmail(request.email()).ifPresent(u -> {
                throw new ResponseStatusException(HttpStatus.CONFLICT, "Email já cadastrado");
            });
            usuario.setEmail(request.email());
        }
        if (request.nome() != null) {
            usuario.setNome(request.nome());
        }
        if (request.ativo() != null) {
            usuario.setAtivo(request.ativo());
        }
        if (request.senha() != null) {
            usuario.setSenhaHash(request.senha());
        }

        var saved = usuarioRepository.save(usuario);
        return toResponse(saved);
    }

    @Transactional
    public void excluir(UUID id) {
        var usuario = usuarioRepository.findById(id)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Usuário não encontrado"));
        usuarioRepository.delete(usuario);
    }

    private UsuarioResponse toResponse(Usuario usuario) {
        return new UsuarioResponse(
                usuario.getId(),
                usuario.getNome(),
                usuario.getEmail(),
                usuario.getAtivo(),
                usuario.getCriadoEm(),
                usuario.getAtualizadoEm()
        );
    }
}
