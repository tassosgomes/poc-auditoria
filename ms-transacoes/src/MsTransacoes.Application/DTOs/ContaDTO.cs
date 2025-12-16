namespace MsTransacoes.Application.DTOs;

public sealed class ContaDTO
{
    public Guid Id { get; set; }
    public string NumeroConta { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public decimal Saldo { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public bool Ativa { get; set; }
}
