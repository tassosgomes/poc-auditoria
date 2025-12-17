# POC - Sistema de Auditoria de Transações Bancárias

## Visão Geral

Este projeto é uma **Prova de Conceito (POC)** para validar uma **arquitetura de auditoria transparente** em sistemas distribuídos. O sistema captura automaticamente todas as operações de **INSERT**, **UPDATE** e **DELETE** na camada de aplicação (não no banco de dados), registrando **quem**, **quando** e **o quê** foi alterado, incluindo valores anteriores e novos.

O domínio de transações bancárias é usado apenas como cenário de teste. O **verdadeiro valor está na arquitetura de auditoria** que pode ser replicada em qualquer sistema.

### Objetivo Principal

Validar a viabilidade de uma arquitetura de auditoria transparente que:
- **Capture eventos na camada de aplicação** via Hibernate Event Listeners (Java) e EF Core Interceptors (.NET)
- **Não exija alteração no código de negócio** - auditoria completamente transparente
- **Processe eventos de forma assíncrona** via RabbitMQ
- **Armazene e permita consulta eficiente** dos logs no Elasticsearch
- **Mantenha histórico completo** com valores anteriores e novos (diff)

## Arquitetura

### Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────────────┐
│                         FRONTEND (React)                            │
│                  Interface Web + Visualização de Auditoria          │
└────────────────────────────┬────────────────────────────────────────┘
                             │ REST API
              ┌──────────────┴──────────────┐
              ▼                             ▼
┌──────────────────────────┐    ┌──────────────────────────┐
│   MS-Contas (Java 21)    │    │  MS-Transações (.NET 8)  │
│   Spring Boot 3.2        │    │  EF Core 8               │
├──────────────────────────┤    ├──────────────────────────┤
│ 🎯 Hibernate Event       │    │ 🎯 EF Core               │
│    Listeners             │    │    SaveChangesInterceptor│
│  • PreInsertListener     │    │  • ChangeTracker         │
│  • PreUpdateListener     │    │  • Original/Current      │
│  • PreDeleteListener     │    │    Values                │
└──────────┬───────────────┘    └──────────┬───────────────┘
           │                               │
           │ Publica Eventos               │ Publica Eventos
           ▼                               ▼
         ┌─────────────────────────────────────┐
         │         RabbitMQ (3.12)             │
         │  Exchange: audit-events             │
         │  Queue: audit-queue                 │
         │  DLQ: audit-error-queue             │
         └──────────────┬──────────────────────┘
                        │ Consome Eventos
                        ▼
         ┌─────────────────────────────────────┐
         │    MS-Auditoria (.NET 8)            │
         │  • RabbitMQ Consumer                │
         │  • Elastic.Clients.Elasticsearch    │
         └──────────────┬──────────────────────┘
                        │ Indexa
                        ▼
         ┌─────────────────────────────────────┐
         │      Elasticsearch (8.11)           │
         │  Índices:                           │
         │  • audit-ms-contas                  │
         │  • audit-ms-transacoes              │
         └─────────────────────────────────────┘

         ┌─────────────────────────────────────┐
         │      PostgreSQL (16)                │
         │  Schemas:                           │
         │  • contas (usuarios, contas)        │
         │  • transacoes (transacoes)          │
         └─────────────────────────────────────┘
