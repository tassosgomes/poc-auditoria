import { useState, useEffect } from 'react';
import { transacoesService } from '../services/transacoesApi';
import { contasService } from '../services/contasApi';
import type { Conta } from '../types';

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
    contasService.listar().then(res => setContas(res.data)).catch(console.error);
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
