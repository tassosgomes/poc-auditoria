namespace MsTransacoes.Domain.Entities;

public sealed class Transacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContaOrigemId { get; set; }
    public Guid? ContaDestinoId { get; set; }
    public TipoTransacao Tipo { get; set; }
    public decimal Valor { get; set; }
    public string? Descricao { get; set; }
    public StatusTransacao Status { get; set; } = StatusTransacao.PENDENTE;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessadoEm { get; set; }
}

public enum TipoTransacao
{
    DEPOSITO,
    SAQUE,
    TRANSFERENCIA
}

public enum StatusTransacao
{
    PENDENTE,
    CONCLUIDA,
    CANCELADA
}