```

### 🎯 Mecanismo de Captura de Auditoria

A auditoria é capturada **na camada de aplicação**, não no banco de dados:

| Tecnologia | Mecanismo | Como Funciona |
|------------|-----------|---------------|
| **Java/Spring** | Hibernate Event Listeners | Intercepta operações antes do commit: `PreInsertEventListener`, `PreUpdateEventListener`, `PreDeleteEventListener` |
| **.NET/EF Core** | SaveChangesInterceptor | Intercepta `SaveChangesAsync()` e usa `ChangeTracker` para capturar valores originais e atuais |

**Vantagens desta abordagem:**
- ✅ Transparente: sem alteração no código de negócio
- ✅ Portátil: não depende de features específicas do banco
- ✅ Contexto completo: acesso ao usuário logado e contexto da aplicação
- ✅ Flexível: pode enriquecer eventos com dados adicionais

## Tecnologias

| Componente | Tecnologia | Versão | Bibliotecas Principais |
|------------|------------|--------|------------------------|
| MS-Contas | Java, Spring Boot, Hibernate | Java 21, Spring Boot 3.2 | Spring Data JPA, Spring AMQP, PostgreSQL Driver |
| MS-Transações | .NET, EF Core | .NET 8, EF Core 8 | Npgsql.EntityFrameworkCore, RabbitMQ.Client |
| MS-Auditoria | .NET, Elasticsearch | .NET 8 | Elastic.Clients.Elasticsearch 8.11, RabbitMQ.Client |
| Frontend | React, Vite, Tailwind CSS | React 18, Vite 5 | React Router, Axios |
| Banco de Dados | PostgreSQL | PostgreSQL 16 | Schemas separados: `contas`, `transacoes` |
| Mensageria | RabbitMQ | RabbitMQ 3.12 | Exchange: `audit-events`, Queue: `audit-queue` |
| Busca | Elasticsearch | Elasticsearch 8.11 | Índices: `audit-ms-contas`, `audit-ms-transacoes` |

## Pré-requisitos

### Obrigatório
- Docker 24+
- Docker Compose 2.20+

### Opcional (para desenvolvimento local)
- Java 21
- .NET 8 SDK
- Node.js 20+
- Maven 3.9+

## Como Executar

### Usando Docker Compose (Recomendado)

```bash
# 1. Clone o repositório
git clone <repo-url>
cd poc-auditoria

# 2. (Opcional) Configure variáveis de ambiente
cp .env.example .env

# 3. Execute o script de build (opcional, mas recomendado)
./build.sh

# 4. Inicie todos os serviços
docker-compose up -d

# 5. Acompanhe os logs
docker-compose logs -f

# 6. Aguarde todos os serviços ficarem healthy (~1-2 minutos)
docker-compose ps
```

### Desenvolvimento Local

```bash
# 1. Inicie apenas a infraestrutura
docker-compose up -d postgres rabbitmq elasticsearch

# 2. MS-Contas (terminal 1)
cd ms-contas
./mvnw spring-boot:run

# 3. MS-Transações (terminal 2)
cd ms-transacoes/src/MsTransacoes.API
dotnet run

# 4. MS-Auditoria (terminal 3)
cd ms-auditoria/src/MsAuditoria.API
dotnet run

