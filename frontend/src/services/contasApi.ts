import { contasApi } from './api';
import type { Usuario, Conta } from '../types';

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
