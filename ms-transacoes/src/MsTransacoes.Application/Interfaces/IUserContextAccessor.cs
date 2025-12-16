namespace MsTransacoes.Application.Interfaces;

public interface IUserContextAccessor
{
    string? GetCurrentUserId();
    void SetCurrentUserId(string? userId);
}
