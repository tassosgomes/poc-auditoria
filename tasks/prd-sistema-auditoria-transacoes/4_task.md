---
status: completed
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>backend/dotnet</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>rabbitmq, elasticsearch</dependencies>
<unblocks>5.0</unblocks>
</task_context>

# Tarefa 4.0: MS-Auditoria (.NET 8) ✅ CONCLUÍDA

## Visão Geral

Desenvolver o microserviço de auditoria em .NET 8. Este serviço consome eventos da fila RabbitMQ e os persiste no Elasticsearch, além de expor uma API REST para consulta dos eventos de auditoria.

<requirements>
- .NET 8 SDK instalado
- Infraestrutura Docker rodando (Tarefa 1.0)
- Conhecimento de ASP.NET Core, RabbitMQ, Elasticsearch
</requirements>

## Subtarefas

- [x] 4.1 Setup projeto .NET 8 com Clean Architecture ✅
- [x] 4.2 Configurar cliente Elasticsearch ✅
- [x] 4.3 Criar DTOs de auditoria ✅
- [x] 4.4 Implementar consumer RabbitMQ (BackgroundService) ✅
- [x] 4.5 Implementar serviço de persistência no Elasticsearch ✅
- [x] 4.6 Criar índices do Elasticsearch via código ✅
- [x] 4.7 Criar controller REST para consulta de auditoria ✅
- [x] 4.8 Implementar filtros de consulta ✅
- [x] 4.9 Configurar Swagger/OpenAPI ✅
- [x] 4.10 Implementar middleware de autenticação ✅
- [x] 4.11 Criar Dockerfile ✅
- [x] 4.12 Testar fluxo completo (publicação → consumo → consulta) ✅

## Checklist de Conclusão

- [x] 4.0 MS-Auditoria (.NET 8) ✅ CONCLUÍDA
    - [x] 4.0.1 Implementação completada
    - [x] 4.0.2 Definição da tarefa, PRD e tech spec validados
    - [x] 4.0.3 Análise de regras e conformidade verificadas
    - [x] 4.0.4 Revisão de código completada
    - [x] 4.0.5 Pronto para deploy

## Sequenciamento

- **Bloqueado por:** 1.0 (Infraestrutura)
- **Desbloqueia:** 5.0 (Frontend)
- **Paralelizável:** Sim, pode ser desenvolvido em paralelo com 2.0 e 3.0

## Detalhes de Implementação

### 4.1 Estrutura do Projeto

```
ms-auditoria/
├── MsAuditoria.sln
├── Dockerfile
└── src/
    ├── MsAuditoria.API/
    │   ├── MsAuditoria.API.csproj
    │   ├── Program.cs
    │   ├── Controllers/
    │   │   └── AuditController.cs
    │   └── Middleware/
    │       └── SimpleAuthMiddleware.cs
    ├── MsAuditoria.Application/
    │   ├── MsAuditoria.Application.csproj
    │   ├── DTOs/
    │   │   ├── AuditEventDTO.cs
    │   │   └── AuditQueryParams.cs
    │   └── Services/
    │       ├── IAuditService.cs
    │       └── AuditService.cs
    └── MsAuditoria.Infra/
        ├── MsAuditoria.Infra.csproj
        ├── Elasticsearch/
        │   ├── ElasticsearchService.cs
        │   └── IndexInitializer.cs
        └── Messaging/
            └── AuditEventConsumer.cs
```

### 4.3 DTO de Auditoria

```csharp
namespace MsAuditoria.Application.DTOs;

public record AuditEventDTO
{
    public string Id { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Operation { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public Dictionary<string, object?>? OldValues { get; init; }
    public Dictionary<string, object?>? NewValues { get; init; }
    public List<string>? ChangedFields { get; init; }
    public string SourceService { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}

public record AuditQueryParams
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Operation { get; init; }
    public string? EntityName { get; init; }
    public string? UserId { get; init; }
    public string? SourceService { get; init; }
    public string? CorrelationId { get; init; }
}
```

### 4.4 Consumer RabbitMQ (BackgroundService)

```csharp
namespace MsAuditoria.Infra.Messaging;

public class AuditEventConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditEventConsumer> _logger;

    private const string QueueName = "audit-queue";

    public AuditEventConsumer(
        IConfiguration config,
        IServiceProvider serviceProvider,
        ILogger<AuditEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:User"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Garantir que a fila existe
        _channel.QueueDeclare(QueueName, durable: true, 
            exclusive: false, autoDelete: false);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                var auditEvent = JsonSerializer.Deserialize<AuditEventDTO>(message);
                
                if (auditEvent != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var elasticService = scope.ServiceProvider
                        .GetRequiredService<IElasticsearchService>();
                    
                    await elasticService.IndexEventAsync(auditEvent);
                    
                    _logger.LogInformation(
                        "Evento de auditoria processado: {EventId}", 
                        auditEvent.Id);
                }
                
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento de auditoria");
                // Rejeitar e enviar para DLQ
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Consumer de auditoria iniciado");
        
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
```

### 4.5 Serviço Elasticsearch

