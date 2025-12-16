# Tech Spec - Sistema de Auditoria de Transações Bancárias (POC)

## Resumo Executivo

Esta especificação técnica detalha a implementação de uma arquitetura de auditoria transparente usando interceptors na camada de aplicação. A solução utiliza **Hibernate Event Listeners** (Java) e **EF Core SaveChangesInterceptor** (.NET) para capturar automaticamente operações de INSERT, UPDATE e DELETE, publicando eventos no RabbitMQ para processamento assíncrono e armazenamento no Elasticsearch.

A arquitetura prioriza simplicidade e demonstrabilidade (POC), utilizando um único PostgreSQL com schemas separados, comunicação REST entre serviços, e bibliotecas de baixo nível (RabbitMQ.Client) para máximo controle e aprendizado.

---

## Arquitetura do Sistema

### Visão Geral dos Componentes

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND (React + Tailwind)                     │
│                                   :3000                                      │
└─────────────────────────────────────┬───────────────────────────────────────┘
                                      │ REST API
                    ┌─────────────────┴─────────────────┐
                    ▼                                   ▼
┌───────────────────────────────┐     ┌───────────────────────────────┐
│   MS-CONTAS (Java/Spring)     │     │   MS-TRANSACOES (.NET 8)      │
│          :8080                │     │          :5000                │
│                               │     │                               │
│  ┌─────────────────────────┐  │     │  ┌─────────────────────────┐  │
│  │ Hibernate Event Listener│  │     │  │ EF Core Interceptor     │  │
│  └───────────┬─────────────┘  │     │  └───────────┬─────────────┘  │
└──────────────┼────────────────┘     └──────────────┼────────────────┘
               │                                      │
               │         ┌────────────────┐           │
               └────────►│   PostgreSQL   │◄──────────┘
                         │     :5432      │
                         │                │
                         │ ┌────────────┐ │
                         │ │schema:contas│ │
                         │ │schema:trans │ │
                         │ └────────────┘ │
                         └────────────────┘
               │                                      │
               │         ┌────────────────┐           │
               └────────►│   RabbitMQ     │◄──────────┘
                         │     :5672      │
                         │   :15672 (UI)  │
                         └───────┬────────┘
                                 │
                         ┌───────▼────────┐
                         │  MS-AUDITORIA  │
                         │   (.NET 8)     │
                         │     :5001      │
                         └───────┬────────┘
                                 │
                         ┌───────▼────────┐
                         │ Elasticsearch  │
                         │     :9200      │
                         └────────────────┘
```

### Responsabilidades dos Componentes

| Componente | Responsabilidade | Tecnologia |
|------------|------------------|------------|
| MS-Contas | CRUD usuários e contas, auditoria via Hibernate | Java 21, Spring Boot 3.2 |
| MS-Transações | Operações financeiras, auditoria via EF Core | .NET 8, EF Core 8 |
| MS-Auditoria | Consumir fila, persistir e consultar auditoria | .NET 8, Elastic.Clients |
| Frontend | Interface web SPA | React 18, Tailwind CSS |
| PostgreSQL | Persistência relacional | PostgreSQL 16 |
| RabbitMQ | Mensageria assíncrona | RabbitMQ 3.12 |
| Elasticsearch | Indexação e busca de auditoria | Elasticsearch 8.11 |

---

## Design de Implementação

### Estrutura de Projetos

#### MS-Contas (Java)
```
ms-contas/
├── pom.xml
├── Dockerfile
└── src/main/java/com/pocauditoria/contas/
    ├── MsContasApplication.java
    ├── domain/
    │   ├── entity/
    │   │   ├── Usuario.java
    │   │   └── Conta.java
    │   └── repository/
    │       ├── UsuarioRepository.java
    │       └── ContaRepository.java
    ├── application/
    │   ├── dto/
    │   │   ├── UsuarioDTO.java
    │   │   ├── ContaDTO.java
    │   │   └── AuditEventDTO.java
    │   └── service/
    │       ├── UsuarioService.java
    │       └── ContaService.java
    ├── api/
    │   ├── controller/
    │   │   ├── UsuarioController.java
    │   │   └── ContaController.java
    │   └── config/
    │       ├── SecurityConfig.java
    │       └── SwaggerConfig.java
    └── infra/
        ├── audit/
        │   ├── AuditEventListener.java
        │   └── AuditEventPublisher.java
        ├── messaging/
        │   └── RabbitMQConfig.java
        └── persistence/
            └── JpaConfig.java
