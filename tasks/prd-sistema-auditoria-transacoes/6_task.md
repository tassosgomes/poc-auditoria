---
status: pending
parallelizable: false
blocked_by: ["1.0", "2.0", "3.0", "4.0", "5.0"]
---

<task_context>
<domain>devops/integration</domain>
<type>configuration</type>
<scope>full_system</scope>
<complexity>medium</complexity>
<dependencies>todas-as-tarefas-anteriores</dependencies>
<unblocks>none</unblocks>
</task_context>

# Tarefa 6.0: Integração Final e Documentação

## Visão Geral

Consolidar todos os microserviços em um ambiente Docker Compose funcional, criar a documentação final e executar testes de integração end-to-end para validar o fluxo completo de auditoria.

<requirements>
- Todas as tarefas anteriores concluídas
- Docker e Docker Compose instalados
- Todos os Dockerfiles individuais criados
</requirements>

## Subtarefas

- [ ] 6.1 Consolidar docker-compose.yml com todos os serviços
- [ ] 6.2 Criar script de build completo
- [ ] 6.3 Criar arquivo .env de exemplo
- [ ] 6.4 Criar README.md principal
- [ ] 6.5 Testar startup completo do ambiente
- [ ] 6.6 Validar comunicação entre serviços
- [ ] 6.7 Executar cenário de teste E2E
- [ ] 6.8 Validar auditoria no Elasticsearch
- [ ] 6.9 Documentar troubleshooting comum
- [ ] 6.10 Criar script de seed de dados (opcional)

## Sequenciamento

- **Bloqueado por:** 1.0, 2.0, 3.0, 4.0, 5.0 (todas as tarefas)
- **Desbloqueia:** Nenhuma (tarefa final)
- **Paralelizável:** Não

## Detalhes de Implementação

### 6.1 docker-compose.yml Final

```yaml
version: '3.8'

services:
  # ====================
  # INFRAESTRUTURA
  # ====================
  postgres:
    image: postgres:16-alpine
    container_name: poc-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
      POSTGRES_DB: poc_auditoria
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./infra/init.sql:/docker-entrypoint-initdb.d/init.sql:ro
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5
    networks:
      - poc-network

  rabbitmq:
    image: rabbitmq:3.12-management-alpine
    container_name: poc-rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: rabbitmq
      RABBITMQ_DEFAULT_PASS: rabbitmq123
    ports:
      - "5672:5672"
      - "15672:15672"
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "check_port_connectivity"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - poc-network

  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    container_name: poc-elasticsearch
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
    ports:
      - "9200:9200"
    volumes:
      - elasticsearch_data:/usr/share/elasticsearch/data
    healthcheck:
      test: ["CMD-SHELL", "curl -s http://localhost:9200/_cluster/health | grep -q 'green\\|yellow'"]
      interval: 10s
      timeout: 5s
      retries: 10
    networks:
      - poc-network

  # ====================
  # MICROSERVIÇOS
  # ====================
  ms-contas:
    build:
      context: ./ms-contas
      dockerfile: Dockerfile
    container_name: poc-ms-contas
    environment:
      SPRING_PROFILES_ACTIVE: docker
      SPRING_DATASOURCE_URL: jdbc:postgresql://postgres:5432/poc_auditoria?currentSchema=contas
      SPRING_DATASOURCE_USERNAME: postgres
      SPRING_DATASOURCE_PASSWORD: postgres123
      SPRING_RABBITMQ_HOST: rabbitmq
      SPRING_RABBITMQ_USERNAME: rabbitmq
      SPRING_RABBITMQ_PASSWORD: rabbitmq123
    ports:
      - "8080:8080"
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/actuator/health || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s
    networks:
      - poc-network

  ms-transacoes:
    build:
      context: ./ms-transacoes
      dockerfile: Dockerfile
    container_name: poc-ms-transacoes
    environment:
      ASPNETCORE_ENVIRONMENT: Docker
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=poc_auditoria;Username=postgres;Password=postgres123;Search Path=transacoes"
      RabbitMQ__Host: rabbitmq
      RabbitMQ__Username: rabbitmq
      RabbitMQ__Password: rabbitmq123
      ContasApi__BaseUrl: http://ms-contas:8080
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
      ms-contas:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s
    networks:
      - poc-network

  ms-auditoria:
    build:
      context: ./ms-auditoria
      dockerfile: Dockerfile
    container_name: poc-ms-auditoria
    environment:
      ASPNETCORE_ENVIRONMENT: Docker
      RabbitMQ__Host: rabbitmq
      RabbitMQ__Username: rabbitmq
      RabbitMQ__Password: rabbitmq123
      Elasticsearch__Uri: http://elasticsearch:9200
    ports:
      - "5001:8080"
    depends_on:
      rabbitmq:
        condition: service_healthy
      elasticsearch:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s
    networks:
      - poc-network

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    container_name: poc-frontend
    ports:
      - "3000:3000"
    depends_on:
      - ms-contas
      - ms-transacoes
      - ms-auditoria
    networks:
      - poc-network

volumes:
  postgres_data:
  rabbitmq_data:
  elasticsearch_data:

networks:
  poc-network:
    driver: bridge
```

