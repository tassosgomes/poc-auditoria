using System.Threading;
using MsTransacoes.Application.Interfaces;

namespace MsTransacoes.API.Context;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string? GetCorrelationId()
    {
        return Current.Value;
    }

    public void SetCorrelationId(string? correlationId)
    {
        Current.Value = correlationId;
    }
}
