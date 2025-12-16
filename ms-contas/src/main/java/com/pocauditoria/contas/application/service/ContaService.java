package com.pocauditoria.contas.application.service;

import com.pocauditoria.contas.application.dto.ContaCreateRequest;
import com.pocauditoria.contas.application.dto.ContaResponse;
import com.pocauditoria.contas.application.dto.ContaSaldoUpdateRequest;
import com.pocauditoria.contas.application.dto.ContaUpdateRequest;
import com.pocauditoria.contas.domain.entity.Conta;
import com.pocauditoria.contas.domain.repository.ContaRepository;
import com.pocauditoria.contas.domain.repository.UsuarioRepository;
import java.math.BigDecimal;
import java.util.List;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;

@Service
public class ContaService {

    private final ContaRepository contaRepository;
    private final UsuarioRepository usuarioRepository;

    public ContaService(ContaRepository contaRepository, UsuarioRepository usuarioRepository) {
        this.contaRepository = contaRepository;
        this.usuarioRepository = usuarioRepository;
    }

    @Transactional
    public ContaResponse criar(ContaCreateRequest request) {
        var usuario = usuarioRepository.findById(request.usuarioId())
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.BAD_REQUEST, "Usuário inválido"));

        var conta = new Conta();
        conta.setNumeroConta(request.numeroConta());
        conta.setUsuario(usuario);
        conta.setTipo(request.tipo());
        conta.setSaldo(request.saldo() != null ? request.saldo() : BigDecimal.ZERO);
        conta.setAtiva(true);

        var saved = contaRepository.save(conta);
        return toResponse(saved);
    }

    @Transactional(readOnly = true)
    public List<ContaResponse> listar() {
        return contaRepository.findAll().stream().map(this::toResponse).toList();
    }

    @Transactional(readOnly = true)
    public ContaResponse buscar(UUID id) {
        var conta = contaRepository.findById(id)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Conta não encontrada"));
        return toResponse(conta);
    }

    @Transactional(readOnly = true)
    public List<ContaResponse> listarPorUsuario(UUID usuarioId) {
        return contaRepository.findByUsuarioId(usuarioId).stream().map(this::toResponse).toList();
    }

    @Transactional
    public ContaResponse atualizar(UUID id, ContaUpdateRequest request) {
        var conta = contaRepository.findById(id)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Conta não encontrada"));

        if (request.numeroConta() != null) {
            conta.setNumeroConta(request.numeroConta());
        }
        if (request.tipo() != null) {
            conta.setTipo(request.tipo());
        }
        if (request.ativa() != null) {
            conta.setAtiva(request.ativa());
        }

        var saved = contaRepository.save(conta);
        return toResponse(saved);
    }

    @Transactional
    public ContaResponse atualizarSaldo(UUID id, ContaSaldoUpdateRequest request) {
        var conta = contaRepository.findById(id)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Conta não encontrada"));

        conta.setSaldo(request.saldo());
        var saved = contaRepository.save(conta);
        return toResponse(saved);
    }

    @Transactional
    public void excluir(UUID id) {
        var conta = contaRepository.findById(id)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Conta não encontrada"));
        contaRepository.delete(conta);
    }

    private ContaResponse toResponse(Conta conta) {
        return new ContaResponse(
                conta.getId(),
                conta.getNumeroConta(),
                conta.getUsuario() != null ? conta.getUsuario().getId() : null,
                conta.getSaldo(),
                conta.getTipo(),
                conta.getAtiva(),
                conta.getCriadoEm(),
                conta.getAtualizadoEm()
        );
    }
}
