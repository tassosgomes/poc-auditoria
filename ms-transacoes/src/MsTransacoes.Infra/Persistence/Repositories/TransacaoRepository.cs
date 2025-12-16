using Microsoft.EntityFrameworkCore;
using MsTransacoes.Domain.Entities;
using MsTransacoes.Domain.Repositories;

namespace MsTransacoes.Infra.Persistence.Repositories;

public sealed class TransacaoRepository : ITransacaoRepository
{
    private readonly TransacoesDbContext _context;

    public TransacaoRepository(TransacoesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transacao transacao, CancellationToken cancellationToken)
    {
        await _context.Transacoes.AddAsync(transacao, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Transacao?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Transacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Transacao>> ListByContaAsync(Guid contaId, CancellationToken cancellationToken)
    {
        return await _context.Transacoes
            .AsNoTracking()
            .Where(t => t.ContaOrigemId == contaId || t.ContaDestinoId == contaId)
            .OrderByDescending(t => t.CriadoEm)
            .ToListAsync(cancellationToken);
    }
}
