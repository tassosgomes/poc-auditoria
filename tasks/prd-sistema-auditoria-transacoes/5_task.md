---
status: pending
parallelizable: partial
blocked_by: ["2.0", "3.0", "4.0"]
---

<task_context>
<domain>frontend/react</domain>
<type>implementation</type>
<scope>user_interface</scope>
<complexity>medium</complexity>
<dependencies>ms-contas-api, ms-transacoes-api, ms-auditoria-api</dependencies>
<unblocks>6.0</unblocks>
</task_context>

# Tarefa 5.0: Frontend (React + Tailwind)

## Visão Geral

Desenvolver a interface web em React com Tailwind CSS para operações bancárias e visualização de auditoria. O frontend se comunica com os três microserviços via REST API e inclui uma tela dedicada para visualização de diff de auditoria.

<requirements>
- Node.js 18+ instalado
- APIs dos microserviços disponíveis (Tarefas 2.0, 3.0, 4.0)
- Conhecimento de React, Tailwind CSS, TypeScript
</requirements>

## Subtarefas

- [ ] 5.1 Setup projeto Vite + React + TypeScript
- [ ] 5.2 Configurar Tailwind CSS
- [ ] 5.3 Criar estrutura de pastas e componentes base
- [ ] 5.4 Implementar AuthContext e tela de Login
- [ ] 5.5 Criar Layout principal (Sidebar, Header)
- [ ] 5.6 Implementar serviços de API (api.ts, contasApi.ts, etc.)
- [ ] 5.7 Criar página de Dashboard
- [ ] 5.8 Criar CRUD de Usuários
- [ ] 5.9 Criar CRUD de Contas
- [ ] 5.10 Criar tela de Transações (depósito, saque, transferência)
- [ ] 5.11 Criar tela de Auditoria com filtros
- [ ] 5.12 Criar componente AuditDiff (visualização de diff)
- [ ] 5.13 Criar Dockerfile
- [ ] 5.14 Testar fluxo completo E2E

## Sequenciamento

- **Bloqueado por:** 2.0, 3.0, 4.0 (precisa das APIs)
- **Desbloqueia:** 6.0 (Integração Final)
- **Paralelizável:** Parcialmente (estrutura pode ser criada antes, mas testes precisam das APIs)

## Detalhes de Implementação

### 5.1 Setup Projeto

```bash
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
```

### 5.2 Dependências

```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.20.0",
    "axios": "^1.6.0",
    "@tanstack/react-query": "^5.0.0"
  },
  "devDependencies": {
    "@types/react": "^18.2.0",
    "typescript": "^5.3.0",
    "tailwindcss": "^3.4.0",
    "autoprefixer": "^10.4.0",
    "postcss": "^8.4.0",
    "vite": "^5.0.0"
  }
}
```

### Estrutura de Pastas

```
frontend/
├── package.json
├── Dockerfile
├── tailwind.config.js
├── vite.config.ts
├── index.html
└── src/
    ├── main.tsx
    ├── App.tsx
    ├── index.css
    ├── components/
    │   ├── Layout/
    │   │   ├── Sidebar.tsx
    │   │   ├── Header.tsx
    │   │   └── MainLayout.tsx
    │   ├── Auth/
    │   │   └── ProtectedRoute.tsx
    │   ├── Audit/
    │   │   ├── AuditList.tsx
    │   │   ├── AuditFilters.tsx
    │   │   └── AuditDiff.tsx
    │   └── UI/
    │       ├── Button.tsx
    │       ├── Input.tsx
    │       ├── Card.tsx
    │       ├── Table.tsx
    │       └── Modal.tsx
    ├── pages/
    │   ├── Login.tsx
    │   ├── Dashboard.tsx
    │   ├── Usuarios.tsx
    │   ├── Contas.tsx
    │   ├── Transacoes.tsx
    │   └── Auditoria.tsx
    ├── services/
    │   ├── api.ts
    │   ├── authService.ts
    │   ├── contasApi.ts
    │   ├── transacoesApi.ts
    │   └── auditoriaApi.ts
    ├── contexts/
    │   └── AuthContext.tsx
    ├── hooks/
    │   └── useAuth.ts
    └── types/
        └── index.ts
```

