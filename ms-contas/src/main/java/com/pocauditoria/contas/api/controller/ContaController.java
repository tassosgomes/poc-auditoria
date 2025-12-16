package com.pocauditoria.contas.api.controller;

import com.pocauditoria.contas.application.dto.ContaCreateRequest;
import com.pocauditoria.contas.application.dto.ContaResponse;
import com.pocauditoria.contas.application.dto.ContaSaldoUpdateRequest;
import com.pocauditoria.contas.application.dto.ContaTransferenciaRequest;
import com.pocauditoria.contas.application.dto.ContaUpdateRequest;
import com.pocauditoria.contas.application.service.ContaService;
import jakarta.validation.Valid;
import java.net.URI;
import java.util.List;
import java.util.UUID;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/v1/contas")
public class ContaController {

    private final ContaService contaService;

    public ContaController(ContaService contaService) {
        this.contaService = contaService;
    }

    @PostMapping
    public ResponseEntity<ContaResponse> criar(@Valid @RequestBody ContaCreateRequest request) {
        var created = contaService.criar(request);
        return ResponseEntity.created(URI.create("/api/v1/contas/" + created.id())).body(created);
    }

    @GetMapping
    public ResponseEntity<List<ContaResponse>> listar() {
        return ResponseEntity.ok(contaService.listar());
    }

    @GetMapping("/{id}")
    public ResponseEntity<ContaResponse> buscar(@PathVariable UUID id) {
        return ResponseEntity.ok(contaService.buscar(id));
    }

    @GetMapping("/usuario/{usuarioId}")
    public ResponseEntity<List<ContaResponse>> listarPorUsuario(@PathVariable UUID usuarioId) {
        return ResponseEntity.ok(contaService.listarPorUsuario(usuarioId));
    }

    @PutMapping("/{id}")
    public ResponseEntity<ContaResponse> atualizar(
            @PathVariable UUID id,
            @Valid @RequestBody ContaUpdateRequest request
    ) {
        return ResponseEntity.ok(contaService.atualizar(id, request));
    }

    @PutMapping("/{id}/saldo")
    public ResponseEntity<ContaResponse> atualizarSaldo(
            @PathVariable UUID id,
            @Valid @RequestBody ContaSaldoUpdateRequest request
    ) {
        return ResponseEntity.ok(contaService.atualizarSaldo(id, request));
    }

    @PostMapping("/transferencia")
    public ResponseEntity<Void> transferir(@Valid @RequestBody ContaTransferenciaRequest request) {
        contaService.transferir(request);
        return ResponseEntity.noContent().build();
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> excluir(@PathVariable UUID id) {
        contaService.excluir(id);
        return ResponseEntity.noContent().build();
    }
}
