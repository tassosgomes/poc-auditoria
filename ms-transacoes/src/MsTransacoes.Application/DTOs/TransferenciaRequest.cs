namespace MsTransacoes.Application.DTOs;

public sealed class TransferenciaRequest
{
    public Guid ContaOrigemId { get; set; }
    public Guid ContaDestinoId { get; set; }
    public decimal Valor { get; set; }
    public string? Descricao { get; set; }
}
