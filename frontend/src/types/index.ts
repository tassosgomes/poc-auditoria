export interface Usuario {
  id: string;
  nome: string;
  email: string;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
}

export interface UsuarioCreateRequest {
  nome: string;
  email: string;
  senha: string;
}

export interface UsuarioUpdateRequest {
  nome: string;
  email: string;
}

export interface Conta {
  id: string;
  numeroConta: string;
  saldo: number;
}

export interface Transacao {
  id: string;
  contaOrigemId: string;
  contaDestinoId?: string;
  valor: number;
  descricao?: string;
  criadoEm: string;
}

export interface AuditEvent {
  id: string;
  timestamp: string;
  operation: 'INSERT' | 'UPDATE' | 'DELETE';
  entityName: string;
  entityId: string;
  userId?: string;
  oldValues?: Record<string, unknown> | null;
  newValues?: Record<string, unknown> | null;
  sourceService?: string;
  correlationId?: string;
}

export interface AuditQueryParams {
  startDate?: string;
  endDate?: string;
  operation?: string;
  entityName?: string;
  userId?: string;
  sourceService?: string;
  correlationId?: string;
}

export interface DepositoRequest {
  contaId: string;
  valor: number;
  descricao?: string;
}

export interface SaqueRequest {
  contaId: string;
  valor: number;
  descricao?: string;
}

export interface TransferenciaRequest {
  contaOrigemId: string;
  contaDestinoId: string;
  valor: number;
  descricao?: string;
}
