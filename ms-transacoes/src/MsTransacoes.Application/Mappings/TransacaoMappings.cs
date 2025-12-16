using MsTransacoes.Application.DTOs;
using MsTransacoes.Domain.Entities;

namespace MsTransacoes.Application.Mappings;

public static class TransacaoMappings
{
    public static TransacaoDTO ToDto(this Transacao transacao)
    {
        return new TransacaoDTO
        {
            Id = transacao.Id,
            ContaOrigemId = transacao.ContaOrigemId,
            ContaDestinoId = transacao.ContaDestinoId,
            Tipo = transacao.Tipo.ToString(),
            Valor = transacao.Valor,
            Descricao = transacao.Descricao,
            Status = transacao.Status.ToString(),
            CriadoEm = transacao.CriadoEm,
            ProcessadoEm = transacao.ProcessadoEm
        };
    }
}
