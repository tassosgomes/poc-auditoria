import { transacoesApi } from './api';
import type { Transacao, DepositoRequest, SaqueRequest, TransferenciaRequest } from '../types';

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