```csharp
namespace MsAuditoria.Infra.Elasticsearch;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(IConfiguration config, 
        ILogger<ElasticsearchService> logger)
    {
        _logger = logger;
        
        var settings = new ElasticsearchClientSettings(
            new Uri(config["Elasticsearch:Url"] ?? "http://localhost:9200"))
            .DefaultIndex("audit-events");
        
        _client = new ElasticsearchClient(settings);
    }

    public async Task IndexEventAsync(AuditEventDTO auditEvent)
    {
        // Determinar índice baseado no serviço de origem
        var indexName = $"audit-{auditEvent.SourceService.ToLowerInvariant()}";
        
        var response = await _client.IndexAsync(auditEvent, idx => idx
            .Index(indexName)
            .Id(auditEvent.Id));

        if (!response.IsValidResponse)
        {
            _logger.LogError("Falha ao indexar evento: {Error}", 
                response.DebugInformation);
            throw new Exception($"Falha ao indexar evento: {response.DebugInformation}");
        }

        _logger.LogDebug("Evento indexado: {EventId} no índice {Index}", 
            auditEvent.Id, indexName);
    }

    public async Task<IEnumerable<AuditEventDTO>> SearchAsync(AuditQueryParams query)
    {
        var searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
            .Index("audit-*")
            .Query(q => BuildQuery(q, query))
            .Sort(sort => sort.Field(f => f.Timestamp, 
                new FieldSort { Order = SortOrder.Desc }))
            .Size(1000)); // Sem paginação conforme definido

        if (!searchResponse.IsValidResponse)
        {
            _logger.LogError("Falha na busca: {Error}", 
                searchResponse.DebugInformation);
            return Enumerable.Empty<AuditEventDTO>();
        }

        return searchResponse.Documents;
    }

    public async Task<AuditEventDTO?> GetByIdAsync(string id)
    {
        var searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
            .Index("audit-*")
            .Query(q => q.Term(t => t.Field(f => f.Id).Value(id))));

        return searchResponse.Documents.FirstOrDefault();
    }

    public async Task<IEnumerable<AuditEventDTO>> GetByEntityAsync(
        string entityName, string entityId)
    {
        var searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
            .Index("audit-*")
            .Query(q => q.Bool(b => b
                .Must(
                    m => m.Term(t => t.Field(f => f.EntityName).Value(entityName)),
                    m => m.Term(t => t.Field(f => f.EntityId).Value(entityId))
                )))
            .Sort(sort => sort.Field(f => f.Timestamp, 
                new FieldSort { Order = SortOrder.Desc })));

        return searchResponse.Documents;
    }

    public async Task<IEnumerable<AuditEventDTO>> GetByUserAsync(string userId)
    {
        var searchResponse = await _client.SearchAsync<AuditEventDTO>(s => s
            .Index("audit-*")
            .Query(q => q.Term(t => t.Field(f => f.UserId).Value(userId)))
            .Sort(sort => sort.Field(f => f.Timestamp, 
                new FieldSort { Order = SortOrder.Desc })));

        return searchResponse.Documents;
    }

    private Action<QueryDescriptor<AuditEventDTO>> BuildQuery(
        QueryDescriptor<AuditEventDTO> q, AuditQueryParams query)
    {
        var mustClauses = new List<Action<QueryDescriptor<AuditEventDTO>>>();

        if (query.StartDate.HasValue || query.EndDate.HasValue)
        {
            mustClauses.Add(m => m.Range(r => r
                .DateRange(dr =>
                {
                    var range = dr.Field(f => f.Timestamp);
                    if (query.StartDate.HasValue)
                        range.Gte(query.StartDate.Value);
                    if (query.EndDate.HasValue)
                        range.Lte(query.EndDate.Value);
                    return range;
                })));
        }

        if (!string.IsNullOrEmpty(query.Operation))
            mustClauses.Add(m => m.Term(t => t
                .Field(f => f.Operation).Value(query.Operation)));

        if (!string.IsNullOrEmpty(query.EntityName))
            mustClauses.Add(m => m.Term(t => t
                .Field(f => f.EntityName).Value(query.EntityName)));

        if (!string.IsNullOrEmpty(query.UserId))
            mustClauses.Add(m => m.Term(t => t
                .Field(f => f.UserId).Value(query.UserId)));

        if (!string.IsNullOrEmpty(query.SourceService))
            mustClauses.Add(m => m.Term(t => t
                .Field(f => f.SourceService).Value(query.SourceService)));

        if (!string.IsNullOrEmpty(query.CorrelationId))
            mustClauses.Add(m => m.Term(t => t
                .Field(f => f.CorrelationId).Value(query.CorrelationId)));

        if (mustClauses.Any())
        {
            return qd => qd.Bool(b => b.Must(mustClauses.ToArray()));
        }

        return qd => qd.MatchAll(new MatchAllQuery());
    }
}
```

### 4.6 Inicialização de Índices