```

#### MS-Transações (.NET 8)
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
    │       └── SimpleAuthMiddleware.cs
    ├── MsTransacoes.Application/
    │   ├── MsTransacoes.Application.csproj
    │   ├── DTOs/
    │   │   ├── TransacaoDTO.cs
    │   │   └── AuditEventDTO.cs
    │   └── Services/
    │       └── TransacaoService.cs
    ├── MsTransacoes.Domain/
    │   ├── MsTransacoes.Domain.csproj
    │   └── Entities/
    │       └── Transacao.cs
    └── MsTransacoes.Infra/
        ├── MsTransacoes.Infra.csproj
        ├── Persistence/
        │   ├── TransacoesDbContext.cs
        │   └── Repositories/
        ├── Audit/
        │   ├── AuditInterceptor.cs
        │   └── AuditEventPublisher.cs
        └── Messaging/
            └── RabbitMQPublisher.cs
```

#### MS-Auditoria (.NET 8)
```
ms-auditoria/
├── MsAuditoria.sln
├── Dockerfile
└── src/
    ├── MsAuditoria.API/
    │   ├── Program.cs
    │   └── Controllers/
    │       └── AuditController.cs
    ├── MsAuditoria.Application/
    │   ├── DTOs/
    │   │   └── AuditEventDTO.cs
    │   └── Services/
    │       └── AuditService.cs
    └── MsAuditoria.Infra/
        ├── Elasticsearch/
        │   └── ElasticsearchService.cs
        └── Messaging/
            └── RabbitMQConsumer.cs
```

#### Frontend (React)
```
frontend/
├── package.json
├── Dockerfile
├── tailwind.config.js
├── vite.config.ts
└── src/
    ├── main.tsx
    ├── App.tsx
    ├── components/
    │   ├── Layout/
    │   ├── Auth/
    │   └── Audit/
    │       ├── AuditList.tsx
    │       └── AuditDiff.tsx
    ├── pages/
    │   ├── Login.tsx
    │   ├── Dashboard.tsx
    │   ├── Usuarios.tsx
    │   ├── Contas.tsx
    │   ├── Transacoes.tsx
    │   └── Auditoria.tsx
    ├── services/
    │   ├── api.ts
    │   ├── contasApi.ts
    │   ├── transacoesApi.ts
    │   └── auditoriaApi.ts
    └── contexts/
        └── AuthContext.tsx
```

---

### Interfaces Principais

#### Java - Audit Event Listener

```java
@Component
public class AuditEventListener implements 
        PreInsertEventListener, 
        PreUpdateEventListener, 
        PreDeleteEventListener {

    private final AuditEventPublisher publisher;
    private final UserContextHolder userContext;

    @Override
    public boolean onPreInsert(PreInsertEvent event) {
        publishAuditEvent("INSERT", event.getEntity(), null, event.getState(), 
                          event.getPersister().getPropertyNames());
        return false;
    }

    @Override
    public boolean onPreUpdate(PreUpdateEvent event) {
        publishAuditEvent("UPDATE", event.getEntity(), event.getOldState(), 
                          event.getState(), event.getPersister().getPropertyNames());
        return false;
    }

    @Override
    public boolean onPreDelete(PreDeleteEvent event) {
        publishAuditEvent("DELETE", event.getEntity(), event.getDeletedState(), 
                          null, event.getPersister().getPropertyNames());
        return false;
    }

    private void publishAuditEvent(String operation, Object entity, 
            Object[] oldState, Object[] newState, String[] propertyNames) {
        var auditEvent = AuditEventDTO.builder()
            .id(UUID.randomUUID().toString())
            .timestamp(Instant.now())
            .operation(operation)
            .entityName(entity.getClass().getSimpleName())
            .entityId(extractEntityId(entity))
            .userId(userContext.getCurrentUserId())
            .oldValues(buildValuesMap(oldState, propertyNames))
            .newValues(buildValuesMap(newState, propertyNames))
            .sourceService("ms-contas")
            .build();
        
        publisher.publishAsync(auditEvent);
    }
}
```