### 6.2 Script de Build

```bash
#!/bin/bash
# build.sh

set -e

echo "========================================"
echo "POC Auditoria - Build Completo"
echo "========================================"

# Cores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}[1/5] Building MS-Contas (Java)...${NC}"
cd ms-contas
./mvnw clean package -DskipTests
cd ..

echo -e "${YELLOW}[2/5] Building MS-Transacoes (.NET)...${NC}"
cd ms-transacoes
dotnet publish -c Release -o ./publish
cd ..

echo -e "${YELLOW}[3/5] Building MS-Auditoria (.NET)...${NC}"
cd ms-auditoria
dotnet publish -c Release -o ./publish
cd ..

echo -e "${YELLOW}[4/5] Building Frontend (React)...${NC}"
cd frontend
npm ci
npm run build
cd ..

echo -e "${YELLOW}[5/5] Building Docker images...${NC}"
docker-compose build

echo -e "${GREEN}========================================"
echo "Build concluído com sucesso!"
echo "========================================${NC}"
echo ""
echo "Para iniciar o ambiente, execute:"
echo "  docker-compose up -d"
```

### 6.3 Arquivo .env.example

```env
# PostgreSQL
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres123
POSTGRES_DB=poc_auditoria

# RabbitMQ
RABBITMQ_USER=rabbitmq
RABBITMQ_PASSWORD=rabbitmq123

# Elasticsearch
ES_JAVA_OPTS=-Xms512m -Xmx512m

# Frontend
VITE_API_CONTAS=http://localhost:8080
VITE_API_TRANSACOES=http://localhost:5000
VITE_API_AUDITORIA=http://localhost:5001
```

### 6.4 README.md Principal

```markdown
# POC - Sistema de Auditoria de Transações Bancárias

## Visão Geral

Este projeto é uma prova de conceito (POC) para validar um sistema de auditoria transparente 
para transações bancárias. A auditoria captura automaticamente operações de INSERT, UPDATE 
e DELETE registrando quem, quando e o quê foi alterado.

## Arquitetura

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Frontend   │────▶│  MS-Contas  │────▶│ PostgreSQL  │
│   (React)   │     │   (Java)    │     │ (schema:    │
└─────────────┘     └──────┬──────┘     │   contas)   │
       │                   │            └─────────────┘
       │           ┌───────▼───────┐           │
       │           │   RabbitMQ    │◀──────────┘
       │           └───────┬───────┘
       │                   │
       ▼                   ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│MS-Transações│     │MS-Auditoria │────▶│Elasticsearch│
│   (.NET)    │────▶│   (.NET)    │     │             │
└──────┬──────┘     └─────────────┘     └─────────────┘
       │
       ▼
┌─────────────┐
│ PostgreSQL  │
│ (schema:    │
│ transacoes) │
└─────────────┘
```

