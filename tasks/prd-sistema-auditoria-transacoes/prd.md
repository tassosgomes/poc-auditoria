# PRD - Sistema de Auditoria de Transações Bancárias (POC)

## Visão Geral

Este projeto é uma **Prova de Conceito (POC)** para validar uma arquitetura de auditoria transparente em sistemas distribuídos. O foco principal é implementar um mecanismo de auditoria que capture automaticamente todas as operações de **INSERT**, **UPDATE** e **DELETE** no banco de dados, registrando **quem**, **quando** e **o quê** foi alterado (incluindo valores anteriores e novos).

O sistema utiliza um domínio simplificado de transações bancárias como cenário de teste, mas o verdadeiro valor está na arquitetura de auditoria que pode ser replicada em qualquer sistema.

### Arquitetura de Alto Nível

```
┌─────────────┐     ┌─────────────────────────────────┐     ┌─────────────────┐
│   Frontend  │────▶│  MS Contas (Java/Spring)        │────▶│   PostgreSQL    │
│   (React)   │     │  + Hibernate Event Listeners    │     │                 │
└─────────────┘     └───────────────┬─────────────────┘     └─────────────────┘
      │                             │
      │                             │ Eventos de Auditoria
      │                             ▼
      │             ┌─────────────────────────────────┐     ┌─────────────────┐
      └────────────▶│  MS Transações (.NET 8)         │────▶│   PostgreSQL    │
                    │  + EF Core Interceptors         │     │                 │
                    └───────────────┬─────────────────┘     └─────────────────┘
                                    │
                                    │ Eventos de Auditoria
                                    ▼
                            ┌─────────────────┐
                            │   RabbitMQ      │
                            └────────┬────────┘
                                     │
                            ┌────────▼────────┐
                            │  MS Auditoria   │
                            │  (.NET 8)       │
                            └────────┬────────┘
                                     │
                            ┌────────▼────────┐
                            │  Elasticsearch  │
                            └─────────────────┘
```

## Objetivos

### Objetivo Principal
Validar a viabilidade de uma arquitetura de auditoria transparente que:
- Capture eventos de forma automática no banco de dados (sem alteração no código de negócio)
- Processe eventos de forma assíncrona via mensageria
- Armazene e permita consulta eficiente dos logs de auditoria

### Métricas de Sucesso
1. **Completude**: 100% das operações de INSERT/UPDATE/DELETE são auditadas
2. **Rastreabilidade**: Cada registro de auditoria contém: usuário, timestamp, operação, valores anteriores e novos
3. **Transparência**: Nenhuma alteração no código de negócio para adicionar auditoria
4. **Consulta**: Logs de auditoria consultáveis via interface web

### Objetivos de Negócio
- Demonstrar arquitetura replicável para compliance e auditoria
- Validar stack tecnológica para futuros projetos
- Criar base de conhecimento para implementações em produção

## Histórias de Usuário

### Persona: Auditor
- **HU-01**: Como auditor, quero visualizar o histórico completo de alterações em uma conta para rastrear todas as modificações realizadas.
- **HU-02**: Como auditor, quero filtrar eventos por período, usuário ou tipo de operação para encontrar rapidamente informações específicas.
- **HU-03**: Como auditor, quero ver os valores anteriores e novos de cada campo alterado para entender exatamente o que mudou.

### Persona: Usuário do Sistema Bancário
- **HU-04**: Como usuário, quero criar uma conta bancária para realizar transações.
- **HU-05**: Como usuário, quero realizar depósitos em minha conta para aumentar meu saldo.
- **HU-06**: Como usuário, quero realizar saques da minha conta para obter dinheiro.
- **HU-07**: Como usuário, quero transferir valores entre contas para movimentar meu dinheiro.
- **HU-08**: Como usuário, quero consultar meu saldo e extrato para acompanhar minhas finanças.

### Persona: Administrador
- **HU-09**: Como administrador, quero gerenciar usuários do sistema para controlar o acesso.
- **HU-10**: Como administrador, quero visualizar todas as transações realizadas para monitorar o sistema.

## Funcionalidades Principais

### F1. Microserviço de Contas (Java 21 / Spring Boot 3.x)

**O que faz**: Gerencia usuários e contas bancárias, com auditoria automática via Hibernate Event Listeners.

**Por que é importante**: Fornece as entidades base que serão auditadas e demonstra integração Java com a arquitetura de auditoria.

**Requisitos Funcionais**:
- **RF-01**: Criar, atualizar, listar e excluir usuários
- **RF-02**: Criar, atualizar, listar e excluir contas bancárias
- **RF-03**: Associar contas a usuários
- **RF-04**: Expor API REST documentada com Swagger/OpenAPI
- **RF-05**: Autenticar requisições via credenciais hardcoded
- **RF-06**: Implementar Hibernate Event Listeners para capturar INSERT, UPDATE, DELETE
- **RF-07**: Capturar valores anteriores e novos das entidades auditadas
- **RF-08**: Publicar eventos de auditoria no RabbitMQ de forma assíncrona