#### .NET - EF Core SaveChangesInterceptor

```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IAuditEventPublisher _publisher;
    private readonly IUserContextAccessor _userContext;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context!;
        var auditEntries = new List<AuditEventDTO>();

        foreach (var entry in context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var auditEvent = new AuditEventDTO
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Operation = entry.State.ToString().ToUpper(),
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetPrimaryKeyValue(entry),
                UserId = _userContext.GetCurrentUserId(),
                OldValues = entry.State != EntityState.Added 
                    ? GetValues(entry.OriginalValues) : null,
                NewValues = entry.State != EntityState.Deleted 
                    ? GetValues(entry.CurrentValues) : null,
                SourceService = "ms-transacoes"
            };
            
            auditEntries.Add(auditEvent);
        }

        // Fire-and-forget para não bloquear a operação principal
        _ = Task.Run(() => _publisher.PublishBatchAsync(auditEntries), cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private Dictionary<string, object?> GetValues(PropertyValues values)
    {
        return values.Properties
            .ToDictionary(p => p.Name, p => values[p]);
    }
}
```

#### .NET - RabbitMQ Publisher

```csharp
public class RabbitMQPublisher : IAuditEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "audit-events";
    private const string RoutingKey = "audit";

    public RabbitMQPublisher(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"],
            UserName = config["RabbitMQ:User"],
            Password = config["RabbitMQ:Password"]
        };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);
    }

    public Task PublishAsync(AuditEventDTO auditEvent)
    {
        var message = JsonSerializer.SerializeToUtf8Bytes(auditEvent);
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel.BasicPublish(ExchangeName, RoutingKey, properties, message);
        return Task.CompletedTask;
    }
}
```

---

### Modelos de Dados

#### PostgreSQL - Schema `contas`

```sql
-- Schema para MS-Contas
CREATE SCHEMA IF NOT EXISTS contas;

CREATE TABLE contas.usuarios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    senha_hash VARCHAR(255) NOT NULL,
    ativo BOOLEAN DEFAULT TRUE,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE contas.contas_bancarias (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    numero_conta VARCHAR(20) NOT NULL UNIQUE,
    usuario_id UUID NOT NULL REFERENCES contas.usuarios(id),
    saldo DECIMAL(18,2) DEFAULT 0.00,
    tipo VARCHAR(20) NOT NULL, -- CORRENTE, POUPANCA
    ativa BOOLEAN DEFAULT TRUE,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_contas_usuario ON contas.contas_bancarias(usuario_id);
```

#### PostgreSQL - Schema `transacoes`

```sql
-- Schema para MS-Transacoes
CREATE SCHEMA IF NOT EXISTS transacoes;

CREATE TABLE transacoes.transacoes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_origem_id UUID NOT NULL,
    conta_destino_id UUID,
    tipo VARCHAR(20) NOT NULL, -- DEPOSITO, SAQUE, TRANSFERENCIA
    valor DECIMAL(18,2) NOT NULL,
    descricao VARCHAR(255),
    status VARCHAR(20) DEFAULT 'PENDENTE', -- PENDENTE, CONCLUIDA, CANCELADA
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processado_em TIMESTAMP
);

CREATE INDEX idx_transacoes_conta_origem ON transacoes.transacoes(conta_origem_id);
CREATE INDEX idx_transacoes_data ON transacoes.transacoes(criado_em);
```

#### Elasticsearch - Índices de Auditoria

