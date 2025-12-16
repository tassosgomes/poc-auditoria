---
status: pending
parallelizable: partial
blocked_by: ["1.0", "2.0"]
---

<task_context>
<domain>backend/dotnet</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>postgresql, rabbitmq, efcore, ms-contas-api</dependencies>
<unblocks>5.0</unblocks>
</task_context>

# Tarefa 3.0: MS-Transações (.NET 8)

## Visão Geral

Desenvolver o microserviço de transações em .NET 8. Este serviço gerencia operações financeiras (depósito, saque, transferência), implementando auditoria automática via EF Core SaveChangesInterceptor. As atualizações de saldo são feitas via API REST do MS-Contas.

<requirements>
- .NET 8 SDK instalado
- Infraestrutura Docker rodando (Tarefa 1.0)
- MS-Contas disponível para integração REST (Tarefa 2.0)
- Conhecimento de ASP.NET Core, EF Core
</requirements>

## Subtarefas

- [ ] 3.1 Setup projeto .NET 8 com Clean Architecture
- [ ] 3.2 Configurar conexão PostgreSQL (schema `transacoes`)
- [ ] 3.3 Criar entidade Transacao e DbContext
- [ ] 3.4 Criar DTOs e serviços de aplicação
- [ ] 3.5 Implementar EF Core SaveChangesInterceptor para auditoria
- [ ] 3.6 Implementar publicador RabbitMQ
- [ ] 3.7 Implementar cliente HTTP para MS-Contas
- [ ] 3.8 Criar controller REST para Transações
- [ ] 3.9 Implementar lógica de depósito, saque e transferência
- [ ] 3.10 Configurar Swagger/OpenAPI
- [ ] 3.11 Implementar middleware de autenticação e CorrelationId
- [ ] 3.12 Criar Dockerfile
- [ ] 3.13 Testar fluxo completo com MS-Contas

## Sequenciamento

- **Bloqueado por:** 1.0, 2.0 (precisa da API de contas)
- **Desbloqueia:** 5.0 (Frontend)
- **Paralelizável:** Parcialmente (pode iniciar estrutura, mas integração depende de 2.0)

## Detalhes de Implementação

### 3.1 Estrutura do Projeto

```
ms-transacoes/
├── MsTransacoes.sln
├── Dockerfile
└── src/
    ├── MsTransacoes.API/
    │   ├── MsTransacoes.API.csproj
    │   ├── Program.cs
    │   ├── Controllers/
    │   │   └── TransacaoController.cs
    │   └── Middleware/
    │       ├── SimpleAuthMiddleware.cs
    │       └── CorrelationIdMiddleware.cs
    ├── MsTransacoes.Application/
    │   ├── MsTransacoes.Application.csproj
    │   ├── DTOs/
    │   │   ├── TransacaoDTO.cs
    │   │   ├── DepositoRequest.cs
    │   │   ├── SaqueRequest.cs
    │   │   ├── TransferenciaRequest.cs
    │   │   └── AuditEventDTO.cs
    │   ├── Services/
    │   │   ├── ITransacaoService.cs
    │   │   └── TransacaoService.cs
    │   └── Interfaces/
    │       └── IContasApiClient.cs
    ├── MsTransacoes.Domain/
    │   ├── MsTransacoes.Domain.csproj
    │   └── Entities/
    │       └── Transacao.cs
    └── MsTransacoes.Infra/
        ├── MsTransacoes.Infra.csproj
        ├── Persistence/
        │   ├── TransacoesDbContext.cs
        │   └── Configurations/
        ├── Audit/
        │   ├── AuditInterceptor.cs
        │   └── IAuditEventPublisher.cs
        ├── Messaging/
        │   └── RabbitMQPublisher.cs
        └── ExternalServices/
            └── ContasApiClient.cs
```

### 3.3 Entidade Transacao

```csharp
namespace MsTransacoes.Domain.Entities;

public class Transacao
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
```

### 3.5 EF Core SaveChangesInterceptor