### F2. Microserviço de Transações (.NET 8)

**O que faz**: Gerencia operações financeiras (depósito, saque, transferência), com auditoria automática via EF Core Interceptors.

**Por que é importante**: Demonstra integração .NET com a arquitetura e gera os eventos mais críticos para auditoria.

**Requisitos Funcionais**:
- **RF-09**: Realizar depósito em conta (atualiza saldo)
- **RF-10**: Realizar saque de conta (atualiza saldo com validação)
- **RF-11**: Realizar transferência entre contas (débito + crédito atômico)
- **RF-12**: Listar transações por conta
- **RF-13**: Expor API REST documentada com Swagger/OpenAPI
- **RF-14**: Autenticar requisições via credenciais hardcoded
- **RF-15**: Implementar EF Core SaveChangesInterceptor para capturar INSERT, UPDATE, DELETE
- **RF-16**: Utilizar ChangeTracker para obter valores originais e atuais das entidades
- **RF-17**: Publicar eventos de auditoria no RabbitMQ de forma assíncrona

### F3. Mecanismo de Auditoria na Camada de Aplicação

**O que faz**: Captura automaticamente todas as alterações nas entidades via interceptors/listeners.

**Por que é importante**: Este é o **core** da POC - auditoria transparente na camada de aplicação, sem poluir o código de negócio.

**Arquitetura de Interceptors**:

| Tecnologia | Mecanismo | Componente |
|------------|-----------|------------|
| Java/Spring | Hibernate Event Listeners | `PreInsertEventListener`, `PreUpdateEventListener`, `PreDeleteEventListener` |
| .NET 8 | EF Core Interceptors | `SaveChangesInterceptor` + `ChangeTracker` |

**Requisitos Funcionais**:
- **RF-18**: Interceptar operações de persistência antes/depois do commit
- **RF-19**: Capturar metadados: usuário logado, timestamp, nome da entidade, operação
- **RF-20**: Capturar dados anteriores (para UPDATE e DELETE)
- **RF-21**: Capturar dados novos (para INSERT e UPDATE)
- **RF-22**: Serializar entidades auditadas em JSON
- **RF-23**: Não bloquear a operação principal em caso de falha na auditoria (fire-and-forget com garantia de entrega)

### F4. Publicação de Eventos no RabbitMQ

**O que faz**: Publica eventos de auditoria capturados pelos interceptors na fila do RabbitMQ.

**Por que é importante**: Desacopla a captura do evento do processamento, garantindo resiliência.

**Requisitos Funcionais**:
- **RF-24**: Publicar evento de auditoria na fila do RabbitMQ
- **RF-25**: Garantir entrega da mensagem (confirmação de publicação)
- **RF-26**: Implementar padrão Outbox para garantia de entrega (opcional para POC)

### F5. Microserviço de Auditoria (.NET 8)

**O que faz**: Consome eventos da fila e persiste no Elasticsearch.

**Por que é importante**: Centraliza e indexa os logs de auditoria para consulta eficiente.

**Requisitos Funcionais**:
- **RF-27**: Consumir mensagens da fila de auditoria do RabbitMQ
- **RF-28**: Persistir eventos no Elasticsearch com índice apropriado
- **RF-29**: Expor API REST para consulta de eventos de auditoria
- **RF-30**: Permitir filtros por: período, usuário, tabela, tipo de operação, registro_id
- **RF-31**: Expor API documentada com Swagger/OpenAPI

### F6. Frontend (React)

**O que faz**: Interface web para operações bancárias e visualização de auditoria.

**Por que é importante**: Permite validar o fluxo completo end-to-end e visualizar os resultados.

**Requisitos Funcionais**:
- **RF-32**: Tela de login com credenciais hardcoded
- **RF-33**: Dashboard com resumo de contas e transações
- **RF-34**: CRUD de usuários
- **RF-35**: CRUD de contas
- **RF-36**: Tela de operações: depósito, saque, transferência
- **RF-37**: Tela de extrato por conta
- **RF-38**: **Tela de auditoria**: listagem de eventos com filtros
- **RF-39**: **Detalhe de auditoria**: visualização de diff (valor anterior vs novo)

### F7. Infraestrutura (Docker Compose)

**O que faz**: Orquestra todos os serviços para execução local.

**Por que é importante**: Permite executar a POC completa com um único comando.

**Requisitos Funcionais**:
- **RF-40**: Definir serviços: PostgreSQL, RabbitMQ, Elasticsearch
- **RF-41**: Definir serviços: ms-contas, ms-transacoes, ms-auditoria
- **RF-42**: Definir serviço: frontend
- **RF-43**: Configurar rede interna para comunicação entre serviços
- **RF-44**: Configurar volumes para persistência de dados
- **RF-45**: Expor portas necessárias para acesso externo
- **RF-46**: Incluir script de inicialização do banco de dados (schema)

## Experiência do Usuário

### Fluxo Principal - Operação Bancária com Auditoria