```json
// Índice: audit-ms-contas
// Índice: audit-ms-transacoes
{
  "mappings": {
    "properties": {
      "id": { "type": "keyword" },
      "timestamp": { "type": "date" },
      "operation": { "type": "keyword" },
      "entityName": { "type": "keyword" },
      "entityId": { "type": "keyword" },
      "userId": { "type": "keyword" },
      "oldValues": { "type": "object", "enabled": true },
      "newValues": { "type": "object", "enabled": true },
      "changedFields": { "type": "keyword" },
      "sourceService": { "type": "keyword" },
      "correlationId": { "type": "keyword" }
    }
  },
  "settings": {
    "number_of_shards": 1,
    "number_of_replicas": 0
  }
}
```

#### DTO de Evento de Auditoria

```csharp
public record AuditEventDTO
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Operation { get; init; } = string.Empty; // INSERT, UPDATE, DELETE
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public Dictionary<string, object?>? OldValues { get; init; }
    public Dictionary<string, object?>? NewValues { get; init; }
    public List<string>? ChangedFields { get; init; }
    public string SourceService { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString(); // Gerado a cada requisição
}
```

#### Correlation ID

O `CorrelationId` é gerado automaticamente a cada requisição HTTP via middleware e propagado para todos os eventos de auditoria da mesma requisição:

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor accessor)
    {
        var correlationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        accessor.SetCorrelationId(correlationId);
        
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        await _next(context);
    }
}
```

---

### Endpoints de API

#### MS-Contas (Java) - `:8080`

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/v1/usuarios` | Criar usuário |
| `GET` | `/api/v1/usuarios` | Listar usuários |
| `GET` | `/api/v1/usuarios/{id}` | Buscar usuário por ID |
| `PUT` | `/api/v1/usuarios/{id}` | Atualizar usuário |
| `DELETE` | `/api/v1/usuarios/{id}` | Excluir usuário |
| `POST` | `/api/v1/contas` | Criar conta bancária |
| `GET` | `/api/v1/contas` | Listar contas |
| `GET` | `/api/v1/contas/{id}` | Buscar conta por ID |
| `GET` | `/api/v1/contas/usuario/{usuarioId}` | Listar contas do usuário |
| `PUT` | `/api/v1/contas/{id}` | Atualizar conta |
| `DELETE` | `/api/v1/contas/{id}` | Excluir conta |

#### MS-Transações (.NET) - `:5000`

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/v1/transacoes/deposito` | Realizar depósito |
| `POST` | `/api/v1/transacoes/saque` | Realizar saque |
| `POST` | `/api/v1/transacoes/transferencia` | Realizar transferência |
| `GET` | `/api/v1/transacoes/conta/{contaId}` | Listar transações da conta |
| `GET` | `/api/v1/transacoes/{id}` | Buscar transação por ID |
| `GET` | `/api/v1/contas/{id}/saldo` | Consultar saldo (proxy para MS-Contas) |

#### MS-Auditoria (.NET) - `:5001`

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GET` | `/api/v1/audit` | Listar eventos de auditoria |
| `GET` | `/api/v1/audit/{id}` | Buscar evento por ID |
| `GET` | `/api/v1/audit/entity/{entityName}/{entityId}` | Histórico de uma entidade |
| `GET` | `/api/v1/audit/user/{userId}` | Eventos por usuário |

**Parâmetros de Query para listagem:**
- `startDate`, `endDate` - Filtro por período
- `operation` - Filtro por operação (INSERT, UPDATE, DELETE)
- `entityName` - Filtro por entidade
- `sourceService` - Filtro por serviço origem
- `correlationId` - Filtro por correlation ID

> **Nota:** Não há paginação. Todas as consultas retornam todos os resultados (adequado para POC com volume baixo de dados).

---

## Pontos de Integração

### MS-Transações → MS-Contas (REST)

O MS-Transações consulta e atualiza saldos de contas **exclusivamente via API REST** do MS-Contas (não acessa banco diretamente):

