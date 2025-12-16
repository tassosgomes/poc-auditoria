# Roteiro de Teste E2E - POC Auditoria

## Objetivo

Validar o fluxo completo de auditoria desde a operação no frontend até a visualização do diff, garantindo que todos os serviços estão funcionando e se comunicando corretamente.

## Pré-condições

Antes de iniciar os testes, certifique-se de que:

- [ ] Todos os containers estão rodando: `docker-compose ps`
- [ ] Todos os serviços estão healthy (status deve estar "Up (healthy)")
- [ ] Frontend acessível em http://localhost:3000
- [ ] Elasticsearch está respondendo: `curl http://localhost:9200`
- [ ] RabbitMQ Management acessível em http://localhost:15672

```bash
# Verificar status
docker-compose ps

# Verificar logs se algum serviço estiver com problema
docker-compose logs -f
```

## Cenário 1: Criação de Usuário

### Objetivo
Validar que a criação de um usuário gera um evento de auditoria com operação INSERT.

### Passos

1. **Login**
   - Acessar http://localhost:3000
   - Usuário: `admin`
   - Senha: `admin123`
   - ✅ **Esperado:** Redireciona para Dashboard

2. **Criar Usuário**
   - Menu: **Usuários**
   - Clicar: **Novo Usuário**
   - Preencher:
     - Nome: "João Silva"
     - CPF: "12345678901"
     - Email: "joao@test.com"
   - Clicar: **Salvar**
   - ✅ **Esperado:** Usuário aparece na lista

3. **Verificar Auditoria**
   - Menu: **Auditoria**
   - Filtrar: entityName="Usuario"
   - ✅ **Esperado:** Evento INSERT visível na lista
   - Clicar no evento
   - ✅ **Esperado:** Diff mostra:
     - `oldValues`: null
     - `newValues`: {"nome":"João Silva", "cpf":"12345678901", "email":"joao@test.com", ...}
     - `operation`: "INSERT"
     - `performedBy`: "admin"

### Verificação via API

```bash
# Listar eventos de auditoria
curl -u admin:admin123 http://localhost:5001/api/v1/audit?entityName=Usuario | jq

# Verificar no Elasticsearch
curl http://localhost:9200/audit-ms-contas/_search?q=entityName:Usuario | jq
```

---

## Cenário 2: Atualização de Usuário

### Objetivo
Validar que a atualização de um usuário gera um evento de auditoria com operação UPDATE mostrando valores anteriores e novos.

### Passos

1. **Editar Usuário**
   - Menu: **Usuários**
   - Localizar: "João Silva"
   - Clicar: **Editar**
   - Alterar: Nome para "João da Silva"
   - Clicar: **Salvar**
   - ✅ **Esperado:** Nome atualizado na lista

2. **Verificar Auditoria**
   - Menu: **Auditoria**
   - Filtrar: entityName="Usuario"
   - ✅ **Esperado:** Evento UPDATE visível (mais recente)
   - Clicar no evento
   - ✅ **Esperado:** Diff mostra:
     - `oldValues.nome`: "João Silva"
     - `newValues.nome`: "João da Silva"
     - `operation`: "UPDATE"
     - Outros campos inalterados não devem aparecer no diff

### Verificação via API

```bash
# Buscar eventos UPDATE
curl -u admin:admin123 "http://localhost:5001/api/v1/audit?operation=UPDATE&entityName=Usuario" | jq
```

---

## Cenário 3: Criação de Conta

### Objetivo
Validar auditoria de conta bancária e verificar associação com usuário.

### Passos

1. **Criar Conta**
   - Menu: **Contas**
   - Clicar: **Nova Conta**
   - Selecionar: Usuário "João da Silva"
   - Tipo: "Corrente"
   - Clicar: **Salvar**
   - ✅ **Esperado:** Conta criada com saldo 0

2. **Verificar Auditoria**
   - Menu: **Auditoria**
   - Filtrar: entityName="Conta"
   - ✅ **Esperado:** Evento INSERT visível
   - Clicar no evento
   - ✅ **Esperado:** Diff mostra:
     - `newValues.saldo`: 0
     - `newValues.tipo`: "Corrente"
     - `newValues.usuarioId`: {id do João}

---

## Cenário 4: Transação com Auditoria Cross-Service