### 5.4 AuthContext

```tsx
// src/contexts/AuthContext.tsx
import { createContext, useContext, useState, ReactNode } from 'react';

interface User {
  username: string;
}

interface AuthContextType {
  user: User | null;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  isAuthenticated: boolean;
  getAuthHeader: () => string;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const VALID_CREDENTIALS: Record<string, string> = {
  admin: 'admin123',
  user: 'user123',
};

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => {
    const saved = localStorage.getItem('user');
    return saved ? JSON.parse(saved) : null;
  });

  const login = async (username: string, password: string): Promise<boolean> => {
    if (VALID_CREDENTIALS[username] === password) {
      const userData = { username };
      setUser(userData);
      localStorage.setItem('user', JSON.stringify(userData));
      localStorage.setItem('credentials', btoa(`${username}:${password}`));
      return true;
    }
    return false;
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem('user');
    localStorage.removeItem('credentials');
  };

  const getAuthHeader = () => {
    const credentials = localStorage.getItem('credentials');
    return credentials ? `Basic ${credentials}` : '';
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        login,
        logout,
        isAuthenticated: !!user,
        getAuthHeader,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
};
```

### 5.6 Serviço de API Base

```tsx
// src/services/api.ts
import axios from 'axios';

const API_CONTAS = import.meta.env.VITE_API_CONTAS || 'http://localhost:8080';
const API_TRANSACOES = import.meta.env.VITE_API_TRANSACOES || 'http://localhost:5000';
const API_AUDITORIA = import.meta.env.VITE_API_AUDITORIA || 'http://localhost:5001';

const getAuthHeader = () => {
  const credentials = localStorage.getItem('credentials');
  return credentials ? `Basic ${credentials}` : '';
};

export const contasApi = axios.create({
  baseURL: `${API_CONTAS}/api/v1`,
});

export const transacoesApi = axios.create({
  baseURL: `${API_TRANSACOES}/api/v1`,
});

export const auditoriaApi = axios.create({
  baseURL: `${API_AUDITORIA}/api/v1`,
});

// Interceptor para adicionar auth header
[contasApi, transacoesApi, auditoriaApi].forEach((api) => {
  api.interceptors.request.use((config) => {
    config.headers.Authorization = getAuthHeader();
    return config;
  });
});
```

### 5.6 Serviços Específicos

```tsx
// src/services/contasApi.ts
import { contasApi } from './api';
import { Usuario, Conta } from '../types';

export const usuariosService = {
  listar: () => contasApi.get<Usuario[]>('/usuarios'),
  buscar: (id: string) => contasApi.get<Usuario>(`/usuarios/${id}`),
  criar: (data: Partial<Usuario>) => contasApi.post<Usuario>('/usuarios', data),
  atualizar: (id: string, data: Partial<Usuario>) => 
    contasApi.put<Usuario>(`/usuarios/${id}`, data),
  excluir: (id: string) => contasApi.delete(`/usuarios/${id}`),
};

export const contasService = {
  listar: () => contasApi.get<Conta[]>('/contas'),
  buscar: (id: string) => contasApi.get<Conta>(`/contas/${id}`),
  criar: (data: Partial<Conta>) => contasApi.post<Conta>('/contas', data),
  atualizar: (id: string, data: Partial<Conta>) => 
    contasApi.put<Conta>(`/contas/${id}`, data),
  excluir: (id: string) => contasApi.delete(`/contas/${id}`),
};
```