```csharp
public class ContasApiClient : IContasApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ICorrelationIdAccessor _correlationId;

    public async Task<ContaDTO?> GetContaAsync(string contaId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/contas/{contaId}");
        request.Headers.Add("X-Correlation-Id", _correlationId.GetCorrelationId());
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContaDTO>();
    }

    public async Task AtualizarSaldoAsync(string contaId, decimal novoSaldo)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/contas/{contaId}/saldo")
        {
            Content = JsonContent.Create(new { saldo = novoSaldo })
        };
        request.Headers.Add("X-Correlation-Id", _correlationId.GetCorrelationId());
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
```

**Importante:** A atualização de saldo via API garante que o Hibernate Event Listener do MS-Contas capture a alteração e gere o evento de auditoria correspondente.
```

### Publicação RabbitMQ

**Exchange:** `audit-events` (Direct)  
**Queues:**
- `audit-queue` - Fila principal de eventos
- `audit-error-queue` - Fila de erros (DLQ)

**Routing Keys:**
- `audit` - Eventos normais
- `audit.error` - Eventos com falha

```yaml
# Configuração do RabbitMQ
Exchange: audit-events
  Type: direct
  Durable: true

Queue: audit-queue
  Durable: true
  Binding: audit-events -> audit
  Arguments:
    x-dead-letter-exchange: audit-events
    x-dead-letter-routing-key: audit.error

Queue: audit-error-queue
  Durable: true
  Binding: audit-events -> audit.error
```

**Estratégia de Erro:** Em caso de falha na publicação, o evento é enviado diretamente para `audit-error-queue` sem retry automático.

### Autenticação Simples (Middleware)

```csharp
public class SimpleAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<string, string> _validCredentials = new()
    {
        { "admin", "admin123" },
        { "user", "user123" }
    };

    public async Task InvokeAsync(HttpContext context)
    {
        // Bypass para Swagger e health
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
        {
            context.Response.StatusCode = 401;
            return;
        }

        var credentials = Encoding.UTF8.GetString(
            Convert.FromBase64String(authHeader["Basic ".Length..]));
        var parts = credentials.Split(':');

        if (parts.Length != 2 || 
            !_validCredentials.TryGetValue(parts[0], out var pwd) || 
            pwd != parts[1])
        {
            context.Response.StatusCode = 401;
            return;
        }

        context.Items["UserId"] = parts[0];
        await _next(context);
    }
}
```

---

## Análise de Impacto

| Componente Afetado | Tipo de Impacto | Descrição & Nível de Risco | Ação Requerida |
|--------------------|-----------------|----------------------------|----------------|
| PostgreSQL | Novo Schema | Criação de 2 schemas (contas, transacoes). Baixo risco. | Script de inicialização |
| RabbitMQ | Nova Exchange/Queue | Criação de exchange e fila para auditoria. Baixo risco. | Configuração automática |
| Elasticsearch | Novos Índices | 2 índices de auditoria. Baixo risco. | Mapeamento inicial |
| Rede Docker | Nova rede | Comunicação entre containers. Baixo risco. | docker-compose |

---

## Abordagem de Testes

> **Nota:** Testes automatizados estão fora do escopo desta POC conforme PRD.

### Testes Manuais Recomendados

1. **Fluxo de Auditoria E2E:**
   - Criar usuário → Verificar evento no Elasticsearch
   - Atualizar conta → Verificar diff (old/new values)
   - Realizar transferência → Verificar múltiplos eventos

2. **Validação de Campos:**
   - Verificar `userId` correto em cada evento
   - Verificar `timestamp` preciso
   - Verificar `changedFields` calculado corretamente

3. **Resiliência:**
   - Parar RabbitMQ → Operação de negócio deve continuar
   - Parar Elasticsearch → Consumer deve reter mensagens

---

## Sequenciamento de Desenvolvimento

### Ordem de Construção

```
Fase 1: Infraestrutura (1 dia)
├── 1.1 Docker Compose base (PostgreSQL, RabbitMQ, Elasticsearch)
├── 1.2 Scripts de inicialização de banco
└── 1.3 Configuração de rede