## Tecnologias

| Componente | Tecnologia |
|------------|------------|
| MS-Contas | Java 21, Spring Boot 3.2, Hibernate |
| MS-Transações | .NET 8, EF Core 8 |
| MS-Auditoria | .NET 8, Elastic.Clients.Elasticsearch |
| Frontend | React 18, Vite, Tailwind CSS |
| Banco de Dados | PostgreSQL 16 |
| Mensageria | RabbitMQ 3.12 |
| Busca | Elasticsearch 8.11 |

## Pré-requisitos

- Docker 24+
- Docker Compose 2.20+
- (Opcional) Java 21, .NET 8, Node.js 20

## Como Executar

### Usando Docker Compose (Recomendado)

```bash
# Clone o repositório
git clone <repo-url>
cd poc-auditoria

# Inicie todos os serviços
docker-compose up -d

# Acompanhe os logs
docker-compose logs -f
```

### Desenvolvimento Local

```bash
# 1. Inicie apenas a infraestrutura
docker-compose up -d postgres rabbitmq elasticsearch

# 2. MS-Contas (terminal 1)
cd ms-contas
./mvnw spring-boot:run

# 3. MS-Transações (terminal 2)
cd ms-transacoes
dotnet run

# 4. MS-Auditoria (terminal 3)
cd ms-auditoria
dotnet run

# 5. Frontend (terminal 4)
cd frontend
npm run dev
```

## URLs de Acesso

| Serviço | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| MS-Contas API | http://localhost:8080/api/v1 |
| MS-Transações API | http://localhost:5000/api/v1 |
| MS-Auditoria API | http://localhost:5001/api/v1 |
| RabbitMQ Management | http://localhost:15672 |
| Elasticsearch | http://localhost:9200 |

## Credenciais

| Serviço | Usuário | Senha |
|---------|---------|-------|
| Frontend/APIs | admin | admin123 |
| Frontend/APIs | user | user123 |
| RabbitMQ | rabbitmq | rabbitmq123 |
| PostgreSQL | postgres | postgres123 |

## Testando o Fluxo de Auditoria

1. Acesse o frontend em http://localhost:3000
2. Faça login com admin/admin123
3. Crie um usuário em "Usuários"
4. Crie uma conta em "Contas"
5. Faça uma transação (depósito/saque)
6. Acesse "Auditoria" para ver os eventos
7. Clique em um evento para ver o diff

## Verificando Auditoria via API

```bash
# Listar eventos de auditoria
curl -u admin:admin123 http://localhost:5001/api/v1/audit

# Buscar por entidade
curl -u admin:admin123 "http://localhost:5001/api/v1/audit?entityName=Usuario"

# Ver detalhes de um evento
curl -u admin:admin123 http://localhost:5001/api/v1/audit/{id}
```

## Verificando Elasticsearch Diretamente

```bash
# Ver índices criados
curl http://localhost:9200/_cat/indices?v

# Buscar eventos
curl http://localhost:9200/audit-ms-contas/_search?pretty
```

## Estrutura do Projeto

```
poc-auditoria/
├── docker-compose.yml
├── README.md
├── .env.example
├── infra/
│   └── init.sql
├── ms-contas/           # Java/Spring Boot
│   ├── src/
│   ├── pom.xml
│   └── Dockerfile
├── ms-transacoes/       # .NET 8
│   ├── src/
│   ├── MsTransacoes.csproj
│   └── Dockerfile
├── ms-auditoria/        # .NET 8
│   ├── src/
│   ├── MsAuditoria.csproj
│   └── Dockerfile
└── frontend/            # React/Vite
    ├── src/
    ├── package.json
    └── Dockerfile
```

## Troubleshooting

### Serviço não inicia