1. Usuário faz login no frontend
2. Usuário navega até "Transferência"
3. Usuário seleciona conta origem, conta destino e valor
4. Sistema executa transferência (MS Transações)
5. Trigger do PostgreSQL captura UPDATE nas contas
6. Evento é publicado no RabbitMQ
7. MS Auditoria consome e persiste no Elasticsearch
8. Usuário acessa tela de Auditoria e visualiza o evento

### Fluxo de Consulta de Auditoria

1. Auditor acessa tela de Auditoria
2. Aplica filtros (data, usuário, tabela)
3. Visualiza lista de eventos
4. Clica em um evento para ver detalhes
5. Visualiza diff: campos alterados com valores anteriores e novos

### Requisitos de UI/UX

- Interface simples e funcional (foco na POC, não em design elaborado)
- Feedback visual para operações (loading, sucesso, erro)
- Diff de auditoria com destaque visual (verde=novo, vermelho=removido)

### Acessibilidade

- Contraste adequado para leitura
- Labels em formulários
- Navegação por teclado básica

## Restrições Técnicas de Alto Nível

### Stack Tecnológica (Obrigatória)

| Componente | Tecnologia |
|------------|------------|
| MS Contas | Java 21, Spring Boot 3.x |
| MS Transações | .NET 8 |
| MS Auditoria | .NET 8 |
| Frontend | React |
| Banco de Dados | PostgreSQL |
| Mensageria | RabbitMQ |
| Search/Analytics | Elasticsearch |
| Orquestração | Docker Compose |

### Integrações

- **Java → RabbitMQ**: Hibernate Event Listeners publica eventos via Spring AMQP
- **NET → RabbitMQ**: EF Core Interceptors publica eventos via MassTransit ou RabbitMQ.Client
- RabbitMQ → Elasticsearch: via MS Auditoria
- Frontend → Microserviços: via REST API

### Segurança

- Autenticação simplificada (hardcoded) - adequada para POC
- Credenciais de exemplo:
  - `admin / admin123`
  - `user / user123`

### Documentação

- APIs documentadas com Swagger/OpenAPI
- README com instruções de execução

## Não-Objetivos (Fora de Escopo)

### Explicitamente Excluídos

- ❌ Testes automatizados (unitários, integração, e2e)
- ❌ Autenticação robusta (OAuth, JWT com expiração, refresh token)
- ❌ Política de retenção de dados de auditoria
- ❌ Alta disponibilidade ou escalabilidade
- ❌ Monitoramento e observabilidade (APM, métricas)
- ❌ CI/CD pipeline
- ❌ Tratamento avançado de erros e retry
- ❌ Criptografia de dados sensíveis
- ❌ Multi-tenancy

### Considerações Futuras (Pós-POC)

- Implementação de testes automatizados
- Autenticação via Identity Provider
- Política de retenção e arquivamento
- Kubernetes para orquestração em produção
- Observabilidade com stack ELK ou similar

## Questões em Aberto

1. **Biblioteca RabbitMQ .NET**: Usar MassTransit ou RabbitMQ.Client diretamente?

2. **Schema do banco**: As três tabelas (usuários, contas, transações) estarão no mesmo banco de dados?

3. **Kibana**: Devemos incluir Kibana no docker-compose para visualização alternativa no Elasticsearch?

4. **Credenciais hardcoded**: Devem ser as mesmas para todos os microserviços ou diferentes por serviço?

5. **Formato do diff**: O diff deve ser calculado no MS Auditoria antes de salvar ou deve ser feito no frontend na hora de exibir?

6. **Transações atômicas**: No caso de falha ao publicar no RabbitMQ, a operação de negócio deve ser revertida ou apenas logar o erro?

---

## Anexo: Modelo de Dados de Auditoria

```json
{
  "id": "uuid",
  "timestamp": "2025-12-16T10:30:00Z",
  "operation": "UPDATE",
  "entity_name": "Conta",
  "entity_id": "123",
  "user_id": "admin",
  "old_values": {
    "saldo": 1000.00
  },
  "new_values": {
    "saldo": 1500.00
  },
  "changed_fields": ["saldo"],
  "source_service": "ms-transacoes",
  "correlation_id": "uuid-correlacao"
}
```

---

## Anexo: Exemplo de Interceptors

### .NET - EF Core SaveChangesInterceptor

```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken)
    {
        var context = eventData.Context;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                var oldValues = entry.OriginalValues.ToObject();
                var newValues = entry.CurrentValues.ToObject();
                // Publicar evento de auditoria
            }
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

### Java - Hibernate Event Listener

```java
@Component
public class AuditEventListener implements PreUpdateEventListener, PreInsertEventListener {
    
    @Override
    public boolean onPreUpdate(PreUpdateEvent event) {
        Object[] oldState = event.getOldState();
        Object[] newState = event.getState();
        String[] propertyNames = event.getPersister().getPropertyNames();
        // Publicar evento de auditoria
        return false;
    }
}
```

---

**Documento criado em**: 16 de Dezembro de 2025  
**Versão**: 1.1  
**Status**: Aguardando Aprovação