Fase 2: MS-Contas - Java (2 dias)
├── 2.1 Setup projeto Spring Boot
├── 2.2 Entidades e repositórios (Usuario, Conta)
├── 2.3 Controllers REST + Swagger
├── 2.4 Hibernate Event Listeners
└── 2.5 RabbitMQ Publisher

Fase 3: MS-Transações - .NET (2 dias)
├── 3.1 Setup projeto .NET 8
├── 3.2 Entidades e DbContext
├── 3.3 Controllers REST + Swagger
├── 3.4 EF Core Interceptor
├── 3.5 RabbitMQ Publisher
└── 3.6 Integração REST com MS-Contas

Fase 4: MS-Auditoria - .NET (1.5 dias)
├── 4.1 Setup projeto .NET 8
├── 4.2 RabbitMQ Consumer
├── 4.3 Elasticsearch Service
└── 4.4 Controllers REST para consulta

Fase 5: Frontend - React (2 dias)
├── 5.1 Setup Vite + React + Tailwind
├── 5.2 Tela de Login
├── 5.3 CRUD Usuários e Contas
├── 5.4 Tela de Transações
├── 5.5 Tela de Auditoria + Diff
└── 5.6 Dashboard

Fase 6: Integração e Dockerização (1 dia)
├── 6.1 Dockerfiles para cada serviço
├── 6.2 Docker Compose completo
├── 6.3 Testes E2E manuais
└── 6.4 Documentação README
```

**Total estimado:** 9-10 dias

### Dependências Técnicas

| Dependência | Bloqueador para | Resolução |
|-------------|-----------------|-----------|
| PostgreSQL disponível | MS-Contas, MS-Transações | Docker Compose |
| RabbitMQ disponível | Publicação de eventos | Docker Compose |
| Elasticsearch disponível | MS-Auditoria | Docker Compose |
| MS-Contas API | MS-Transações (consulta saldo) | Desenvolver em paralelo com mocks |

---

## Monitoramento e Observabilidade

### Logs (Simplificado para POC)

```csharp
// .NET - Serilog básico
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
```

```java
// Java - Logback padrão
logging.level.com.pocauditoria=DEBUG
logging.pattern.console=%d{HH:mm:ss} [%thread] %-5level %logger{36} - %msg%n
```

### Health Checks

```csharp
// .NET
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddRabbitMQ()
    .AddElasticsearch();