```csharp
namespace MsTransacoes.Infra.Audit;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IAuditEventPublisher _publisher;
    private readonly IUserContextAccessor _userContext;
    private readonly ICorrelationIdAccessor _correlationId;

    public AuditInterceptor(
        IAuditEventPublisher publisher,
        IUserContextAccessor userContext,
        ICorrelationIdAccessor correlationId)
    {
        _publisher = publisher;
        _userContext = userContext;
        _correlationId = correlationId;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context!;
        var auditEntries = new List<AuditEventDTO>();

        foreach (var entry in context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added 
                     or EntityState.Modified 
                     or EntityState.Deleted))
        {
            var auditEvent = new AuditEventDTO
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Operation = GetOperationName(entry.State),
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetPrimaryKeyValue(entry),
                UserId = _userContext.GetCurrentUserId() ?? "system",
                OldValues = entry.State != EntityState.Added 
                    ? GetValues(entry.OriginalValues) : null,
                NewValues = entry.State != EntityState.Deleted 
                    ? GetValues(entry.CurrentValues) : null,
                ChangedFields = GetChangedFields(entry),
                SourceService = "ms-transacoes",
                CorrelationId = _correlationId.GetCorrelationId()
            };
            
            auditEntries.Add(auditEvent);
        }

        // Fire-and-forget para não bloquear a operação principal
        if (auditEntries.Any())
        {
            _ = Task.Run(() => _publisher.PublishBatchAsync(auditEntries), 
                cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static string GetOperationName(EntityState state) => state switch
    {
        EntityState.Added => "INSERT",
        EntityState.Modified => "UPDATE",
        EntityState.Deleted => "DELETE",
        _ => "UNKNOWN"
    };

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var key = entry.Properties
            .FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return key?.CurrentValue?.ToString() ?? "";
    }

    private static Dictionary<string, object?> GetValues(PropertyValues values)
    {
        return values.Properties
            .ToDictionary(p => p.Name, p => values[p]);
    }

    private static List<string> GetChangedFields(EntityEntry entry)
    {
        if (entry.State != EntityState.Modified)
            return new List<string>();

        return entry.Properties
            .Where(p => p.IsModified)
            .Select(p => p.Metadata.Name)
            .ToList();
    }
}
```

### 3.6 RabbitMQ Publisher

```csharp
namespace MsTransacoes.Infra.Messaging;

public class RabbitMQPublisher : IAuditEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQPublisher> _logger;
    
    private const string ExchangeName = "audit-events";
    private const string RoutingKey = "audit";
    private const string ErrorRoutingKey = "audit.error";

    public RabbitMQPublisher(IConfiguration config, ILogger<RabbitMQPublisher> logger)
    {
        _logger = logger;
        
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:User"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declarar exchange e filas
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);
        
        _channel.QueueDeclare("audit-queue", durable: true, exclusive: false, 
            autoDelete: false, arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", ExchangeName },
                { "x-dead-letter-routing-key", ErrorRoutingKey }
            });
        _channel.QueueBind("audit-queue", ExchangeName, RoutingKey);
        
        _channel.QueueDeclare("audit-error-queue", durable: true, 
            exclusive: false, autoDelete: false);
        _channel.QueueBind("audit-error-queue", ExchangeName, ErrorRoutingKey);
    }

    public Task PublishAsync(AuditEventDTO auditEvent)
    {
        try
        {
            var message = JsonSerializer.SerializeToUtf8Bytes(auditEvent);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            _channel.BasicPublish(ExchangeName, RoutingKey, properties, message);
            _logger.LogInformation("Evento de auditoria publicado: {EventId}", 
                auditEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar evento, enviando para fila de erro");
            SendToErrorQueue(auditEvent);
        }
        
        return Task.CompletedTask;
    }

    public async Task PublishBatchAsync(IEnumerable<AuditEventDTO> events)
    {
        foreach (var evt in events)
        {
            await PublishAsync(evt);
        }
    }

    private void SendToErrorQueue(AuditEventDTO auditEvent)
    {
        try
        {
            var message = JsonSerializer.SerializeToUtf8Bytes(auditEvent);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(ExchangeName, ErrorRoutingKey, properties, message);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Falha crítica ao enviar para fila de erro");
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
```

### 3.7 Cliente HTTP para MS-Contas

```csharp
namespace MsTransacoes.Infra.ExternalServices;

public class ContasApiClient : IContasApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly ILogger<ContasApiClient> _logger;

    public ContasApiClient(
        HttpClient httpClient, 
        ICorrelationIdAccessor correlationId,
        ILogger<ContasApiClient> logger)
    {
        _httpClient = httpClient;
        _correlationId = correlationId;
        _logger = logger;
    }

    public async Task<ContaDTO?> GetContaAsync(string contaId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, 
            $"/api/v1/contas/{contaId}");
        AddCorrelationHeader(request);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<ContaDTO>();
    }

    public async Task<decimal> GetSaldoAsync(string contaId)
    {
        var conta = await GetContaAsync(contaId);
        return conta?.Saldo ?? 0;
    }

    public async Task AtualizarSaldoAsync(string contaId, decimal novoSaldo)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, 
            $"/api/v1/contas/{contaId}/saldo")
        {
            Content = JsonContent.Create(new { saldo = novoSaldo })
        };
        AddCorrelationHeader(request);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        _logger.LogInformation("Saldo atualizado para conta {ContaId}: {NovoSaldo}", 
            contaId, novoSaldo);
    }

    private void AddCorrelationHeader(HttpRequestMessage request)
    {
        var correlationId = _correlationId.GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.Add("X-Correlation-Id", correlationId);
        }
    }
}
```

### 3.9 Serviço de Transações