### Objetivo
Validar que uma transação gera eventos de auditoria em múltiplos serviços (MS-Transações cria transação e MS-Contas atualiza saldo).

### Passos

1. **Realizar Depósito**
   - Menu: **Transações**
   - Clicar: **Nova Transação**
   - Tipo: **Depósito**
   - Conta: Selecionar conta do João
   - Valor: 1000.00
   - Descrição: "Depósito inicial"
   - Clicar: **Salvar**
   - ✅ **Esperado:** Transação realizada com sucesso

2. **Verificar Saldo Atualizado**
   - Menu: **Contas**
   - ✅ **Esperado:** Saldo da conta = 1000.00

3. **Verificar Auditoria da Transação**
   - Menu: **Auditoria**
   - Filtrar: entityName="Transacao"
   - ✅ **Esperado:** Evento INSERT de Transação
   - Clicar no evento
   - ✅ **Esperado:** 
     - `newValues.valor`: 1000.00
     - `newValues.tipo`: "Depósito"
     - `sourceService`: "ms-transacoes"

4. **Verificar Auditoria da Atualização de Saldo**
   - Menu: **Auditoria**
   - Filtrar: entityName="Conta"
   - Buscar evento UPDATE mais recente
   - ✅ **Esperado:** Evento UPDATE de Conta
   - Clicar no evento
   - ✅ **Esperado:**
     - `oldValues.saldo`: 0
     - `newValues.saldo`: 1000.00
     - `sourceService`: "ms-contas"

### Verificação da Comunicação entre Serviços

```bash
# Ver eventos de ambos os serviços
curl -u admin:admin123 http://localhost:5001/api/v1/audit?sourceService=ms-transacoes | jq
curl -u admin:admin123 http://localhost:5001/api/v1/audit?sourceService=ms-contas | jq
```

---

## Cenário 5: Saque e Validação de Regra de Negócio

### Objetivo
Validar transação de saque e auditoria do UPDATE de saldo.

### Passos

1. **Realizar Saque**
   - Menu: **Transações**
   - Clicar: **Nova Transação**
   - Tipo: **Saque**
   - Conta: Conta do João
   - Valor: 300.00
   - Descrição: "Saque para despesas"
   - Clicar: **Salvar**
   - ✅ **Esperado:** Transação realizada

2. **Verificar Saldo**
   - Menu: **Contas**
   - ✅ **Esperado:** Saldo = 700.00

3. **Tentar Saque Acima do Saldo**
   - Menu: **Transações**
   - Tipo: **Saque**
   - Valor: 800.00 (maior que saldo)
   - ✅ **Esperado:** Erro "Saldo insuficiente"
   - ✅ **Esperado:** Nenhum evento de auditoria criado (transação não foi persistida)

---

## Cenário 6: Transferência entre Contas

### Objetivo
Validar transferência e auditoria de atualização de saldo em ambas as contas.

### Passos

1. **Criar Segunda Conta**
   - Criar usuário "Maria Silva"
   - Criar conta para Maria
   - Fazer depósito de 500.00 na conta da Maria

2. **Realizar Transferência**
   - Menu: **Transações**
   - Tipo: **Transferência**
   - Conta Origem: João (saldo 700)
   - Conta Destino: Maria (saldo 500)
   - Valor: 200.00
   - Clicar: **Salvar**
   - ✅ **Esperado:** Transferência realizada

3. **Verificar Saldos**
   - João: 500.00
   - Maria: 700.00

4. **Verificar Auditoria**
   - Menu: **Auditoria**
   - ✅ **Esperado:** 3 eventos:
     1. INSERT da Transação (tipo Transferência)
     2. UPDATE da Conta origem (João: 700 → 500)
     3. UPDATE da Conta destino (Maria: 500 → 700)

---

## Cenário 7: Exclusão de Usuário

### Objetivo
Validar evento DELETE e restrição de exclusão com dependências.

### Passos

1. **Tentar Excluir Usuário com Conta**
   - Menu: **Usuários**
   - Selecionar: João (que tem conta)
   - Clicar: **Excluir**
   - ✅ **Esperado:** Erro "Não é possível excluir usuário com contas associadas"

