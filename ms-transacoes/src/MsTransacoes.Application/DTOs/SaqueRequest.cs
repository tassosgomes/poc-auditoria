namespace MsTransacoes.Application.DTOs;

public sealed class SaqueRequest
{
    public Guid ContaId { get; set; }
    public decimal Valor { get; set; }
    public string? Descricao { get; set; }
}
