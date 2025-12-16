---
status: completed
parallelizable: false
blocked_by: []
---

<task_context>
<domain>infra/docker</domain>
<type>configuration</type>
<scope>infrastructure</scope>
<complexity>medium</complexity>
<dependencies>docker, postgresql, rabbitmq, elasticsearch</dependencies>
<unblocks>2.0, 3.0, 4.0</unblocks>
</task_context>

# Tarefa 1.0: Infraestrutura Base (Docker Compose) ✅ CONCLUÍDA

## Visão Geral

Configurar toda a infraestrutura necessária para executar a POC localmente usando Docker Compose. Esta tarefa é **bloqueadora** para todas as demais, pois define os serviços de banco de dados, mensageria e busca.

<requirements>
- Docker e Docker Compose instalados
- Portas 5432, 5672, 15672, 9200 disponíveis
- Conhecimento básico de Docker e SQL
</requirements>

## Subtarefas

- [x] 1.1 Criar estrutura de diretórios do projeto ✅
- [x] 1.2 Criar `docker-compose.yml` com serviços base (PostgreSQL, RabbitMQ, Elasticsearch) ✅
- [x] 1.3 Criar script SQL de inicialização (`scripts/init.sql`) ✅
- [x] 1.4 Configurar volumes para persistência de dados ✅
- [x] 1.5 Configurar rede Docker para comunicação entre serviços ✅
- [x] 1.6 Testar subida dos containers e conectividade ✅

## Sequenciamento

- **Bloqueado por:** Nenhuma tarefa
- **Desbloqueia:** 2.0, 3.0, 4.0
- **Paralelizável:** Não (é a primeira tarefa)

## Detalhes de Implementação

### 1.1 Estrutura de Diretórios

```
poc-auditoria/
├── docker-compose.yml
├── scripts/
│   └── init.sql
├── ms-contas/
├── ms-transacoes/
├── ms-auditoria/
└── frontend/
```

### 1.2 Docker Compose Base

Criar `docker-compose.yml` na raiz com:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    container_name: poc-postgres
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
    networks:
      - poc-network

  rabbitmq:
    image: rabbitmq:3.12-management
    container_name: poc-rabbitmq
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
      test: curl -s http://localhost:9200 >/dev/null || exit 1
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - poc-network

networks:
  poc-network:
    driver: bridge

volumes:
  postgres_data:
  elasticsearch_data:
```

### 1.3 Script SQL de Inicialização

Criar `scripts/init.sql`:

```sql
-- ===========================================
-- Schema: contas (MS-Contas)
-- ===========================================
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
    tipo VARCHAR(20) NOT NULL,
    ativa BOOLEAN DEFAULT TRUE,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_contas_usuario ON contas.contas_bancarias(usuario_id);

-- ===========================================
-- Schema: transacoes (MS-Transacoes)
-- ===========================================
CREATE SCHEMA IF NOT EXISTS transacoes;

CREATE TABLE transacoes.transacoes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_origem_id UUID NOT NULL,
    conta_destino_id UUID,
    tipo VARCHAR(20) NOT NULL,
    valor DECIMAL(18,2) NOT NULL,
    descricao VARCHAR(255),
    status VARCHAR(20) DEFAULT 'PENDENTE',
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processado_em TIMESTAMP
);

CREATE INDEX idx_transacoes_conta_origem ON transacoes.transacoes(conta_origem_id);
CREATE INDEX idx_transacoes_data ON transacoes.transacoes(criado_em);

-- ===========================================
-- Dados Iniciais (Seed)
-- ===========================================
INSERT INTO contas.usuarios (id, nome, email, senha_hash) VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Administrador', 'admin@poc.com', 'admin123'),
    ('22222222-2222-2222-2222-222222222222', 'Usuário Teste', 'user@poc.com', 'user123');

INSERT INTO contas.contas_bancarias (numero_conta, usuario_id, saldo, tipo) VALUES
    ('0001-1', '11111111-1111-1111-1111-111111111111', 10000.00, 'CORRENTE'),
    ('0001-2', '22222222-2222-2222-2222-222222222222', 5000.00, 'CORRENTE'),
    ('0002-1', '22222222-2222-2222-2222-222222222222', 2000.00, 'POUPANCA');
```

### 1.6 Comandos de Teste

```bash
# Subir infraestrutura
docker-compose up -d postgres rabbitmq elasticsearch

# Verificar status
docker-compose ps

# Testar PostgreSQL
docker exec -it poc-postgres psql -U poc_user -d poc_auditoria -c "\dt contas.*"

# Testar RabbitMQ (acessar UI)
# http://localhost:15672 (guest/guest)

# Testar Elasticsearch
curl http://localhost:9200
```

## Critérios de Sucesso

- [x] `docker-compose up -d` executa sem erros ✅
- [x] PostgreSQL acessível na porta 5432 com schemas `contas` e `transacoes` criados ✅
- [x] RabbitMQ acessível na porta 5672 e UI na 15672 ✅
- [x] Elasticsearch acessível na porta 9200 ✅
- [x] Dados de seed inseridos corretamente ✅
- [x] Containers reiniciam automaticamente em caso de falha ✅

## Estimativa

**Tempo:** 4-6 horas  
**Status:** ✅ **CONCLUÍDA**

## Validação Final

- [x] 1.1 Implementação completada ✅
- [x] 1.2 Definição da tarefa, PRD e tech spec validados ✅
- [x] 1.3 Análise de regras e conformidade verificadas ✅
- [x] 1.4 Revisão de código completada ✅
- [x] 1.5 Pronto para deploy ✅

**Relatório de Revisão:** [1_task_review.md](1_task_review.md)

---

**Referências:**
- Tech Spec: Seção "Docker Compose"
- PRD: RF-40 a RF-46