```csharp
namespace MsTransacoes.Application.Services;

public class TransacaoService : ITransacaoService
{
    private readonly TransacoesDbContext _context;
    private readonly IContasApiClient _contasApi;
    private readonly ILogger<TransacaoService> _logger;

    public async Task<TransacaoDTO> RealizarDepositoAsync(DepositoRequest request)
    {
        // Buscar conta
        var conta = await _contasApi.GetContaAsync(request.ContaId.ToString())
            ?? throw new NotFoundException("Conta não encontrada");

        // Calcular novo saldo
        var novoSaldo = conta.Saldo + request.Valor;

        // Criar transação
        var transacao = new Transacao
        {
            ContaOrigemId = request.ContaId,
            Tipo = TipoTransacao.DEPOSITO,
            Valor = request.Valor,
            Descricao = request.Descricao,
            Status = StatusTransacao.CONCLUIDA,
            ProcessadoEm = DateTime.UtcNow
        };

        _context.Transacoes.Add(transacao);
        await _context.SaveChangesAsync();

        // Atualizar saldo via API (gera auditoria no MS-Contas)
        await _contasApi.AtualizarSaldoAsync(request.ContaId.ToString(), novoSaldo);

        return transacao.ToDTO();
    }

    public async Task<TransacaoDTO> RealizarSaqueAsync(SaqueRequest request)
    {
        var conta = await _contasApi.GetContaAsync(request.ContaId.ToString())
            ?? throw new NotFoundException("Conta não encontrada");

        if (conta.Saldo < request.Valor)
            throw new BusinessException("Saldo insuficiente");

        var novoSaldo = conta.Saldo - request.Valor;

        var transacao = new Transacao
        {
            ContaOrigemId = request.ContaId,
            Tipo = TipoTransacao.SAQUE,
            Valor = request.Valor,
            Descricao = request.Descricao,
            Status = StatusTransacao.CONCLUIDA,
            ProcessadoEm = DateTime.UtcNow
        };

        _context.Transacoes.Add(transacao);
        await _context.SaveChangesAsync();

        await _contasApi.AtualizarSaldoAsync(request.ContaId.ToString(), novoSaldo);

        return transacao.ToDTO();
    }

    public async Task<TransacaoDTO> RealizarTransferenciaAsync(
        TransferenciaRequest request)
    {
        var contaOrigem = await _contasApi.GetContaAsync(
            request.ContaOrigemId.ToString())
            ?? throw new NotFoundException("Conta origem não encontrada");
        
        var contaDestino = await _contasApi.GetContaAsync(
            request.ContaDestinoId.ToString())
            ?? throw new NotFoundException("Conta destino não encontrada");

        if (contaOrigem.Saldo < request.Valor)
            throw new BusinessException("Saldo insuficiente");

        var transacao = new Transacao
        {
            ContaOrigemId = request.ContaOrigemId,
            ContaDestinoId = request.ContaDestinoId,
            Tipo = TipoTransacao.TRANSFERENCIA,
            Valor = request.Valor,
            Descricao = request.Descricao,
            Status = StatusTransacao.CONCLUIDA,
            ProcessadoEm = DateTime.UtcNow
        };

        _context.Transacoes.Add(transacao);
        await _context.SaveChangesAsync();

        // Débito e crédito via API
        await _contasApi.AtualizarSaldoAsync(
            request.ContaOrigemId.ToString(), 
            contaOrigem.Saldo - request.Valor);
        
        await _contasApi.AtualizarSaldoAsync(
            request.ContaDestinoId.ToString(), 
            contaDestino.Saldo + request.Valor);

        return transacao.ToDTO();
    }
}
```

### 3.12 Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MsTransacoes.API/MsTransacoes.API.csproj", "MsTransacoes.API/"]
COPY ["src/MsTransacoes.Application/MsTransacoes.Application.csproj", "MsTransacoes.Application/"]
COPY ["src/MsTransacoes.Domain/MsTransacoes.Domain.csproj", "MsTransacoes.Domain/"]
COPY ["src/MsTransacoes.Infra/MsTransacoes.Infra.csproj", "MsTransacoes.Infra/"]
RUN dotnet restore "MsTransacoes.API/MsTransacoes.API.csproj"
COPY src/ .
RUN dotnet publish "MsTransacoes.API/MsTransacoes.API.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "MsTransacoes.API.dll"]
```

## Endpoints a Implementar

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/v1/transacoes/deposito` | Realizar depósito |
| `POST` | `/api/v1/transacoes/saque` | Realizar saque |
| `POST` | `/api/v1/transacoes/transferencia` | Realizar transferência |
| `GET` | `/api/v1/transacoes/conta/{contaId}` | Listar transações |
| `GET` | `/api/v1/transacoes/{id}` | Buscar transação |

## Critérios de Sucesso

- [ ] Endpoints REST funcionando corretamente
- [ ] Swagger UI acessível em `http://localhost:5000/swagger`
- [ ] Autenticação Basic Auth funcionando
- [ ] EF Core Interceptor capturando INSERT/UPDATE/DELETE
- [ ] Eventos de auditoria publicados no RabbitMQ
- [ ] Integração REST com MS-Contas funcionando
- [ ] Correlation ID sendo propagado nas chamadas
- [ ] Validações de saldo funcionando

## Estimativa

**Tempo:** 2 dias (16 horas)

---

**Referências:**
- Tech Spec: Seção "MS-Transações (.NET)"
- PRD: RF-09 a RF-17
- Rules: `dotnet-architecture.md`, `dotnet-folders.md`