```tsx
// src/services/transacoesApi.ts
import { transacoesApi } from './api';
import { Transacao, DepositoRequest, SaqueRequest, TransferenciaRequest } from '../types';

export const transacoesService = {
  deposito: (data: DepositoRequest) => 
    transacoesApi.post<Transacao>('/transacoes/deposito', data),
  saque: (data: SaqueRequest) => 
    transacoesApi.post<Transacao>('/transacoes/saque', data),
  transferencia: (data: TransferenciaRequest) => 
    transacoesApi.post<Transacao>('/transacoes/transferencia', data),
  listarPorConta: (contaId: string) => 
    transacoesApi.get<Transacao[]>(`/transacoes/conta/${contaId}`),
};
```

```tsx
// src/services/auditoriaApi.ts
import { auditoriaApi } from './api';
import { AuditEvent, AuditQueryParams } from '../types';

export const auditoriaService = {
  listar: (params?: AuditQueryParams) => 
    auditoriaApi.get<AuditEvent[]>('/audit', { params }),
  buscar: (id: string) => 
    auditoriaApi.get<AuditEvent>(`/audit/${id}`),
  porEntidade: (entityName: string, entityId: string) => 
    auditoriaApi.get<AuditEvent[]>(`/audit/entity/${entityName}/${entityId}`),
  porUsuario: (userId: string) => 
    auditoriaApi.get<AuditEvent[]>(`/audit/user/${userId}`),
};
```

### 5.11 Página de Auditoria

```tsx
// src/pages/Auditoria.tsx
import { useState, useEffect } from 'react';
import { auditoriaService } from '../services/auditoriaApi';
import { AuditList } from '../components/Audit/AuditList';
import { AuditFilters } from '../components/Audit/AuditFilters';
import { AuditDiff } from '../components/Audit/AuditDiff';
import { AuditEvent, AuditQueryParams } from '../types';

export function Auditoria() {
  const [events, setEvents] = useState<AuditEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedEvent, setSelectedEvent] = useState<AuditEvent | null>(null);
  const [filters, setFilters] = useState<AuditQueryParams>({});

  useEffect(() => {
    loadEvents();
  }, [filters]);

  const loadEvents = async () => {
    setLoading(true);
    try {
      const response = await auditoriaService.listar(filters);
      setEvents(response.data);
    } catch (error) {
      console.error('Erro ao carregar eventos:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-6">Auditoria</h1>
      
      <AuditFilters filters={filters} onChange={setFilters} />
      
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
        <div className="bg-white rounded-lg shadow">
          <div className="p-4 border-b">
            <h2 className="text-lg font-semibold">Eventos</h2>
          </div>
          <AuditList 
            events={events} 
            loading={loading}
            onSelect={setSelectedEvent}
            selectedId={selectedEvent?.id}
          />
        </div>
        
        <div className="bg-white rounded-lg shadow">
          <div className="p-4 border-b">
            <h2 className="text-lg font-semibold">Detalhes</h2>
          </div>
          {selectedEvent ? (
            <AuditDiff event={selectedEvent} />
          ) : (
            <div className="p-8 text-center text-gray-500">
              Selecione um evento para ver os detalhes
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
```

### 5.12 Componente AuditDiff