# 5. Frontend (terminal 4)
cd frontend
npm install
npm run dev
```

## URLs de Acesso

| Serviço | URL | Descrição |
|---------|-----|-----------|
| Frontend | http://localhost:3000 | Interface web principal |
| MS-Contas API | http://localhost:8080/api/v1 | API de usuários e contas |
| MS-Contas Swagger | http://localhost:8080/swagger-ui.html | Documentação interativa |
| MS-Transações API | http://localhost:5000/api/v1 | API de transações |
| MS-Transações Health | http://localhost:5000/health | Health check |
| MS-Auditoria API | http://localhost:5001/api/v1 | API de consulta de auditoria |
| MS-Auditoria Health | http://localhost:5001/health | Health check |
| RabbitMQ Management | http://localhost:15672 | Console de gerenciamento |
| Elasticsearch | http://localhost:9200 | API do Elasticsearch |

## Credenciais

### Aplicação (Frontend/APIs)
| Usuário | Senha | Perfil |
|---------|-------|--------|
| admin | admin123 | Administrador |
| user | user123 | Usuário |

### Infraestrutura
| Serviço | Usuário | Senha |
|---------|---------|-------|
| RabbitMQ | guest | guest |
| PostgreSQL | postgres | postgres123 |
| Elasticsearch | - | (sem autenticação) |

## Testando o Fluxo de Auditoria

### 🎯 Teste Completo do Fluxo E2E

Este teste valida toda a cadeia: **Operação → Interceptor → RabbitMQ → Consumer → Elasticsearch → API → Frontend**

#### Passo a Passo:

1. **Acesse a interface web**
   ```
   http://localhost:3000
   ```

2. **Faça login**
   - Usuário: `admin`
   - Senha: `admin123`

3. **Crie um usuário** (Menu: Usuários → Novo)
   - Nome: "João Silva"
   - CPF: "12345678901"
   - Email: "joao@test.com"
   - ✅ **Evento capturado**: `INSERT` em `Usuario` pelo Hibernate Listener

4. **Crie uma conta bancária** (Menu: Contas → Nova)
   - Selecione o usuário criado
   - Tipo: "CORRENTE"
   - ✅ **Evento capturado**: `INSERT` em `Conta` pelo Hibernate Listener

5. **Realize um depósito** (Menu: Transações → Depósito)
   - Selecione a conta
   - Valor: R$ 500,00
   - ✅ **Eventos capturados**:
     - `INSERT` em `Transacao` (EF Core Interceptor)
     - `UPDATE` em `Conta` (saldo alterado - Hibernate Listener via API)

6. **Visualize a auditoria** (Menu: Auditoria)
   - Veja todos os 4 eventos capturados
   - Clique em um evento para ver o **diff detalhado**:
     - Campos alterados em destaque
     - Valores anteriores (old) vs novos (new)
     - Timestamp, usuário, serviço de origem

### 🔍 O Que Você Deve Observar

- ✅ **Transparência**: Nenhuma linha de código de auditoria no controller/service
- ✅ **Completude**: 100% das operações capturadas automaticamente
- ✅ **Rastreabilidade**: Cada evento tem usuário, timestamp e valores
- ✅ **Assíncrono**: Operação não bloqueia enquanto auditoria processa
- ✅ **Diff**: Visualização clara do que mudou

### Teste via API

```bash
# 1. Login e obter credenciais
# (No frontend, as credenciais são enviadas via Basic Auth)

# 2. Criar usuário
curl -X POST http://localhost:8080/api/v1/usuarios \
  -u admin:admin123 \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Maria Silva",
    "cpf": "98765432100",
    "email": "maria@test.com"
  }'

# 3. Listar eventos de auditoria
curl -u admin:admin123 http://localhost:5001/api/v1/audit

# 4. Buscar por entidade específica
curl -u admin:admin123 "http://localhost:5001/api/v1/audit?entityName=Usuario"

# 5. Ver detalhes de um evento
curl -u admin:admin123 http://localhost:5001/api/v1/audit/{id}
```

### Verificando Elasticsearch Diretamente

```bash
# Ver índices criados
curl http://localhost:9200/_cat/indices?v

# Buscar eventos de auditoria do MS-Contas
curl http://localhost:9200/audit-ms-contas/_search?pretty

# Buscar eventos de auditoria do MS-Transações
curl http://localhost:9200/audit-ms-transacoes/_search?pretty

# Contar total de eventos
curl http://localhost:9200/audit-*/_count

# Buscar eventos de um usuário específico
curl "http://localhost:9200/audit-*/_search?q=userId:admin&pretty"

