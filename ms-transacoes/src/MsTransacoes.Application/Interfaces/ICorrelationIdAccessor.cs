namespace MsTransacoes.Application.Interfaces;

public interface ICorrelationIdAccessor
{
    string? GetCorrelationId();
    void SetCorrelationId(string? correlationId);
}
