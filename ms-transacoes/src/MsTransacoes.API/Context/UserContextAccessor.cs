using System.Threading;
using MsTransacoes.Application.Interfaces;

namespace MsTransacoes.API.Context;

public sealed class UserContextAccessor : IUserContextAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string? GetCurrentUserId()
    {
        return Current.Value;
    }

    public void SetCurrentUserId(string? userId)
    {
        Current.Value = userId;
    }
}
