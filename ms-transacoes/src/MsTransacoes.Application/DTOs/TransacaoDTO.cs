namespace MsTransacoes.Application.DTOs;

public sealed class TransacaoDTO
{
    public Guid Id { get; set; }
    public Guid ContaOrigemId { get; set; }
    public Guid? ContaDestinoId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string? Descricao { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public DateTime? ProcessadoEm { get; set; }
}