```csharp
namespace MsAuditoria.Infra.Elasticsearch;

public class IndexInitializer : IHostedService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<IndexInitializer> _logger;
    
    private readonly string[] _indices = { "audit-ms-contas", "audit-ms-transacoes" };

    public IndexInitializer(IConfiguration config, ILogger<IndexInitializer> logger)
    {
        _logger = logger;
        
        var settings = new ElasticsearchClientSettings(
            new Uri(config["Elasticsearch:Url"] ?? "http://localhost:9200"));
        
        _client = new ElasticsearchClient(settings);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var indexName in _indices)
        {
            var exists = await _client.Indices.ExistsAsync(indexName, cancellationToken);
            
            if (!exists.Exists)
            {
                var createResponse = await _client.Indices.CreateAsync(indexName, c => c
                    .Mappings(m => m
                        .Properties<AuditEventDTO>(p => p
                            .Keyword(k => k.Name(n => n.Id))
                            .Date(d => d.Name(n => n.Timestamp))
                            .Keyword(k => k.Name(n => n.Operation))
                            .Keyword(k => k.Name(n => n.EntityName))
                            .Keyword(k => k.Name(n => n.EntityId))
                            .Keyword(k => k.Name(n => n.UserId))
                            .Object(o => o.Name(n => n.OldValues).Enabled(true))
                            .Object(o => o.Name(n => n.NewValues).Enabled(true))
                            .Keyword(k => k.Name(n => n.ChangedFields))
                            .Keyword(k => k.Name(n => n.SourceService))
                            .Keyword(k => k.Name(n => n.CorrelationId))
                        ))
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(0)),
                    cancellationToken);

                if (createResponse.IsValidResponse)
                {
                    _logger.LogInformation("Índice {Index} criado com sucesso", indexName);
                }
                else
                {
                    _logger.LogError("Falha ao criar índice {Index}: {Error}", 
                        indexName, createResponse.DebugInformation);
                }
            }
            else
            {
                _logger.LogInformation("Índice {Index} já existe", indexName);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

### 4.7 Controller de Auditoria

```csharp
namespace MsAuditoria.API.Controllers;

[ApiController]
[Route("api/v1/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Listar eventos de auditoria com filtros
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditEventDTO>>> GetAll(
        [FromQuery] AuditQueryParams query)
    {
        var events = await _auditService.SearchAsync(query);
        return Ok(events);
    }

    /// <summary>
    /// Buscar evento por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AuditEventDTO>> GetById(string id)
    {
        var auditEvent = await _auditService.GetByIdAsync(id);
        
        if (auditEvent == null)
            return NotFound();
        
        return Ok(auditEvent);
    }

    /// <summary>
    /// Histórico de alterações de uma entidade específica
    /// </summary>
    [HttpGet("entity/{entityName}/{entityId}")]
    public async Task<ActionResult<IEnumerable<AuditEventDTO>>> GetByEntity(
        string entityName, string entityId)
    {
        var events = await _auditService.GetByEntityAsync(entityName, entityId);
        return Ok(events);
    }

    /// <summary>
    /// Eventos de auditoria por usuário
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<AuditEventDTO>>> GetByUser(string userId)
    {
        var events = await _auditService.GetByUserAsync(userId);
        return Ok(events);
    }
}
```

### 4.11 Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MsAuditoria.API/MsAuditoria.API.csproj", "MsAuditoria.API/"]
COPY ["src/MsAuditoria.Application/MsAuditoria.Application.csproj", "MsAuditoria.Application/"]
COPY ["src/MsAuditoria.Infra/MsAuditoria.Infra.csproj", "MsAuditoria.Infra/"]
RUN dotnet restore "MsAuditoria.API/MsAuditoria.API.csproj"
COPY src/ .
RUN dotnet publish "MsAuditoria.API/MsAuditoria.API.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
ENTRYPOINT ["dotnet", "MsAuditoria.API.dll"]
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MS-Auditoria API", Version = "v1" });
});

// Elasticsearch
builder.Services.AddSingleton<IElasticsearchService, ElasticsearchService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// RabbitMQ Consumer
builder.Services.AddHostedService<AuditEventConsumer>();

// Index Initializer
builder.Services.AddHostedService<IndexInitializer>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<SimpleAuthMiddleware>();

app.MapControllers();

app.Run();
```

## Endpoints a Implementar

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GET` | `/api/v1/audit` | Listar eventos com filtros |
| `GET` | `/api/v1/audit/{id}` | Buscar evento por ID |
| `GET` | `/api/v1/audit/entity/{entityName}/{entityId}` | Histórico de entidade |
| `GET` | `/api/v1/audit/user/{userId}` | Eventos por usuário |

## Critérios de Sucesso

- [ ] Consumer RabbitMQ funcionando como BackgroundService
- [ ] Eventos sendo indexados no Elasticsearch corretamente
- [ ] Índices `audit-ms-contas` e `audit-ms-transacoes` criados automaticamente
- [ ] Endpoints REST de consulta funcionando
- [ ] Filtros por data, operação, entidade, usuário e correlationId
- [ ] Swagger UI acessível em `http://localhost:5001/swagger`
- [ ] Container Docker buildando e executando

## Estimativa

**Tempo:** 1.5 dias (12 horas)

---

**Referências:**
- Tech Spec: Seção "MS-Auditoria (.NET)"
- PRD: RF-27 a RF-31
- Rules: `dotnet-architecture.md`
