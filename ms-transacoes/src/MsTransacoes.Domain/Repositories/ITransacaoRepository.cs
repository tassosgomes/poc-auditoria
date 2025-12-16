using MsTransacoes.Domain.Entities;

namespace MsTransacoes.Domain.Repositories;

public interface ITransacaoRepository
{
    Task AddAsync(Transacao transacao, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<Transacao?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Transacao>> ListByContaAsync(Guid contaId, CancellationToken cancellationToken);
}