# Buscar eventos de UPDATE
curl "http://localhost:9200/audit-*/_search?q=operation:UPDATE&pretty"
```

### 📋 Estrutura do Evento de Auditoria

Cada evento capturado possui a seguinte estrutura:

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-12-17T10:30:00.000Z",
  "operation": "UPDATE",
  "entityName": "Conta",
  "entityId": "123e4567-e89b-12d3-a456-426614174000",
  "userId": "admin",
  "oldValues": {
    "saldo": 1000.00,
    "atualizadoEm": "2025-12-17T10:00:00Z"
  },
  "newValues": {
    "saldo": 1500.00,
    "atualizadoEm": "2025-12-17T10:30:00Z"
  },
  "changedFields": ["saldo", "atualizadoEm"],
  "sourceService": "ms-transacoes",
  "correlationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Campos importantes:**
- `operation`: INSERT, UPDATE ou DELETE
- `entityName`: Nome da entidade/tabela afetada
- `userId`: Usuário que realizou a operação
- `oldValues`/`newValues`: Valores antes e depois (diff)
- `changedFields`: Lista de campos que foram alterados
- `sourceService`: Qual microserviço gerou o evento
- `correlationId`: ID para rastrear múltiplos eventos da mesma requisição

## Estrutura do Projeto

```
poc-auditoria/
├── docker-compose.yml          # Orquestração de containers
├── build.sh                    # Script de build completo
├── .env.example                # Template de variáveis de ambiente
├── README.md                   # Este arquivo
│
├── scripts/
│   └── init.sql                # Script de inicialização do PostgreSQL
│
├── ms-contas/                  # Microserviço de Contas (Java/Spring)
│   ├── Dockerfile
│   ├── pom.xml
│   └── src/
│       └── main/java/com/pocauditoria/contas/
│           ├── domain/         # Entidades e repositórios
│           ├── application/    # Serviços e DTOs
│           ├── api/            # Controllers REST
│           └── infrastructure/ # Event Listeners, RabbitMQ
│
├── ms-transacoes/              # Microserviço de Transações (.NET)
│   ├── Dockerfile
│   ├── MsTransacoes.sln
│   └── src/
│       ├── MsTransacoes.API/   # API REST
│       ├── MsTransacoes.Application/ # Serviços e DTOs
│       ├── MsTransacoes.Domain/ # Entidades e regras de negócio
│       └── MsTransacoes.Infra/ # EF Core, Interceptors, RabbitMQ
│
├── ms-auditoria/               # Microserviço de Auditoria (.NET)
│   ├── Dockerfile
│   ├── MsAuditoria.sln
│   └── src/
│       ├── MsAuditoria.API/    # API REST de consulta
│       ├── MsAuditoria.Application/ # Serviços
│       └── MsAuditoria.Infra/  # Elasticsearch, RabbitMQ Consumer
│
├── frontend/                   # Interface Web (React)
│   ├── Dockerfile
│   ├── nginx.conf
│   ├── package.json
│   └── src/
│       ├── components/         # Componentes React
│       ├── pages/              # Páginas da aplicação
│       ├── services/           # Clientes de API
│       └── contexts/           # Context API (autenticação)
│
├── docs/                       # Documentação adicional
│   ├── e2e-test.md            # Roteiro de testes E2E
│   └── troubleshooting.md     # Guia de resolução de problemas
│
└── tasks/                      # Tarefas e especificações
    └── prd-sistema-auditoria-transacoes/
        ├── prd.md
        ├── techspec.md
        └── *.md                # Tarefas individuais
```

## Comandos Úteis

### Docker Compose

```bash
# Iniciar todos os serviços
docker-compose up -d

# Parar todos os serviços
docker-compose down

# Parar e remover volumes (reset completo)
docker-compose down -v

# Ver logs de todos os serviços
docker-compose logs -f

# Ver logs de um serviço específico
docker-compose logs -f ms-contas

# Ver status dos serviços
docker-compose ps

# Reconstruir um serviço específico
docker-compose up -d --build ms-contas

# Reconstruir todos os serviços
docker-compose up -d --build
```

### Debugging

```bash
# Executar comando em um container
docker exec -it poc-postgres psql -U postgres -d poc_auditoria

# Ver schemas do PostgreSQL
docker exec -it poc-postgres psql -U postgres -d poc_auditoria -c "\dn"

# Ver tabelas de um schema
docker exec -it poc-postgres psql -U postgres -d poc_auditoria -c "\dt contas.*"

# Acessar shell de um container
docker exec -it poc-ms-contas bash

# Ver filas do RabbitMQ
curl -u guest:guest http://localhost:15672/api/queues
```

## Monitoramento

### Health Checks

Todos os serviços implementam health checks que podem ser consultados:

```bash
# MS-Contas
curl http://localhost:8080/actuator/health