```bash
# Verifique os logs
docker-compose logs <service-name>

# Reinicie o serviço
docker-compose restart <service-name>
```

### Elasticsearch com erro de memória

```bash
# Aumente a memória do host ou ajuste ES_JAVA_OPTS
# No docker-compose.yml, reduza para -Xms256m -Xmx256m
```

### RabbitMQ não aceita conexões

```bash
# Aguarde o healthcheck (pode demorar ~30s)
docker-compose ps

# Verifique se a porta está acessível
curl http://localhost:15672
```

### Mensagens não chegam no Elasticsearch

```bash
# Verifique a fila de erros
curl -u rabbitmq:rabbitmq123 http://localhost:15672/api/queues

# Verifique logs do MS-Auditoria
docker-compose logs ms-auditoria
```

## Licença

MIT License
```

### 6.7 Cenário de Teste E2E

```markdown
# Roteiro de Teste E2E

## Objetivo
Validar o fluxo completo de auditoria desde a operação no frontend até a visualização do diff.

## Pré-condições
- [ ] Todos os containers rodando (docker-compose ps)
- [ ] Frontend acessível em localhost:3000
- [ ] Elasticsearch healthy

## Cenário 1: Criação de Usuário

1. **Login**
   - Acessar http://localhost:3000
   - Login: admin / admin123
   - ✅ Redireciona para Dashboard

2. **Criar Usuário**
   - Menu: Usuários
   - Clicar: Novo Usuário
   - Preencher: Nome="João Silva", CPF="12345678901", Email="joao@test.com"
   - Clicar: Salvar
   - ✅ Usuário aparece na lista

3. **Verificar Auditoria**
   - Menu: Auditoria
   - Filtrar: entityName="Usuario"
   - ✅ Evento INSERT visível
   - Clicar no evento
   - ✅ Diff mostra: oldValues=null, newValues={nome:"João Silva",...}

## Cenário 2: Atualização de Usuário

1. **Editar Usuário**
   - Menu: Usuários
   - Clicar: Editar (João Silva)
   - Alterar: Nome="João da Silva"
   - Clicar: Salvar
   - ✅ Nome atualizado na lista

2. **Verificar Auditoria**
   - Menu: Auditoria
   - ✅ Evento UPDATE visível
   - Clicar no evento
   - ✅ Diff mostra: oldValues.nome="João Silva", newValues.nome="João da Silva"

## Cenário 3: Transação com Auditoria

1. **Criar Conta**
   - Menu: Contas
   - Nova Conta para João Silva
   - Saldo inicial: 0
   - ✅ Conta criada

2. **Depósito**
   - Menu: Transações
   - Tipo: Depósito
   - Conta: (selecionar conta do João)
   - Valor: 1000
   - ✅ Depósito realizado

3. **Verificar Auditoria**
   - Menu: Auditoria
   - Filtrar: sourceService="ms-transacoes"
   - ✅ Evento INSERT para Transacao
   - ✅ Evento UPDATE para Conta (saldo alterado)

## Cenário 4: Exclusão

1. **Excluir Usuário (sem conta)**
   - Criar usuário temporário
   - Excluir usuário
   - ✅ Usuário removido da lista

2. **Verificar Auditoria**
   - ✅ Evento DELETE visível
   - ✅ Diff mostra: oldValues={dados completos}, newValues=null

## Resultado Esperado
- [ ] Todos os cenários passaram
- [ ] Tempo de propagação < 5 segundos
- [ ] Diffs corretos em todas as operações
```

### 6.9 Troubleshooting Comum

```markdown
# Troubleshooting

## Problemas de Startup

### Container fica em "starting" indefinidamente

**Causa:** Healthcheck falhando
**Solução:**
```bash
# Ver logs do container
docker logs poc-<service-name>

# Verificar se dependências estão healthy
docker-compose ps
```

### MS-Contas não conecta no PostgreSQL

