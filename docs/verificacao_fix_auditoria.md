# Verificação da Correção de Auditoria (ms-contas)

Este documento descreve como verificar se a correção para o erro `ConcurrentModificationException` no `ms-contas` foi aplicada com sucesso.

## 1. Pré-requisitos

Certifique-se de que os containers estão rodando:

```bash
docker compose ps
```

O serviço `ms-contas` deve estar `Up` e `Healthy`.

## 2. Teste de Criação de Usuário

Execute o seguinte comando para criar um usuário. Isso irá disparar o `PostInsertEventListener`, que por sua vez tentará salvar um registro de auditoria.

```bash
curl -v -X POST http://localhost:8080/api/v1/usuarios \
-H "Content-Type: application/json" \
-H "Authorization: Basic YWRtaW46YWRtaW4xMjM=" \
-d '{"nome": "Teste Verificacao", "email": "verificacao@teste.com", "senha": "123"}'
```

**Resultado Esperado:**
- HTTP Status Code: `201 Created`
- Resposta JSON contendo o ID do usuário criado.

## 3. Verificação no Banco de Dados

Verifique se o registro de auditoria foi criado na tabela `contas.audit_log`.

```bash
docker exec poc-postgres psql -U postgres -d poc_auditoria -c "SELECT * FROM contas.audit_log WHERE entity_name = 'Usuario' ORDER BY created_at DESC LIMIT 1;"
```

**Resultado Esperado:**
- Uma linha retornada com `operation` = `INSERT` e `new_values` contendo os dados do usuário "Teste Verificacao".

## 4. Conclusão

Se o passo 2 retornar 201 e o passo 3 retornar o registro, a correção está funcionando. O uso de `JdbcTemplate` com `REQUIRES_NEW` isolou a persistência da auditoria do processo de flush do Hibernate.
