using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MsTransacoes.Application.DTOs;
using MsTransacoes.Application.Exceptions;
using MsTransacoes.Application.Interfaces;

namespace MsTransacoes.Infra.ExternalServices;

public sealed class ContasApiClient : IContasApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContasApiClient> _logger;

    public ContasApiClient(
        HttpClient httpClient,
        ICorrelationIdAccessor correlationId,
        IConfiguration configuration,
        ILogger<ContasApiClient> logger)
    {
        _httpClient = httpClient;
        _correlationId = correlationId;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ContaDTO?> GetContaAsync(string contaId, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/contas/{contaId}");
        AddHeaders(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ContaDTO>(cancellationToken: cancellationToken);
    }

    public async Task AtualizarSaldoAsync(string contaId, decimal novoSaldo, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/contas/{contaId}/saldo")
        {
            Content = JsonContent.Create(new { saldo = novoSaldo })
        };
        AddHeaders(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Conta não encontrada");
        }

        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Saldo atualizado para conta {ContaId}: {NovoSaldo}", contaId, novoSaldo);
    }

    public async Task TransferirAsync(Guid contaOrigemId, Guid contaDestinoId, decimal valor, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/contas/transferencia")
        {
            Content = JsonContent.Create(new
            {
                contaOrigemId,
                contaDestinoId,
                valor
            })
        };
        AddHeaders(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Conta não encontrada");
        }

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            throw new BusinessException("Saldo insuficiente");
        }

        response.EnsureSuccessStatusCode();
    }

    private void AddHeaders(HttpRequestMessage request)
    {
        var correlationId = _correlationId.GetCorrelationId();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Add("X-Correlation-Id", correlationId);
        }

        var user = _configuration["Services:MsContasAuth:User"] ?? "admin";
        var password = _configuration["Services:MsContasAuth:Password"] ?? "admin123";
        var raw = $"{user}:{password}";
        request.Headers.Authorization = new AuthenticationHeaderValue(
            scheme: "Basic",
            parameter: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw)));
    }
}
