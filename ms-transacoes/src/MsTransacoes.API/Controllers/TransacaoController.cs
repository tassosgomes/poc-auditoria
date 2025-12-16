using Microsoft.AspNetCore.Mvc;
using MsTransacoes.Application.DTOs;
using MsTransacoes.Application.Interfaces;

namespace MsTransacoes.API.Controllers;

[ApiController]
[Route("api/v1/transacoes")]
public sealed class TransacaoController : ControllerBase
{
    private readonly ITransacaoService _service;

    public TransacaoController(ITransacaoService service)
    {
        _service = service;
    }

    [HttpPost("deposito")]
    public async Task<ActionResult<TransacaoDTO>> Deposito([FromBody] DepositoRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.RealizarDepositoAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("saque")]
    public async Task<ActionResult<TransacaoDTO>> Saque([FromBody] SaqueRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.RealizarSaqueAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("transferencia")]
    public async Task<ActionResult<TransacaoDTO>> Transferencia([FromBody] TransferenciaRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.RealizarTransferenciaAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("conta/{contaId:guid}")]
    public async Task<ActionResult<IReadOnlyList<TransacaoDTO>>> ListarPorConta([FromRoute] Guid contaId, CancellationToken cancellationToken)
    {
        var result = await _service.ListarPorContaAsync(contaId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransacaoDTO>> Buscar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.BuscarPorIdAsync(id, cancellationToken);
        return Ok(result);
    }
}
