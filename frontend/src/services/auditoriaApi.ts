import { auditoriaApi } from './api';
import type { AuditEvent, AuditQueryParams } from '../types';

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
