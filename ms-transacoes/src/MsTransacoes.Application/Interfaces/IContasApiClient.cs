using MsTransacoes.Application.DTOs;

namespace MsTransacoes.Application.Interfaces;

public interface IContasApiClient
{
    Task<ContaDTO?> GetContaAsync(string contaId, CancellationToken cancellationToken);
    Task AtualizarSaldoAsync(string contaId, decimal novoSaldo, CancellationToken cancellationToken);
    Task TransferirAsync(Guid contaOrigemId, Guid contaDestinoId, decimal valor, CancellationToken cancellationToken);
}