2. **Criar e Excluir Usuário sem Dependências**
   - Criar usuário "Teste Delete"
   - Não criar conta para ele
   - Excluir o usuário
   - ✅ **Esperado:** Usuário excluído

3. **Verificar Auditoria**
   - Menu: **Auditoria**
   - Filtrar: entityName="Usuario", operation="DELETE"
   - ✅ **Esperado:** Evento DELETE visível
   - Clicar no evento
   - ✅ **Esperado:** Diff mostra:
     - `oldValues`: {dados completos do usuário}
     - `newValues`: null
     - `operation`: "DELETE"

---

## Cenário 8: Filtros e Busca na Auditoria

### Objetivo
Validar funcionalidades de filtro e busca na interface de auditoria.

### Passos

1. **Filtrar por Período**
   - Menu: **Auditoria**
   - Definir: Data início = hoje
   - ✅ **Esperado:** Apenas eventos de hoje

2. **Filtrar por Operação**
   - Filtro: operation="INSERT"
   - ✅ **Esperado:** Apenas operações de inserção

3. **Filtrar por Entidade**
   - Filtro: entityName="Conta"
   - ✅ **Esperado:** Apenas eventos relacionados a contas

4. **Filtrar por Usuário**
   - Filtro: performedBy="admin"
   - ✅ **Esperado:** Apenas ações realizadas pelo admin

5. **Combinar Filtros**
   - entityName="Usuario" + operation="UPDATE"
   - ✅ **Esperado:** Apenas atualizações de usuários

---

## Verificações Técnicas

### RabbitMQ

```bash
# Verificar se a fila está sendo consumida
curl -u rabbitmq:rabbitmq123 http://localhost:15672/api/queues/%2F/audit-queue | jq

# Esperado:
# - consumers: 1 (MS-Auditoria consumindo)
# - messages: 0 ou baixo (mensagens sendo processadas rapidamente)
```

### Elasticsearch

```bash
# Ver índices criados
curl http://localhost:9200/_cat/indices?v
# Esperado: audit-ms-contas e audit-ms-transacoes

# Contar eventos por serviço
curl http://localhost:9200/audit-ms-contas/_count | jq
curl http://localhost:9200/audit-ms-transacoes/_count | jq

# Buscar eventos recentes
curl http://localhost:9200/audit-*/_search?sort=timestamp:desc&size=5 | jq
```

### Tempo de Propagação

```bash
# Medir tempo entre criação e disponibilidade na auditoria
# 1. Anotar hora da operação
# 2. Verificar quando aparece no Elasticsearch
# Esperado: < 5 segundos
```

---

## Critérios de Sucesso

Para considerar o teste E2E bem-sucedido, todos os itens devem ser atendidos:

- [ ] Todos os cenários passaram sem erros
- [ ] Eventos de INSERT, UPDATE e DELETE são capturados
- [ ] Diffs mostram valores anteriores e novos corretamente
- [ ] Eventos aparecem no Elasticsearch em < 5 segundos
- [ ] Filtros de auditoria funcionam corretamente
- [ ] Não há mensagens acumuladas no RabbitMQ
- [ ] Não há erros nos logs dos serviços
- [ ] Performance aceitável (sem lentidão perceptível)

---

## Troubleshooting Durante Testes

### Evento não aparece na auditoria

```bash
# 1. Verificar se chegou no RabbitMQ
curl -u rabbitmq:rabbitmq123 http://localhost:15672/api/queues

# 2. Ver logs do MS-Auditoria
docker-compose logs ms-auditoria | grep ERROR

# 3. Verificar diretamente no Elasticsearch
curl http://localhost:9200/audit-*/_search?q=*&sort=timestamp:desc
```

### Diff não mostra valores corretos

- Verificar logs do serviço que gerou o evento
- Confirmar que o interceptor está capturando os valores corretamente

### Performance lenta

```bash
# Verificar uso de recursos
docker stats

# Verificar logs para timeouts
docker-compose logs | grep -i timeout
```

---

## Relatório de Teste

Após completar todos os cenários, documente:

- ✅ Cenários que passaram
- ❌ Cenários que falharam (com detalhes do erro)
- ⏱️ Tempo médio de propagação dos eventos
- 📊 Quantidade de eventos gerados
- 🐛 Bugs encontrados
- 💡 Melhorias sugeridas