# MS-Transações
curl http://localhost:5000/health

# MS-Auditoria
curl http://localhost:5001/health

# Status de todos os containers
docker-compose ps
```

### Logs

```bash
# Ver logs em tempo real
docker-compose logs -f

# Ver últimas 100 linhas
docker-compose logs --tail=100

# Ver logs de um serviço específico desde um horário
docker-compose logs --since 30m ms-auditoria
```

## Troubleshooting

Consulte [docs/troubleshooting.md](docs/troubleshooting.md) para um guia completo de resolução de problemas.

### Problemas Comuns

#### Serviço não inicia
```bash
# Verificar logs
docker-compose logs <service-name>

# Reiniciar serviço
docker-compose restart <service-name>
```

#### Elasticsearch com erro de memória
Ajuste o `ES_JAVA_OPTS` no arquivo `.env`:
```env
ES_JAVA_OPTS=-Xms256m -Xmx256m
```

#### Mensagens não chegam ao Elasticsearch
```bash
# Verificar se a fila existe e tem mensagens
curl -u guest:guest http://localhost:15672/api/queues/%2F/audit-queue

# Ver logs do MS-Auditoria
docker-compose logs ms-auditoria

# Verificar se há mensagens na fila de erro
curl -u guest:guest http://localhost:15672/api/queues/%2F/audit-error-queue
```

## Características Técnicas

### 🎯 Pontos-Chave da Arquitetura

1. **Auditoria na Camada de Aplicação**
   - Java: Hibernate Event Listeners (`PreInsertEventListener`, `PreUpdateEventListener`, `PreDeleteEventListener`)
   - .NET: EF Core `SaveChangesInterceptor` + `ChangeTracker`
   - ✅ Transparente: zero alteração no código de negócio

2. **Processamento Assíncrono**
   - Eventos publicados no RabbitMQ de forma não-bloqueante
   - Operação principal não é afetada por falhas na auditoria
   - DLQ (Dead Letter Queue) para eventos com erro

3. **Armazenamento Otimizado para Consulta**
   - Elasticsearch para indexação e busca eficiente
   - Índices separados por serviço de origem
   - Schema flexível para diferentes tipos de entidades

4. **Rastreabilidade Completa**
   - Correlation ID para rastrear múltiplos eventos da mesma requisição
   - Usuário capturado do contexto de autenticação
   - Timestamp preciso de cada operação

5. **Diff Automático**
   - Valores anteriores e novos capturados automaticamente
   - Lista de campos alterados calculada
   - Visualização amigável no frontend

### ⚠️ Limitações da POC

- **Autenticação**: Hardcoded (admin/admin123, user/user123)
- **Paginação**: Não implementada nas APIs
- **Testes**: Sem testes automatizados
- **Monitoramento**: Logs básicos apenas
- **Retenção**: Sem política de arquivamento/limpeza
- **Retry**: Sem retry automático em falhas

### 🚀 Possíveis Evoluções

- [ ] Autenticação via OAuth2/JWT
- [ ] Paginação e filtros avançados
- [ ] Testes automatizados (unitários, integração, E2E)
- [ ] Observabilidade (métricas, tracing, APM)
- [ ] Política de retenção de dados
- [ ] Retry com backoff exponencial
- [ ] Criptografia de dados sensíveis
- [ ] Assinatura digital dos eventos
- [ ] Kibana para visualizações avançadas

## Documentação Adicional

- [Roteiro de Testes E2E](docs/e2e-test.md)
- [Guia de Troubleshooting](docs/troubleshooting.md)
- [PRD - Product Requirements Document](tasks/prd-sistema-auditoria-transacoes/prd.md)
- [Tech Spec - Especificação Técnica](tasks/prd-sistema-auditoria-transacoes/techspec.md)

## Licença

MIT License

## Autores

Projeto desenvolvido como POC para validação de arquitetura de auditoria transparente.
