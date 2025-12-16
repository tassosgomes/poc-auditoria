using Microsoft.Extensions.Logging;
using MsTransacoes.Application.DTOs;
using MsTransacoes.Application.Exceptions;
using MsTransacoes.Application.Interfaces;
using MsTransacoes.Application.Mappings;
using MsTransacoes.Domain.Entities;
using MsTransacoes.Domain.Repositories;

namespace MsTransacoes.Application.Services;

public sealed class TransacaoService : ITransacaoService
{
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IContasApiClient _contasApi;
    private readonly ILogger<TransacaoService> _logger;

    public TransacaoService(
        ITransacaoRepository transacaoRepository,
        IContasApiClient contasApi,
        ILogger<TransacaoService> logger)
    {
        _transacaoRepository = transacaoRepository;
        _contasApi = contasApi;
        _logger = logger;
    }

    public async Task<TransacaoDTO> RealizarDepositoAsync(DepositoRequest request, CancellationToken cancellationToken)
    {
        if (request.Valor <= 0)
        {
            throw new BusinessException("Valor deve ser maior que zero");
        }

        var conta = await _contasApi.GetContaAsync(request.ContaId.ToString(), cancellationToken)
            ?? throw new NotFoundException("Conta não encontrada");

        var transacao = new Transacao
        {
            ContaOrigemId = request.ContaId,
            Tipo = TipoTransacao.DEPOSITO,
            Valor = request.Valor,
            Descricao = request.Descricao,
            Status = StatusTransacao.PENDENTE
        };

        await _transacaoRepository.AddAsync(transacao, cancellationToken);
        await _transacaoRepository.SaveChangesAsync(cancellationToken);

        try
        {
            var novoSaldo = conta.Saldo + request.Valor;
            await _contasApi.AtualizarSaldoAsync(request.ContaId.ToString(), novoSaldo, cancellationToken);

            transacao.Status = StatusTransacao.CONCLUIDA;
            transacao.ProcessadoEm = DateTime.UtcNow;
            await _transacaoRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao atualizar saldo no MS-Contas durante depósito");
            transacao.Status = StatusTransacao.CANCELADA;
            transacao.ProcessadoEm = DateTime.UtcNow;
            await _transacaoRepository.SaveChangesAsync(cancellationToken);
            throw;
        }

        return transacao.ToDto();
    }

    public async Task<TransacaoDTO> RealizarSaqueAsync(SaqueRequest request, CancellationToken cancellationToken)
    {
        if (request.Valor <= 0)
        {
            throw new BusinessException("Valor deve ser maior que zero");
        }

        var conta = await _contasApi.GetContaAsync(request.ContaId.ToString(), cancellationToken)
            ?? throw new NotFoundException("Conta não encontrada");

        if (conta.Saldo < request.Valor)
        {
            throw new BusinessException("Saldo insuficiente");
        }

        var transacao = new Transacao
        {
            ContaOrigemId = request.ContaId,
            Tipo = TipoTransacao.SAQUE,
            Valor = request.Valor,
            Descricao = request.Descricao,
            Status = StatusTransacao.PENDENTE
        };

        await _transacaoRepository.AddAsync(transacao, cancellationToken);
        await _transacaoRepository.SaveChangesAsync(cancellationToken);

        try
        {
            var novoSaldo = conta.Saldo - request.Valor;
            await _contasApi.AtualizarSaldoAsync(request.ContaId.ToString(), novoSaldo, cancellationToken);

            transacao.Status = StatusTransacao.CONCLUIDA;
            transacao.ProcessadoEm = DateTime.UtcNow;
            await _transacaoRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao atualizar saldo no MS-Contas durante saque");
            transacao.Status = StatusTransacao.CANCELADA;
            transacao.ProcessadoEm = DateTime.UtcNow;
            await _transacaoRepository.SaveChangesAsync(cancellationToken);
            throw;
        }

        return transacao.ToDto();
    }

    public async Task<TransacaoDTO> RealizarTransferenciaAsync(TransferenciaRequest request, CancellationToken cancellationToken)
    {
        if (request.Valor <= 0)
        {
            throw new BusinessException("Valor deve ser maior que zero");
        }

        var transacao = new Transacao
        {
            ContaOrigemId = request.ContaOrigemId,
            ContaDestinoId = request.ContaDestinoId,
            Tipo = TipoTransacao.TRANSFERENCIA,
            Valor = request.Valor,
            Descricao = request.Descricao,
            Status = StatusTransacao.PENDENTE
        };

        await _transacaoRepository.AddAsync(transacao, cancellationToken);
        await _transacaoRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _contasApi.TransferirAsync(
                request.ContaOrigemId,
                request.ContaDestinoId,
                request.Valor,
                cancellationToken);

            transacao.Status = StatusTransacao.CONCLUIDA;
            transacao.ProcessadoEm = DateTime.UtcNow;
            await _transacaoRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao atualizar saldos no MS-Contas durante transferência");
            transacao.Status = StatusTransacao.CANCELADA;
            transacao.ProcessadoEm = DateTime.UtcNow;
            await _transacaoRepository.SaveChangesAsync(cancellationToken);
            throw;
        }

        return transacao.ToDto();
    }

    public async Task<IReadOnlyList<TransacaoDTO>> ListarPorContaAsync(Guid contaId, CancellationToken cancellationToken)
    {
        var transacoes = await _transacaoRepository.ListByContaAsync(contaId, cancellationToken);
        return transacoes.Select(t => t.ToDto()).ToList();
    }

    public async Task<TransacaoDTO> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var transacao = await _transacaoRepository.GetByIdAsync(id, cancellationToken);

        if (transacao is null)
        {
            throw new NotFoundException("Transação não encontrada");
        }

        return transacao.ToDto();
    }
}
