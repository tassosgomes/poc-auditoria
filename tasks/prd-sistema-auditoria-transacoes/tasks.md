# Resumo de Tarefas de Implementação - Sistema de Auditoria de Transações Bancárias

## Visão Geral

Este documento lista todas as tarefas necessárias para implementar a POC de auditoria de transações bancárias, conforme definido no PRD e Tech Spec.

**Estimativa Total:** 10-11 dias  
**Quantidade de Tarefas:** 7 tarefas principais, 45 subtarefas

---

## Diagrama de Dependências

```
┌─────────────────┐
│  1.0 Infra      │ (Fase 1 - Bloqueadora)
│  Docker Compose │
└────────┬────────┘
         │
         ▼
┌────────┴────────┬─────────────────┐
│                 │                 │
▼                 ▼                 ▼
┌─────────┐  ┌─────────┐     ┌─────────────┐
│ 2.0     │  │ 3.0     │     │ 4.0         │
│MS-Contas│  │MS-Trans.│     │ MS-Auditoria│
│ (Java)  │  │ (.NET)  │     │  (.NET)     │
└────┬────┘  └────┬────┘     └──────┬──────┘
     │            │                 │
     │   ┌────────┘                 │
     │   │ (REST)                   │
     ▼   ▼                          │
┌────────────────┐                  │
│ 3.0 depende de │◄─────────────────┘
│ 2.0 para API   │
└───────┬────────┘
        │
        ▼
┌───────────────────────────────────┐
│         5.0 Frontend (React)      │
│     (depende de todas as APIs)    │
└───────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────┐
│   6.0 Integração & Documentação   │
└───────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────┐
│  7.0 Persistência Local Auditoria │
│  (tabela audit_log por schema)    │
└───────────────────────────────────┘
```

---

## Análise de Paralelização

### Trilhas Paralelas

| Trilha | Tarefas | Desenvolvedor |
|--------|---------|---------------|
| **Trilha A** | 1.0 → 2.0 (Java) | Dev Java |
| **Trilha B** | 1.0 → 3.0 + 4.0 (.NET) | Dev .NET |
| **Trilha C** | 5.0 (Frontend) - após APIs | Dev Frontend |

### Caminho Crítico

```
1.0 → 2.0 → 3.0 → 5.0 → 6.0
```

O MS-Transações (3.0) depende do MS-Contas (2.0) estar pronto para integração REST.

---

## Tarefas

- [x] 1.0 Infraestrutura Base (Docker Compose) ✅ CONCLUÍDA
- [ ] 2.0 MS-Contas (Java/Spring Boot)
- [ ] 3.0 MS-Transações (.NET 8)
- [ ] 4.0 MS-Auditoria (.NET 8)
- [ ] 5.0 Frontend (React + Tailwind)
- [ ] 6.0 Integração Final e Documentação
- [ ] 7.0 Persistência Local de Auditoria (tabela audit_log por schema)

---

## Detalhamento por Fase

### Fase 1: Infraestrutura (1 dia)
| Tarefa | Descrição | Paralelizável |
|--------|-----------|---------------|
| 1.0 | Docker Compose + Scripts SQL | ❌ Bloqueadora |

### Fase 2: Backend (4-5 dias)
| Tarefa | Descrição | Paralelizável |
|--------|-----------|---------------|
| 2.0 | MS-Contas (Java) | ✅ Sim (após 1.0) |
| 3.0 | MS-Transações (.NET) | ⚠️ Parcial (depende de 2.0 para REST) |
| 4.0 | MS-Auditoria (.NET) | ✅ Sim (após 1.0) |

### Fase 3: Frontend (2 dias)
| Tarefa | Descrição | Paralelizável |
|--------|-----------|---------------|
| 5.0 | Frontend React | ⚠️ Parcial (depende de APIs) |

### Fase 4: Integração (1 dia)
| Tarefa | Descrição | Paralelizável |
|--------|-----------|---------------|
| 6.0 | Docker final + README | ❌ Final |

### Fase 5: Persistência Local (1 dia)
| Tarefa | Descrição | Paralelizável |
|--------|-----------|---------------|
| 7.0 | Tabela audit_log + modificação interceptores | ⚠️ Após 2.0 e 3.0 |

---

## Arquivos de Tarefas

- [1_task.md](1_task.md) - Infraestrutura Base
- [2_task.md](2_task.md) - MS-Contas (Java)
- [3_task.md](3_task.md) - MS-Transações (.NET)
- [4_task.md](4_task.md) - MS-Auditoria (.NET)
- [5_task.md](5_task.md) - Frontend (React)
- [6_task.md](6_task.md) - Integração Final
- [7_task.md](7_task.md) - Persistência Local de Auditoria

---

**Documento gerado em**: 16 de Dezembro de 2025
