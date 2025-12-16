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
    // ensure headers is an object compatible with AxiosRequestHeaders
    config.headers = { ...(config.headers as Record<string, any> || {}), Authorization: getAuthHeader() } as any;
    return config;
  });
});
