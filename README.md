# POC - Sistema de Auditoria de Transações Bancárias

## Visão Geral

Este projeto é uma prova de conceito (POC) para validar um sistema de auditoria transparente para transações bancárias. A auditoria captura automaticamente operações de INSERT, UPDATE e DELETE registrando **quem**, **quando** e **o quê** foi alterado.

### Objetivo Principal

Validar a viabilidade de uma arquitetura de auditoria transparente que:
- Capture eventos de forma automática no banco de dados (sem alteração no código de negócio)
- Processe eventos de forma assíncrona via mensageria
- Armazene e permita consulta eficiente dos logs de auditoria

## Arquitetura

```
┌─────────────┐     ┌─────────────────────────────┐     ┌─────────────────┐
│  Frontend   │────▶│  MS-Contas (Java/Spring)    │────▶│   PostgreSQL    │
│   (React)   │     │  + Hibernate Event Listeners│     │  (schema:contas)│
└─────────────┘     └──────────────┬──────────────┘     └─────────────────┘
       │                           │                              │
       │                           │ Eventos de Auditoria         │
       │                           ▼                              │
       │             ┌─────────────────────────┐                  │
       │             │      RabbitMQ           │◀─────────────────┘
       │             └──────────┬──────────────┘
       │                        │                              
       ▼                        ▼                              
┌─────────────┐     ┌─────────────────────────────┐     ┌─────────────────┐
│MS-Transações│     │   MS-Auditoria (.NET 8)     │────▶│  Elasticsearch  │
│   (.NET)    │────▶│                             │     │                 │
└──────┬──────┘     └─────────────────────────────┘     └─────────────────┘
       │
       ▼
┌─────────────────┐
│   PostgreSQL    │
│(schema:transacoes│
└─────────────────┘
```

## Tecnologias

| Componente | Tecnologia | Versão |
|------------|------------|--------|
| MS-Contas | Java, Spring Boot, Hibernate | Java 21, Spring Boot 3.2 |
| MS-Transações | .NET, EF Core | .NET 8, EF Core 8 |
| MS-Auditoria | .NET, Elasticsearch | .NET 8, Elastic.Clients.Elasticsearch 8.11 |
| Frontend | React, Vite, Tailwind CSS | React 18, Vite 5 |
| Banco de Dados | PostgreSQL | PostgreSQL 16 |
| Mensageria | RabbitMQ | RabbitMQ 3.12 |
| Busca | Elasticsearch | Elasticsearch 8.11 |

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
| RabbitMQ | rabbitmq | rabbitmq123 |
| PostgreSQL | postgres | postgres123 |
| Elasticsearch | - | (sem autenticação) |

## Testando o Fluxo de Auditoria

### Teste Rápido via Interface Web

1. Acesse http://localhost:3000
2. Faça login com **admin** / **admin123**
3. Crie um usuário em "Usuários"
   - Nome: "João Silva"
   - CPF: "12345678901"
   - Email: "joao@test.com"
4. Crie uma conta para o usuário em "Contas"
5. Faça uma transação de depósito em "Transações"
6. Acesse "Auditoria" para ver todos os eventos
7. Clique em um evento para ver o diff detalhado

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
```

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
curl -u rabbitmq:rabbitmq123 http://localhost:15672/api/queues
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
curl -u rabbitmq:rabbitmq123 http://localhost:15672/api/queues/%2F/audit-queue

# Ver logs do MS-Auditoria
docker-compose logs ms-auditoria
```

## Documentação Adicional

- [Roteiro de Testes E2E](docs/e2e-test.md)
- [Guia de Troubleshooting](docs/troubleshooting.md)
- [PRD - Product Requirements Document](tasks/prd-sistema-auditoria-transacoes/prd.md)
- [Tech Spec - Especificação Técnica](tasks/prd-sistema-auditoria-transacoes/techspec.md)

## Licença

MIT License

## Autores

Projeto desenvolvido como POC para validação de arquitetura de auditoria transparente.