```tsx
// src/components/Audit/AuditDiff.tsx
import { AuditEvent } from '../../types';

interface AuditDiffProps {
  event: AuditEvent;
}

export function AuditDiff({ event }: AuditDiffProps) {
  const getChangedFields = () => {
    if (!event.oldValues && !event.newValues) return [];
    
    const allKeys = new Set([
      ...Object.keys(event.oldValues || {}),
      ...Object.keys(event.newValues || {}),
    ]);
    
    return Array.from(allKeys).map((key) => ({
      field: key,
      oldValue: event.oldValues?.[key],
      newValue: event.newValues?.[key],
      changed: JSON.stringify(event.oldValues?.[key]) !== 
               JSON.stringify(event.newValues?.[key]),
    }));
  };

  const formatValue = (value: unknown) => {
    if (value === null || value === undefined) return '-';
    if (typeof value === 'object') return JSON.stringify(value, null, 2);
    return String(value);
  };

  const fields = getChangedFields();

  return (
    <div className="p-4">
      {/* Metadados */}
      <div className="mb-6 space-y-2">
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">ID:</span>
          <span className="font-mono">{event.id}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Data:</span>
          <span>{new Date(event.timestamp).toLocaleString('pt-BR')}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Operação:</span>
          <span className={`px-2 py-0.5 rounded text-xs font-medium ${
            event.operation === 'INSERT' ? 'bg-green-100 text-green-800' :
            event.operation === 'UPDATE' ? 'bg-yellow-100 text-yellow-800' :
            'bg-red-100 text-red-800'
          }`}>
            {event.operation}
          </span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Entidade:</span>
          <span>{event.entityName}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">ID Entidade:</span>
          <span className="font-mono text-xs">{event.entityId}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Usuário:</span>
          <span>{event.userId}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Serviço:</span>
          <span>{event.sourceService}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Correlation ID:</span>
          <span className="font-mono text-xs">{event.correlationId}</span>
        </div>
      </div>

      {/* Diff de campos */}
      <div className="border-t pt-4">
        <h3 className="font-semibold mb-3">Alterações</h3>
        <div className="space-y-3">
          {fields.map(({ field, oldValue, newValue, changed }) => (
            <div 
              key={field} 
              className={`p-3 rounded ${changed ? 'bg-yellow-50' : 'bg-gray-50'}`}
            >
              <div className="font-medium text-sm mb-2">{field}</div>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <div className="text-xs text-gray-500 mb-1">Anterior</div>
                  <div className={`p-2 rounded font-mono text-xs ${
                    event.operation === 'INSERT' ? 'bg-gray-100' :
                    changed ? 'bg-red-100 text-red-800' : 'bg-gray-100'
                  }`}>
                    <pre className="whitespace-pre-wrap break-all">
                      {formatValue(oldValue)}
                    </pre>
                  </div>
                </div>
                <div>
                  <div className="text-xs text-gray-500 mb-1">Novo</div>
                  <div className={`p-2 rounded font-mono text-xs ${
                    event.operation === 'DELETE' ? 'bg-gray-100' :
                    changed ? 'bg-green-100 text-green-800' : 'bg-gray-100'
                  }`}>
                    <pre className="whitespace-pre-wrap break-all">
                      {formatValue(newValue)}
                    </pre>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
```

### 5.10 Página de Transações