```

```java
// Java - Spring Actuator
management.endpoints.web.exposure.include=health,info
management.health.rabbit.enabled=true
management.health.db.enabled=true
```

### Portas de Acesso

| Serviço | Porta | Acesso |
|---------|-------|--------|
| Frontend | 3000 | http://localhost:3000 |
| MS-Contas | 8080 | http://localhost:8080/swagger-ui.html |
| MS-Transações | 5000 | http://localhost:5000/swagger |
| MS-Auditoria | 5001 | http://localhost:5001/swagger |
| PostgreSQL | 5432 | localhost:5432 |
| RabbitMQ UI | 15672 | http://localhost:15672 (guest/guest) |
| Elasticsearch | 9200 | http://localhost:9200 |

---

## Considerações Técnicas

### Decisões Principais

| Decisão | Escolha | Justificativa |
|---------|---------|---------------|
| Banco de dados | PostgreSQL único com schemas | Simplicidade para POC, fácil setup |
| Comunicação MS-Contas ↔ MS-Transações | REST síncrono | Simplicidade, adequado para POC |
| Client RabbitMQ (.NET) | RabbitMQ.Client | Controle total, sem overhead de abstração |
| Client Elasticsearch | Elastic.Clients.Elasticsearch | Client oficial mais recente e simples |
| Frontend Framework | React + Vite + Tailwind | Stack moderna, rápido setup, sem boilerplate |
| Autenticação | Basic Auth hardcoded | Adequado para POC, fácil implementação |
| Índices Elasticsearch | Separados por serviço | Facilita consultas e organização |

### Riscos Conhecidos

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| Perda de eventos de auditoria | Baixa | Alto | Fila de erro (DLQ) para análise posterior |
| Latência na consulta Elasticsearch | Baixa | Baixo | Índices otimizados, sem paginação (POC) |
| Inconsistência de saldo | Média | Alto | Atualização via API do MS-Contas (auditado) |
| Hibernate Listener não captura | Baixa | Alto | Testes manuais extensivos |

### Alternativas Consideradas e Rejeitadas

| Alternativa | Motivo da Rejeição |
|-------------|-------------------|
| MassTransit para RabbitMQ | Overhead desnecessário para POC |
| Triggers PostgreSQL | Menos flexível, difícil debug |
| MongoDB para auditoria | Elasticsearch melhor para buscas |
| gRPC entre serviços | Complexidade adicional desnecessária |

### Conformidade com Padrões

- ✅ Clean Architecture em ambos microserviços (.NET e Java)
- ✅ Repository Pattern para acesso a dados
- ✅ DTOs para transferência entre camadas
- ✅ Injeção de dependência nativa
- ✅ Swagger/OpenAPI para documentação
- ✅ REST seguindo convenções do `rules/restful.md`
- ⚠️ CQRS não aplicado (simplicidade da POC)
- ⚠️ Testes automatizados fora do escopo

---

## Docker Compose

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_USER: poc_user
      POSTGRES_PASSWORD: poc_password
      POSTGRES_DB: poc_auditoria
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init.sql:/docker-entrypoint-initdb.d/init.sql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U poc_user -d poc_auditoria"]
      interval: 5s
      timeout: 5s
      retries: 5

  rabbitmq:
    image: rabbitmq:3.12-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5

  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
    ports:
      - "9200:9200"
    volumes:
      - elasticsearch_data:/usr/share/elasticsearch/data
    healthcheck:
      test: curl -s http://localhost:9200 >/dev/null || exit 1
      interval: 10s
      timeout: 5s
      retries: 5

  ms-contas:
    build: ./ms-contas
    ports:
      - "8080:8080"
    environment:
      SPRING_DATASOURCE_URL: jdbc:postgresql://postgres:5432/poc_auditoria?currentSchema=contas
      SPRING_DATASOURCE_USERNAME: poc_user
      SPRING_DATASOURCE_PASSWORD: poc_password
      SPRING_RABBITMQ_HOST: rabbitmq
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy

  ms-transacoes:
    build: ./ms-transacoes
    ports:
      - "5000:5000"
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=poc_auditoria;Username=poc_user;Password=poc_password;SearchPath=transacoes"
      RabbitMQ__Host: rabbitmq
      Services__MsContas: http://ms-contas:8080
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
      ms-contas:
        condition: service_started

  ms-auditoria:
    build: ./ms-auditoria
    ports:
      - "5001:5001"
    environment:
      RabbitMQ__Host: rabbitmq
      Elasticsearch__Url: http://elasticsearch:9200
    depends_on:
      rabbitmq:
        condition: service_healthy
      elasticsearch:
        condition: service_healthy

  frontend:
    build: ./frontend
    ports:
      - "3000:3000"
    environment:
      VITE_API_CONTAS: http://localhost:8080
      VITE_API_TRANSACOES: http://localhost:5000
      VITE_API_AUDITORIA: http://localhost:5001
    depends_on:
      - ms-contas
      - ms-transacoes
      - ms-auditoria

volumes:
  postgres_data:
  elasticsearch_data:
```

---

## Decisões Finais

### ✅ Questões Resolvidas

| Questão | Decisão |
|---------|----------|
| Retry de publicação | Sem retry - eventos com falha vão para `audit-error-queue` (DLQ) |
| Correlation ID | Gerado a cada requisição HTTP via middleware |
| Atualização de saldo | Via API REST do MS-Contas (garante auditoria) |
| Paginação | Não implementada (POC com baixo volume) |

---

**Documento criado em**: 16 de Dezembro de 2025  
**Versão**: 1.1  
**Referência PRD**: `tasks/prd-sistema-auditoria-transacoes/prd.md`