**Causa:** PostgreSQL ainda inicializando ou init.sql com erro
**Solução:**
```bash
# Verificar se schema foi criado
docker exec -it poc-postgres psql -U postgres -d poc_auditoria -c "\dn"

# Re-executar init
docker-compose down -v
docker-compose up -d
```

## Problemas de Comunicação

### MS-Transações não atualiza saldo na conta

**Causa:** URL da API de contas incorreta ou MS-Contas offline
**Solução:**
```bash
# Testar conectividade interna
docker exec -it poc-ms-transacoes curl http://ms-contas:8080/actuator/health
```

### Eventos não aparecem no Elasticsearch

**Causa:** 
1. RabbitMQ não roteando mensagens
2. MS-Auditoria não consumindo
3. Elasticsearch rejeitando documentos

**Diagnóstico:**
```bash
# 1. Verificar se mensagens estão na fila
curl -u rabbitmq:rabbitmq123 http://localhost:15672/api/queues/%2F/audit-queue

# 2. Ver logs do consumer
docker logs poc-ms-auditoria

# 3. Ver erros no Elasticsearch
curl http://localhost:9200/audit-*/_search?q=*&size=1
```

### Erro 401 nas APIs

**Causa:** Header Authorization não está sendo enviado
**Solução:**
- Verificar se localStorage.credentials está setado após login
- Inspecionar Network tab no DevTools

## Problemas de Performance

### Elasticsearch lento ou OOM

**Solução:**
```yaml
# docker-compose.yml
elasticsearch:
  environment:
    - "ES_JAVA_OPTS=-Xms256m -Xmx256m"
  deploy:
    resources:
      limits:
        memory: 1G
```

### Build muito demorado

**Solução:**
```bash
# Usar cache de dependências
docker-compose build --parallel

# Para Java, use cache do Maven
# No Dockerfile do ms-contas, use:
# RUN --mount=type=cache,target=/root/.m2 ./mvnw package
```

## Reset Completo

```bash
# Para e remove tudo (volumes inclusos)
docker-compose down -v

# Remove imagens do projeto
docker images | grep poc | awk '{print $3}' | xargs docker rmi -f

# Rebuild completo
docker-compose up -d --build
```
```

## Estrutura Final do Projeto

```
poc-auditoria/
├── docker-compose.yml
├── .env.example
├── build.sh
├── README.md
├── infra/
│   └── init.sql
├── ms-contas/
│   ├── Dockerfile
│   ├── pom.xml
│   └── src/
├── ms-transacoes/
│   ├── Dockerfile
│   ├── MsTransacoes.csproj
│   └── src/
├── ms-auditoria/
│   ├── Dockerfile
│   ├── MsAuditoria.csproj
│   └── src/
├── frontend/
│   ├── Dockerfile
│   ├── nginx.conf
│   ├── package.json
│   └── src/
├── tasks/
│   └── prd-sistema-auditoria-transacoes/
│       ├── prd.md
│       ├── techspec.md
│       ├── tasks.md
│       ├── 1_task.md
│       ├── 2_task.md
│       ├── 3_task.md
│       ├── 4_task.md
│       ├── 5_task.md
│       └── 6_task.md
└── docs/
    ├── e2e-test.md
    └── troubleshooting.md
```

## Critérios de Sucesso

- [ ] `docker-compose up -d` inicia todos os serviços sem erros
- [ ] Todos os healthchecks passam (docker-compose ps mostra "healthy")
- [ ] Frontend acessível em localhost:3000
- [ ] Login funciona com admin/admin123
- [ ] Operações CRUD geram eventos de auditoria
- [ ] Eventos aparecem no Elasticsearch em < 5 segundos
- [ ] Tela de auditoria mostra diff correto
- [ ] README documenta como executar o projeto

## Estimativa

**Tempo:** 1 dia (8 horas)

---

**Referências:**
- Tech Spec: Seção "Docker Compose"
- PRD: RF-44 a RF-46
