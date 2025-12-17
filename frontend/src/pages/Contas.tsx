import { useState, useEffect } from 'react';
import { contasService, usuariosService } from '../services/contasApi';
import type { Conta, Usuario } from '../types';

export function Contas() {
  const [contas, setContas] = useState<Conta[]>([]);
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [mensagem, setMensagem] = useState<{ tipo: 'success' | 'error'; texto: string } | null>(null);
  
  const [form, setForm] = useState({
    numeroConta: '',
    saldo: '',
    usuarioId: '',
    tipo: 'CORRENTE',
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const [contasRes, usuariosRes] = await Promise.all([
        contasService.listar(),
        usuariosService.listar(),
      ]);
      setContas(contasRes.data);
      setUsuarios(usuariosRes.data);
    } catch (error) {
      console.error('Erro ao carregar dados:', error);
      setMensagem({ tipo: 'error', texto: 'Erro ao carregar dados' });
    } finally {
      setLoading(false);
    }
  };

  const handleOpenModal = (conta?: Conta) => {
    if (conta) {
      setEditingId(conta.id);
      setForm({
        numeroConta: conta.numeroConta,
        saldo: conta.saldo.toString(),
        usuarioId: (conta as any).usuarioId || '',
        tipo: (conta as any).tipo || 'CORRENTE',
      });
    } else {
      setEditingId(null);
      setForm({ numeroConta: '', saldo: '0', usuarioId: '', tipo: 'CORRENTE' });
    }
    setShowModal(true);
    setMensagem(null);
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setEditingId(null);
    setForm({ numeroConta: '', saldo: '0', usuarioId: '', tipo: 'CORRENTE' });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setMensagem(null);

    try {
      const payload = {
        numeroConta: form.numeroConta,
        saldo: parseFloat(form.saldo),
        usuarioId: form.usuarioId,
        tipo: form.tipo,
      };

      if (editingId) {
        await contasService.atualizar(editingId, payload);
        setMensagem({ tipo: 'success', texto: 'Conta atualizada com sucesso!' });
      } else {
        await contasService.criar(payload);
        setMensagem({ tipo: 'success', texto: 'Conta criada com sucesso!' });
      }
      handleCloseModal();
      loadData();
    } catch (error: any) {
      setMensagem({ 
        tipo: 'error', 
        texto: error.response?.data?.message || 'Erro ao salvar conta' 
      });
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Tem certeza que deseja excluir esta conta?')) return;

    try {
      await contasService.excluir(id);
      setMensagem({ tipo: 'success', texto: 'Conta excluída com sucesso!' });
      loadData();
    } catch (error: any) {
      setMensagem({ 
        tipo: 'error', 
        texto: error.response?.data?.message || 'Erro ao excluir conta' 
      });
    }
  };

  const getUsuarioName = (usuarioId: string) => {
    const usuario = usuarios.find(u => u.id === usuarioId);
    return usuario?.username || 'N/A';
  };

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold">Contas Bancárias</h1>
        <button
          onClick={() => handleOpenModal()}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
        >
          Nova Conta
        </button>
      </div>

      {mensagem && (
        <div className={`mb-4 p-4 rounded ${
          mensagem.tipo === 'success' 
            ? 'bg-green-100 text-green-800' 
            : 'bg-red-100 text-red-800'
        }`}>
          {mensagem.texto}
        </div>
      )}

      <div className="bg-white rounded-lg shadow overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-gray-500">Carregando...</div>
        ) : contas.length === 0 ? (
          <div className="p-8 text-center text-gray-500">Nenhuma conta cadastrada</div>
        ) : (
          <table className="w-full">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  ID
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Número da Conta
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Saldo
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Usuário
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Ações
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {contas.map((conta) => (
                <tr key={conta.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-500">
                    {conta.id.substring(0, 8)}...
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {conta.numeroConta}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    <span className={conta.saldo < 0 ? 'text-red-600' : 'text-green-600'}>
                      R$ {conta.saldo.toFixed(2)}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {getUsuarioName((conta as any).usuarioId)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button
                      onClick={() => handleOpenModal(conta)}
                      className="text-blue-600 hover:text-blue-900 mr-4"
                    >
                      Editar
                    </button>
                    <button
                      onClick={() => handleDelete(conta.id)}
                      className="text-red-600 hover:text-red-900"
                    >
                      Excluir
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <h2 className="text-xl font-bold mb-4">
              {editingId ? 'Editar Conta' : 'Nova Conta'}
            </h2>
            
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1">Número da Conta</label>
                <input
                  type="text"
                  value={form.numeroConta}
                  onChange={(e) => setForm({ ...form, numeroConta: e.target.value })}
                  className="w-full border rounded-lg p-2"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-1">Saldo Inicial</label>
                <input
                  type="number"
                  step="0.01"
                  value={form.saldo}
                  onChange={(e) => setForm({ ...form, saldo: e.target.value })}
                  className="w-full border rounded-lg p-2"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-1">Tipo de Conta</label>
                <select
                  value={form.tipo}
                  onChange={(e) => setForm({ ...form, tipo: e.target.value })}
                  className="w-full border rounded-lg p-2"
                  required
                >
                  <option value="CORRENTE">Conta Corrente</option>
                  <option value="POUPANCA">Conta Poupança</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium mb-1">Usuário</label>
                <select
                  value={form.usuarioId}
                  onChange={(e) => setForm({ ...form, usuarioId: e.target.value })}
                  className="w-full border rounded-lg p-2"
                  required
                >
                  <option value="">Selecione um usuário...</option>
                  {usuarios.map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.nome} - {u.email}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex justify-end space-x-3">
                <button
                  type="button"
                  onClick={handleCloseModal}
                  className="px-4 py-2 border rounded-lg hover:bg-gray-50"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  disabled={loading}
                  className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
                >
                  {loading ? 'Salvando...' : 'Salvar'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