```tsx
// src/pages/Transacoes.tsx
import { useState } from 'react';
import { transacoesService } from '../services/transacoesApi';
import { contasService } from '../services/contasApi';
import { Conta } from '../types';

type TipoOperacao = 'deposito' | 'saque' | 'transferencia';

export function Transacoes() {
  const [tipo, setTipo] = useState<TipoOperacao>('deposito');
  const [contas, setContas] = useState<Conta[]>([]);
  const [loading, setLoading] = useState(false);
  const [mensagem, setMensagem] = useState<{ tipo: 'success' | 'error'; texto: string } | null>(null);

  const [form, setForm] = useState({
    contaOrigemId: '',
    contaDestinoId: '',
    valor: '',
    descricao: '',
  });

  useEffect(() => {
    contasService.listar().then(res => setContas(res.data));
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setMensagem(null);

    try {
      const valor = parseFloat(form.valor);
      
      if (tipo === 'deposito') {
        await transacoesService.deposito({
          contaId: form.contaOrigemId,
          valor,
          descricao: form.descricao,
        });
      } else if (tipo === 'saque') {
        await transacoesService.saque({
          contaId: form.contaOrigemId,
          valor,
          descricao: form.descricao,
        });
      } else {
        await transacoesService.transferencia({
          contaOrigemId: form.contaOrigemId,
          contaDestinoId: form.contaDestinoId,
          valor,
          descricao: form.descricao,
        });
      }

      setMensagem({ tipo: 'success', texto: 'Operação realizada com sucesso!' });
      setForm({ contaOrigemId: '', contaDestinoId: '', valor: '', descricao: '' });
    } catch (error: any) {
      setMensagem({ 
        tipo: 'error', 
        texto: error.response?.data?.message || 'Erro ao realizar operação' 
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-6">Transações</h1>

      {/* Tabs */}
      <div className="flex space-x-4 mb-6">
        {(['deposito', 'saque', 'transferencia'] as TipoOperacao[]).map((t) => (
          <button
            key={t}
            onClick={() => setTipo(t)}
            className={`px-4 py-2 rounded-lg font-medium ${
              tipo === t
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            {t.charAt(0).toUpperCase() + t.slice(1)}
          </button>
        ))}
      </div>

      {/* Formulário */}
      <div className="bg-white rounded-lg shadow p-6 max-w-md">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">
              {tipo === 'transferencia' ? 'Conta Origem' : 'Conta'}
            </label>
            <select
              value={form.contaOrigemId}
              onChange={(e) => setForm({ ...form, contaOrigemId: e.target.value })}
              className="w-full border rounded-lg p-2"
              required
            >
              <option value="">Selecione...</option>
              {contas.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.numeroConta} - Saldo: R$ {c.saldo.toFixed(2)}
                </option>
              ))}
            </select>
          </div>

          {tipo === 'transferencia' && (
            <div>
              <label className="block text-sm font-medium mb-1">Conta Destino</label>
              <select
                value={form.contaDestinoId}
                onChange={(e) => setForm({ ...form, contaDestinoId: e.target.value })}
                className="w-full border rounded-lg p-2"
                required
              >
                <option value="">Selecione...</option>
                {contas
                  .filter((c) => c.id !== form.contaOrigemId)
                  .map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.numeroConta}
                    </option>
                  ))}
              </select>
            </div>
          )}

          <div>
            <label className="block text-sm font-medium mb-1">Valor</label>
            <input
              type="number"
              step="0.01"
              min="0.01"
              value={form.valor}
              onChange={(e) => setForm({ ...form, valor: e.target.value })}
              className="w-full border rounded-lg p-2"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Descrição</label>
            <input
              type="text"
              value={form.descricao}
              onChange={(e) => setForm({ ...form, descricao: e.target.value })}
              className="w-full border rounded-lg p-2"
            />
          </div>

          {mensagem && (
            <div className={`p-3 rounded ${
              mensagem.tipo === 'success' 
                ? 'bg-green-100 text-green-800' 
                : 'bg-red-100 text-red-800'
            }`}>
              {mensagem.texto}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 text-white py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {loading ? 'Processando...' : 'Confirmar'}
          </button>
        </form>
      </div>
    </div>
  );
}
```

### 5.13 Dockerfile

```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
```

### nginx.conf

```nginx
server {
    listen 3000;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

## Páginas a Implementar

| Página | Rota | Descrição |
|--------|------|-----------|
| Login | `/login` | Autenticação com credenciais hardcoded |
| Dashboard | `/` | Resumo de contas e transações recentes |
| Usuários | `/usuarios` | CRUD de usuários |
| Contas | `/contas` | CRUD de contas bancárias |
| Transações | `/transacoes` | Depósito, saque, transferência |
| Auditoria | `/auditoria` | Listagem + filtros + diff |

## Critérios de Sucesso

- [ ] Login funcionando com credenciais admin/admin123 e user/user123
- [ ] Navegação entre páginas funcionando
- [ ] CRUD de usuários integrado com MS-Contas
- [ ] CRUD de contas integrado com MS-Contas
- [ ] Operações de transação integradas com MS-Transações
- [ ] Tela de auditoria mostrando eventos do Elasticsearch
- [ ] Filtros de auditoria funcionando
- [ ] Componente de diff mostrando valores antigos vs novos
- [ ] Feedback visual para loading e erros
- [ ] Container Docker buildando e executando

## Estimativa

**Tempo:** 2 dias (16 horas)

---

**Referências:**
- Tech Spec: Seção "Frontend (React)"
- PRD: RF-32 a RF-39
