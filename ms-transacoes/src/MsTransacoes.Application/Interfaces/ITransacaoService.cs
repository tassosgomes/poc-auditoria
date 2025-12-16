using MsTransacoes.Application.DTOs;

namespace MsTransacoes.Application.Interfaces;

public interface ITransacaoService
{
    Task<TransacaoDTO> RealizarDepositoAsync(DepositoRequest request, CancellationToken cancellationToken);
    Task<TransacaoDTO> RealizarSaqueAsync(SaqueRequest request, CancellationToken cancellationToken);
    Task<TransacaoDTO> RealizarTransferenciaAsync(TransferenciaRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransacaoDTO>> ListarPorContaAsync(Guid contaId, CancellationToken cancellationToken);
    Task<TransacaoDTO> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken);
}
