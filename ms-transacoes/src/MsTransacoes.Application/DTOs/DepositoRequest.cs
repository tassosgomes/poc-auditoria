namespace MsTransacoes.Application.DTOs;

public sealed class DepositoRequest
{
    public Guid ContaId { get; set; }
    public decimal Valor { get; set; }
    public string? Descricao { get; set; }
}
