using Microsoft.AspNetCore.Mvc;
using MsAuditoria.Application.DTOs;
using MsAuditoria.Application.Interfaces;

namespace MsAuditoria.API.Controllers;

/// <summary>
/// Controller para consulta de eventos de auditoria
/// </summary>
[ApiController]
[Route("api/v1/audit")]
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditService auditService,
        ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Listar eventos de auditoria com filtros
    /// </summary>
    /// <param name="query">Parâmetros de filtro</param>
    /// <returns>Lista de eventos de auditoria</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuditEventDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuditEventDTO>>> GetAll(
        [FromQuery] AuditQueryParams query)
    {
        _logger.LogInformation("Buscando eventos de auditoria com filtros: {@Query}", query);
        
        var events = await _auditService.SearchAsync(query);
        return Ok(events);
    }

    /// <summary>
    /// Buscar evento por ID
    /// </summary>
    /// <param name="id">ID do evento</param>
    /// <returns>Evento de auditoria</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AuditEventDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditEventDTO>> GetById(string id)
    {
        _logger.LogInformation("Buscando evento de auditoria por ID: {Id}", id);
        
        var auditEvent = await _auditService.GetByIdAsync(id);

        if (auditEvent == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "about:blank",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Evento de auditoria com ID '{id}' não encontrado",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(auditEvent);
    }

    /// <summary>
    /// Histórico de alterações de uma entidade específica
    /// </summary>
    /// <param name="entityName">Nome da entidade</param>
    /// <param name="entityId">ID da entidade</param>
    /// <returns>Lista de eventos de auditoria da entidade</returns>
    [HttpGet("entity/{entityName}/{entityId}")]
    [ProducesResponseType(typeof(IEnumerable<AuditEventDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuditEventDTO>>> GetByEntity(
        string entityName, string entityId)
    {
        _logger.LogInformation("Buscando histórico da entidade {EntityName}/{EntityId}", 
            entityName, entityId);
        
        var events = await _auditService.GetByEntityAsync(entityName, entityId);
        return Ok(events);
    }

    /// <summary>
    /// Eventos de auditoria por usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Lista de eventos de auditoria do usuário</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<AuditEventDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuditEventDTO>>> GetByUser(string userId)
    {
        _logger.LogInformation("Buscando eventos do usuário {UserId}", userId);
        
        var events = await _auditService.GetByUserAsync(userId);
        return Ok(events);
    }
}
