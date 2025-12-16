import { useState, useEffect } from 'react';
import { contasService, usuariosService } from '../services/contasApi';
import { auditoriaService } from '../services/auditoriaApi';
import type { Usuario, Conta, AuditEvent } from '../types';

export function Dashboard() {
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [contas, setContas] = useState<Conta[]>([]);
  const [eventos, setEventos] = useState<AuditEvent[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const [usuariosRes, contasRes, eventosRes] = await Promise.all([
        usuariosService.listar().catch(() => ({ data: [] })),
        contasService.listar().catch(() => ({ data: [] })),
        auditoriaService.listar().catch(() => ({ data: [] })),
      ]);
      setUsuarios(usuariosRes.data);
      setContas(contasRes.data);
      setEventos(eventosRes.data.slice(0, 5));
    } catch (error) {
      console.error('Erro ao carregar dados:', error);
    } finally {
      setLoading(false);
    }
  };

  const totalSaldo = contas.reduce((acc, conta) => acc + conta.saldo, 0);

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-6">Dashboard</h1>
      
      {loading ? (
        <div className="text-center py-8 text-gray-500">Carregando...</div>
      ) : (
        <>
          {/* Cards de Estatísticas */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
            <div className="bg-white p-6 rounded-lg shadow">
              <div className="text-sm text-gray-500 mb-2">Total de Usuários</div>
              <div className="text-3xl font-bold text-blue-600">{usuarios.length}</div>
            </div>
            
            <div className="bg-white p-6 rounded-lg shadow">
              <div className="text-sm text-gray-500 mb-2">Total de Contas</div>
              <div className="text-3xl font-bold text-green-600">{contas.length}</div>
            </div>
            
            <div className="bg-white p-6 rounded-lg shadow">
              <div className="text-sm text-gray-500 mb-2">Saldo Total</div>
              <div className="text-3xl font-bold text-purple-600">
                R$ {totalSaldo.toFixed(2)}
              </div>
            </div>
          </div>

          {/* Tabelas */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Contas Recentes */}
            <div className="bg-white rounded-lg shadow">
              <div className="p-4 border-b">
                <h2 className="text-lg font-semibold">Contas Cadastradas</h2>
              </div>
              <div className="p-4">
                {contas.length === 0 ? (
                  <p className="text-gray-500 text-center py-4">Nenhuma conta cadastrada</p>
                ) : (
                  <div className="space-y-3">
                    {contas.slice(0, 5).map((conta) => (
                      <div key={conta.id} className="flex justify-between items-center border-b pb-2">
                        <div>
                          <div className="font-medium">{conta.numeroConta}</div>
                          <div className="text-xs text-gray-500">{conta.id.substring(0, 8)}...</div>
                        </div>
                        <div className={`font-bold ${conta.saldo < 0 ? 'text-red-600' : 'text-green-600'}`}>
                          R$ {conta.saldo.toFixed(2)}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>

            {/* Eventos Recentes */}
            <div className="bg-white rounded-lg shadow">
              <div className="p-4 border-b">
                <h2 className="text-lg font-semibold">Eventos de Auditoria Recentes</h2>
              </div>
              <div className="p-4">
                {eventos.length === 0 ? (
                  <p className="text-gray-500 text-center py-4">Nenhum evento registrado</p>
                ) : (
                  <div className="space-y-3">
                    {eventos.map((evento) => (
                      <div key={evento.id} className="flex justify-between items-start border-b pb-2">
                        <div className="flex-1">
                          <div className="font-medium text-sm">{evento.entityName}</div>
                          <div className="text-xs text-gray-500">
                            {new Date(evento.timestamp).toLocaleString('pt-BR')}
                          </div>
                        </div>
                        <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                          evento.operation === 'INSERT' ? 'bg-green-100 text-green-800' :
                          evento.operation === 'UPDATE' ? 'bg-yellow-100 text-yellow-800' :
                          'bg-red-100 text-red-800'
                        }`}>
                          {evento.operation}
                        </span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
