# Guia de Troubleshooting - POC Auditoria

Este documento contém soluções para problemas comuns encontrados durante a execução da POC de Sistema de Auditoria.

---

## Índice

- [Problemas de Startup](#problemas-de-startup)
- [Problemas de Comunicação](#problemas-de-comunicação)
- [Problemas de Performance](#problemas-de-performance)
- [Problemas de Auditoria](#problemas-de-auditoria)
- [Problemas de Infraestrutura](#problemas-de-infraestrutura)
- [Reset Completo](#reset-completo)

---

## Problemas de Startup

### Container fica em "starting" indefinidamente

**Sintomas:**
- `docker-compose ps` mostra serviço como "starting" por mais de 2 minutos
- Container não passa no healthcheck

**Causa Comum:**
- Healthcheck falhando
- Dependências não iniciadas
- Erro na aplicação

**Solução:**

```bash
# 1. Ver logs detalhados do container
docker logs poc-<service-name>

# 2. Verificar se dependências estão healthy
docker-compose ps

# 3. Verificar recursos do sistema
docker stats

# 4. Reiniciar o serviço
docker-compose restart <service-name>

# 5. Se persistir, reconstruir
docker-compose up -d --build <service-name>
```

---

### MS-Contas não conecta no PostgreSQL

**Sintomas:**
- MS-Contas falha ao iniciar
- Logs mostram: "Connection refused" ou "Unknown database"

**Causa Comum:**
- PostgreSQL ainda inicializando
- Schema não foi criado
- Credenciais incorretas

**Solução:**

```bash
# 1. Verificar se PostgreSQL está healthy
docker-compose ps postgres

# 2. Verificar se os schemas foram criados
docker exec -it poc-postgres psql -U postgres -d poc_auditoria -c "\dn"

# Deve mostrar: contas, transacoes, public

# 3. Se schemas não existem, verificar o init.sql
docker exec -it poc-postgres psql -U postgres -d poc_auditoria -c "\dt contas.*"

# 4. Se necessário, recriar o banco
docker-compose down -v
docker-compose up -d postgres
# Aguardar 10 segundos
docker-compose up -d ms-contas
```

---

### Elasticsearch não inicia (Out of Memory)

**Sintomas:**
- Elasticsearch crashloop
- Logs mostram: "OutOfMemoryError" ou "insufficient memory"

**Causa:**
- Pouca memória disponível no host
- ES_JAVA_OPTS muito alto

**Solução:**

```bash
# 1. Ajustar memória no .env
echo "ES_JAVA_OPTS=-Xms256m -Xmx256m" >> .env

# 2. Reiniciar Elasticsearch
docker-compose down elasticsearch
docker-compose up -d elasticsearch

# 3. Monitorar memória
docker stats poc-elasticsearch

# 4. Se ainda falhar, aumentar memória do Docker
# Docker Desktop: Settings > Resources > Memory (mínimo 4GB)
```

---

### RabbitMQ não aceita conexões

**Sintomas:**
- Serviços não conseguem conectar no RabbitMQ
- Logs: "Connection refused" na porta 5672

**Causa:**
- RabbitMQ ainda inicializando (pode demorar até 30s)
- Porta em uso por outro serviço

**Solução:**

```bash
# 1. Aguardar o healthcheck passar
docker-compose ps rabbitmq
# Status deve ser "Up (healthy)"

# 2. Verificar se porta está acessível
curl http://localhost:15672
# Deve retornar página de login

# 3. Testar autenticação
curl -u rabbitmq:rabbitmq123 http://localhost:15672/api/overview

# 4. Ver logs
docker-compose logs rabbitmq

# 5. Se porta estiver em uso
sudo netstat -tlnp | grep 5672
# Matar processo ou alterar porta no docker-compose.yml
```

---

## Problemas de Comunicação

### MS-Transações não consegue chamar MS-Contas

**Sintomas:**
- Transações falham ao criar
- Logs mostram: "Connection refused" ou "Network unreachable"

**Causa:**
- MS-Contas offline
- URL incorreta
- Rede Docker mal configurada

**Solução:**

```bash
# 1. Verificar se MS-Contas está rodando
docker-compose ps ms-contas

# 2. Testar conectividade interna do Docker
docker exec -it poc-ms-transacoes curl http://ms-contas:8080/actuator/health

# Deve retornar: {"status":"UP"}

# 3. Verificar configuração de rede
docker network inspect poc-auditoria_poc-network

# Todos os serviços devem aparecer

# 4. Testar do host
curl http://localhost:8080/actuator/health

# 5. Ver logs do MS-Transações
docker-compose logs ms-transacoes | grep -i "ms-contas"
```

---

### Frontend não consegue chamar APIs

**Sintomas:**
- Erro de CORS no console do navegador
- Requisições retornam 404 ou Network Error

**Causa:**
- APIs não estão rodando
- Proxy reverso (nginx) mal configurado
- CORS não habilitado nas APIs

**Solução:**

```bash
# 1. Verificar se APIs estão acessíveis
curl http://localhost:8080/api/v1/usuarios
curl http://localhost:5000/health
curl http://localhost:5001/health

# 2. Verificar configuração do nginx no frontend
docker exec -it poc-frontend cat /etc/nginx/conf.d/default.conf

# 3. Ver logs do navegador
# Abrir DevTools (F12) > Console > Network

# 4. Ver logs do nginx
docker-compose logs frontend

# 5. Testar sem autenticação
curl -i http://localhost:8080/actuator/health
```

---

## Problemas de Performance

### Sistema muito lento

**Sintomas:**
- Requisições demoram mais de 5 segundos
- Interface trava ou congela

**Causa:**
- Recursos insuficientes
- Elasticsearch lento
- Muitos logs acumulados

**Solução:**

```bash
# 1. Verificar uso de recursos
docker stats

# CPU > 90% ou Memory > 90% indica problema

# 2. Verificar tamanho dos índices do Elasticsearch
curl http://localhost:9200/_cat/indices?v

# 3. Limpar índices antigos (se necessário)
curl -X DELETE http://localhost:9200/audit-*

# 4. Limpar logs do Docker
docker system prune -a

# 5. Reduzir tamanho do Elasticsearch
# No .env:
ES_JAVA_OPTS=-Xms256m -Xmx256m

# 6. Desabilitar logs verbose
# Ajustar nível de log nos appsettings.json para Warning
```

---

### Elasticsearch queries lentas

**Sintomas:**
- Auditoria demora para carregar
- Timeout em buscas

**Solução:**

```bash
# 1. Verificar saúde do cluster
curl http://localhost:9200/_cluster/health

# Status deve ser "green" ou "yellow"

# 2. Ver estatísticas de índices
curl http://localhost:9200/_cat/indices?v

# 3. Limitar tamanho da busca
# Na API de auditoria, usar paginação

# 4. Criar índice com mappings otimizados
# (Ver MsAuditoria.Infra/Elasticsearch/ElasticsearchService.cs)

# 5. Adicionar mais memória ao Elasticsearch
ES_JAVA_OPTS=-Xms1g -Xmx1g
```

---

## Problemas de Auditoria

### Eventos não aparecem no Elasticsearch

**Sintomas:**
- Operações são realizadas, mas não aparecem na tela de Auditoria
- API de auditoria retorna lista vazia

**Causa Comum:**
1. RabbitMQ não está roteando mensagens
2. MS-Auditoria não está consumindo
3. Elasticsearch rejeitando documentos

**Diagnóstico:**

```bash
# Passo 1: Verificar se mensagens estão na fila
curl -u rabbitmq:rabbitmq123 http://localhost:15672/api/queues/%2F/audit-queue | jq

# Esperado:
# - "messages": 0 (mensagens sendo processadas)
# - "consumers": 1 (MS-Auditoria conectado)

# Se "messages" > 0 e "consumers" = 0:
# MS-Auditoria não está consumindo

# Passo 2: Ver logs do MS-Auditoria
docker-compose logs ms-auditoria | grep -i error

# Passo 3: Verificar se eventos chegaram no Elasticsearch
curl http://localhost:9200/audit-*/_search?size=5&sort=timestamp:desc | jq

# Passo 4: Ver índices
curl http://localhost:9200/_cat/indices?v | grep audit
```

**Solução:**

```bash
# Se MS-Auditoria não está consumindo:
docker-compose restart ms-auditoria

# Se Elasticsearch está rejeitando:
# Ver logs detalhados
docker-compose logs elasticsearch | grep -i error

# Recriar índices
curl -X DELETE http://localhost:9200/audit-*
docker-compose restart ms-auditoria

# Se fila está acumulando mensagens:
# Purgar a fila (CUIDADO: perde dados)
curl -X DELETE -u rabbitmq:rabbitmq123 \
  http://localhost:15672/api/queues/%2F/audit-queue/contents
```

---

### Diff não mostra valores corretos

**Sintomas:**
- oldValues ou newValues estão null quando não deveriam
- Campos faltando no diff

**Causa:**
- Interceptor não está capturando corretamente
- Serialização incorreta

**Solução:**

```bash
# 1. Ver logs do serviço que gerou o evento
docker-compose logs ms-contas | grep -i audit
docker-compose logs ms-transacoes | grep -i audit

# 2. Verificar evento diretamente na fila (antes de ser consumido)
# Acessar RabbitMQ Management
# http://localhost:15672 > Queues > audit-queue > Get Messages

# 3. Ver payload no Elasticsearch
curl http://localhost:9200/audit-ms-contas/_search?size=1 | jq '.hits.hits[0]._source'

# 4. Verificar se interceptor está registrado
# Java: AuditInterceptor em hibernate.properties
# .NET: AuditInterceptor em Program.cs
```

---

### Auditoria não captura DELETE

**Sintomas:**
- Operações INSERT e UPDATE funcionam
- DELETE não gera evento

**Causa:**
- Interceptor de DELETE não configurado
- Entidade não está sendo rastreada pelo ORM

**Solução:**

```bash
# 1. Verificar logs durante a exclusão
docker-compose logs -f ms-contas

# Deve aparecer: "Publishing audit event: DELETE"

# 2. Verificar configuração do interceptor
# Java: PostDeleteEventListener
# .NET: SaveChangesInterceptor com ChangeTracker.Entries() estado Deleted

# 3. Testar exclusão via API
curl -X DELETE -u admin:admin123 http://localhost:8080/api/v1/usuarios/{id}

# 4. Verificar imediatamente no RabbitMQ
curl -u rabbitmq:rabbitmq123 http://localhost:15672/api/queues/%2F/audit-queue
```

---

## Problemas de Infraestrutura

### PostgreSQL: "Too many connections"

**Solução:**

```bash
# 1. Ver conexões ativas
docker exec -it poc-postgres psql -U postgres -c \
  "SELECT count(*) FROM pg_stat_activity;"

# 2. Matar conexões idle
docker exec -it poc-postgres psql -U postgres -c \
  "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE state = 'idle';"

# 3. Ajustar max_connections
# Editar postgresql.conf ou variável de ambiente
```

---

### RabbitMQ: Fila cheia (memory alarm)

**Sintomas:**
- RabbitMQ para de aceitar mensagens
- Management UI mostra alerta vermelho

**Solução:**

```bash
# 1. Ver uso de memória
curl -u rabbitmq:rabbitmq123 http://localhost:15672/api/nodes | jq '.[0].mem_used'

# 2. Purgar mensagens antigas (CUIDADO: perde dados)
curl -X DELETE -u rabbitmq:rabbitmq123 \
  http://localhost:15672/api/queues/%2F/audit-queue/contents

# 3. Aumentar limite de memória
# docker-compose.yml > rabbitmq > environment:
# RABBITMQ_VM_MEMORY_HIGH_WATERMARK: 1024MB

# 4. Reiniciar
docker-compose restart rabbitmq
```

---

### Docker: "No space left on device"

**Solução:**

```bash
# 1. Verificar espaço em disco
df -h

# 2. Limpar containers parados
docker container prune -f

# 3. Limpar imagens não usadas
docker image prune -a -f

# 4. Limpar volumes órfãos
docker volume prune -f

# 5. Limpar tudo (CUIDADO: remove todos os dados)
docker system prune -a --volumes -f

# 6. Recriar ambiente
cd poc-auditoria
./build.sh
docker-compose up -d
```

---

## Reset Completo

Quando tudo falhar, use este procedimento para reset completo:

```bash
# 1. Parar e remover todos os containers e volumes
cd /path/to/poc-auditoria
docker-compose down -v

# 2. Remover imagens do projeto
docker images | grep poc | awk '{print $3}' | xargs docker rmi -f

# 3. Limpar cache do Docker
docker builder prune -a -f

# 4. Limpar builds locais (opcional)
# Java
cd ms-contas && ./mvnw clean && cd ..

# .NET
cd ms-transacoes && dotnet clean && rm -rf bin obj publish && cd ..
cd ms-auditoria && dotnet clean && rm -rf bin obj publish && cd ..

# Node
cd frontend && rm -rf node_modules dist && cd ..

# 5. Rebuild completo
./build.sh

# 6. Iniciar novamente
docker-compose up -d

# 7. Aguardar todos os serviços ficarem healthy
watch -n 2 docker-compose ps

# 8. Verificar logs
docker-compose logs -f
```

---

## Logs Úteis

### Ver logs de todos os serviços
```bash
docker-compose logs -f
```

### Ver logs de um serviço específico
```bash
docker-compose logs -f ms-contas
docker-compose logs -f ms-transacoes
docker-compose logs -f ms-auditoria
docker-compose logs -f frontend
```

### Ver apenas erros
```bash
docker-compose logs | grep -i error
docker-compose logs | grep -i exception
```

### Ver logs desde um horário
```bash
docker-compose logs --since 30m ms-auditoria
docker-compose logs --since 2024-01-01T10:00:00
```

### Seguir logs em tempo real com filtro
```bash
docker-compose logs -f ms-contas | grep -i "audit"
```

---

## Comandos de Diagnóstico

### Verificar saúde de todos os serviços
```bash
docker-compose ps

# Ou mais detalhado:
docker-compose ps --format json | jq
```

### Executar comando dentro de um container
```bash
# Bash
docker exec -it poc-ms-contas bash

# PostgreSQL
docker exec -it poc-postgres psql -U postgres -d poc_auditoria

# Ver variáveis de ambiente
docker exec -it poc-ms-contas env
```

### Verificar rede Docker
```bash
docker network ls
docker network inspect poc-auditoria_poc-network
```

### Verificar volumes
```bash
docker volume ls
docker volume inspect poc-auditoria_postgres_data
```

### Estatísticas de recursos
```bash
docker stats

# Ou para um serviço específico:
docker stats poc-elasticsearch
```

---

## Suporte Adicional

Se o problema persistir após seguir este guia:

1. **Coletar Informações:**
   ```bash
   # Versão do Docker
   docker --version
   docker-compose --version
   
   # Logs completos
   docker-compose logs > logs.txt
   
   # Status dos serviços
   docker-compose ps > status.txt
   
   # Informações do sistema
   uname -a
   ```

2. **Verificar Documentação:**
   - [README.md](../README.md) - Instruções de instalação
   - [e2e-test.md](e2e-test.md) - Roteiro de testes
   - [Tech Spec](../tasks/prd-sistema-auditoria-transacoes/techspec.md) - Especificação técnica

3. **Questões Comuns:**
   - Certifique-se de estar usando as versões corretas (Docker 24+, Docker Compose 2.20+)
   - Verifique se tem pelo menos 4GB de RAM disponível para o Docker
   - Desabilite VPN que possa interferir na rede Docker
   - No Windows, use WSL2 para melhor performance
